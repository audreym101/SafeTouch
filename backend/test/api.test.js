const test = require('node:test');
const assert = require('node:assert');
const { createApp } = require('../src/app');
const { MemoryStore } = require('../src/store');

const sample = {
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

async function withServer(fn) {
  const server = createApp(new MemoryStore()).listen(0);
  const base = `http://127.0.0.1:${server.address().port}`;
  try { await fn(base); } finally { server.close(); }
}

const post = (base, body) => fetch(`${base}/api/progress`, {
  method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body),
});

test('saves and reads back anonymous progress', () => withServer(async (base) => {
  const res = await post(base, sample);
  assert.strictEqual(res.status, 201);
  const got = await (await fetch(`${base}/api/progress/${sample.deviceId}`)).json();
  assert.deepStrictEqual(got.completedLevels, [1, 2]);
}));

test('rejects a payload that carries a child name', () => withServer(async (base) => {
  const res = await post(base, { ...sample, childName: 'Aline' });
  assert.strictEqual(res.status, 400);
  const { errors } = await res.json();
  assert.ok(errors.some((e) => e.includes('childName')));
}));

test('rejects a device id that is not a random UUID', () => withServer(async (base) => {
  const res = await post(base, { ...sample, deviceId: '0788123456' });
  assert.strictEqual(res.status, 400);
}));

test('summary returns totals only', () => withServer(async (base) => {
  await post(base, sample);
  const s = await (await fetch(`${base}/api/summary`)).json();
  assert.strictEqual(s.devices, 1);
  assert.strictEqual(s.scenarioAccuracy, 50);
  assert.strictEqual(s.byLesson[0].completed, 1);
  assert.ok(!JSON.stringify(s).includes(sample.deviceId));
}));

test('a parent can withdraw and delete the record', () => withServer(async (base) => {
  await post(base, sample);
  const del = await fetch(`${base}/api/progress/${sample.deviceId}`, { method: 'DELETE' });
  assert.strictEqual(del.status, 204);
  const after = await fetch(`${base}/api/progress/${sample.deviceId}`);
  assert.strictEqual(after.status, 404);
}));

test('accepts the Memory Quest as level 5', () => withServer(async (base) => {
  const res = await post(base, { ...sample, completedLevels: [1, 2, 3, 4, 5] });
  assert.strictEqual(res.status, 201);
}));

test('rejects level numbers outside the lessons', () => withServer(async (base) => {
  const res = await post(base, { ...sample, completedLevels: [1, 6] });
  assert.strictEqual(res.status, 400);
}));
