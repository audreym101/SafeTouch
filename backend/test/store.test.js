const test = require('node:test');
const assert = require('node:assert');
const { createApp } = require('../src/app');
const { createStore, MemoryStore, MongoStore } = require('../src/store');
const { createMailer } = require('../src/mailer');

const progress = {
  deviceId: '3f2b8c1e-5a7d-4e2f-9b61-0c4d8e7a1f23',
  ageBand: '6-8',
  language: 'en',
  completedLevels: [1, 2],
  quizScores: [3, 6],
  badges: ['bubble', 'spotter'],
  trustedAdultRoles: ['teacher', 'grandparent'],
  scenarioResponses: [
    { scenarioId: 'S01', selectedAnswer: 'A', isCorrect: true, answeredAt: '2026-10-01T09:00:00Z' },
    { scenarioId: 'S03', selectedAnswer: 'B', isCorrect: false, answeredAt: '2026-10-01T09:01:00Z' },
  ],
};

const consent = {
  consentId: '6b1d6c2e-8f0a-4c5b-9d7e-1a2b3c4d5e6f',
  deviceId: progress.deviceId,
  parentName: 'Uwase Marie',
  phone: '+250 788 123 456',
  email: 'uwase.marie@example.com',
  country: 'Rwanda',
  language: 'en',
  termsVersion: '1.0',
  agreements: { guardian: true, terms: true, childUse: true, dataUse: true },
};

// Just enough of a MongoDB database for MongoStore. Like the real driver, it adds
// an _id to stored documents and only leaves it out when the projection says so.
function fakeMongo() {
  const collections = new Map();
  const copy = (data) => JSON.parse(JSON.stringify(data));
  const matches = (filter) => (d) => Object.entries(filter).every(([k, v]) => d[k] === v);
  const project = (d, options) => {
    const out = copy(d);
    if (options && options.projection && options.projection._id === 0) delete out._id;
    return out;
  };
  return {
    collections,
    collection(name) {
      if (!collections.has(name)) collections.set(name, { docs: [], indexes: [] });
      const col = collections.get(name);
      return {
        createIndex: async (keys, options) => { col.indexes.push({ keys, options }); },
        replaceOne: async (filter, doc, options) => {
          assert.ok(!('_id' in doc), 'never write a Mongo _id from the API');
          const i = col.docs.findIndex(matches(filter));
          if (i >= 0) col.docs[i] = { _id: col.docs[i]._id, ...copy(doc) };
          else if (options && options.upsert) col.docs.push({ _id: `id${col.docs.length}`, ...copy(doc) });
        },
        findOne: async (filter, options) => { const d = col.docs.find(matches(filter)); return d ? project(d, options) : null; },
        find: (filter, options) => ({ toArray: async () => col.docs.filter(matches(filter)).map((d) => project(d, options)) }),
        deleteOne: async (filter) => {
          const i = col.docs.findIndex(matches(filter));
          if (i >= 0) col.docs.splice(i, 1);
          return { deletedCount: i >= 0 ? 1 : 0 };
        },
        countDocuments: async (filter = {}) => col.docs.filter(matches(filter)).length,
      };
    },
  };
}

test('MongoStore keeps progress in progress, one document per device', async () => {
  const db = fakeMongo();
  const store = await new MongoStore(db).connect();
  assert.deepStrictEqual(db.collections.get('progress').indexes, [{ keys: { deviceId: 1 }, options: { unique: true } }]);

  await store.upsert(progress);
  await store.upsert({ ...progress, completedLevels: [1, 2, 3] });
  assert.strictEqual(db.collections.get('progress').docs.length, 1, 'a second sync replaces the first');
  assert.deepStrictEqual(await store.get(progress.deviceId), { ...progress, completedLevels: [1, 2, 3] });
  assert.strictEqual(await store.get('nobody'), null);
  assert.strictEqual((await store.summary()).scenarioAccuracy, 50);

  assert.strictEqual(await store.remove(progress.deviceId), true);
  assert.strictEqual(await store.remove(progress.deviceId), false);
});

test('MongoStore keeps consents in their own collection and counts them', async () => {
  const db = fakeMongo();
  const store = await new MongoStore(db).connect();
  await store.saveConsent({ ...consent, verified: false });
  await store.saveConsent({ ...consent, consentId: 'second', verified: true });
  assert.deepStrictEqual(await store.consentCounts(), { total: 2, verified: 1 });
  assert.deepStrictEqual(await store.getConsent(consent.consentId), { ...consent, verified: false });
  assert.strictEqual(db.collections.get('progress').docs.length, 0, 'consent data never goes into progress');
  assert.strictEqual(await store.removeConsent('second'), true);
  assert.deepStrictEqual(await store.consentCounts(), { total: 1, verified: 0 });
});

test('the whole consent flow works over MongoDB', async () => {
  const db = fakeMongo();
  const store = await new MongoStore(db).connect();
  const mailer = createMailer({ MAIL_PROVIDER: 'console', QUIET_MAILER: '1' });
  const server = createApp(store, { mailer }).listen(0);
  const base = `http://127.0.0.1:${server.address().port}`;
  const call = (method, p, body) => fetch(`${base}${p}`, {
    method, headers: { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body),
  });
  try {
    assert.strictEqual((await call('POST', '/api/consent', consent)).status, 201);
    const code = mailer.sent[0].text.match(/\b(\d{6})\b/)[1];
    assert.strictEqual((await call('POST', `/api/consent/${consent.consentId}/verify`, { code })).status, 200);
    assert.strictEqual((await (await call('GET', `/api/consent/${consent.consentId}`)).json()).verified, true);

    assert.strictEqual((await call('POST', '/api/progress', progress)).status, 201);
    assert.deepStrictEqual((await (await call('GET', '/api/summary')).json()).consents, { total: 1, verified: 1 });

    assert.strictEqual((await call('DELETE', `/api/consent/${consent.consentId}`)).status, 204);
    assert.strictEqual(db.collections.get('consents').docs.length, 0);
    assert.strictEqual(db.collections.get('progress').docs.length, 0, 'withdrawal also deletes the synced progress');
  } finally { server.close(); }
});

test('MONGODB_URI picks MongoDB, and without it the store is in memory', async () => {
  // A URI the driver refuses before any network call: the error shows MongoDB was chosen.
  await assert.rejects(createStore({ MONGODB_URI: 'not-a-mongo-uri' }), /scheme/i);
  assert.ok((await createStore({})) instanceof MemoryStore);
});
