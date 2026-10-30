const test = require('node:test');
const assert = require('node:assert');
const { createApp } = require('../src/app');
const { MemoryStore } = require('../src/store');
const { createMailer } = require('../src/mailer');

const consent = {
  consentId: '6b1d6c2e-8f0a-4c5b-9d7e-1a2b3c4d5e6f',
  deviceId: '3f2b8c1e-5a7d-4e2f-9b61-0c4d8e7a1f23',
  parentName: 'Uwase Marie',
  phone: '+250 788 123 456',
  email: 'uwase.marie@example.com',
  country: 'Rwanda',
  language: 'en',
  termsVersion: '1.0',
  agreements: { guardian: true, terms: true, childUse: true, dataUse: true },
};

// A server with an in-memory store, a mailer that keeps emails, and a clock tests can move.
async function withServer(fn) {
  const store = new MemoryStore();
  const mailer = createMailer({ MAIL_PROVIDER: 'console', QUIET_MAILER: '1' });
  const clock = { t: Date.parse('2026-10-20T08:00:00Z') };
  const server = createApp(store, { mailer, now: () => clock.t }).listen(0);
  const base = `http://127.0.0.1:${server.address().port}`;
  const call = (method, path, body) => fetch(`${base}${path}`, {
    method, headers: { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body),
  });
  const lastCode = () => mailer.sent[mailer.sent.length - 1].text.match(/\b(\d{6})\b/)[1];
  try { await fn({ call, mailer, store, clock, lastCode }); } finally { server.close(); }
}

test('a parent submits consent and gets a code and a copy by email', () => withServer(async ({ call, mailer }) => {
  const res = await call('POST', '/api/consent', consent);
  assert.strictEqual(res.status, 201);
  const body = await res.json();
  assert.strictEqual(body.verified, false);
  assert.strictEqual(body.emailSent, true);
  assert.strictEqual(body.emailMasked, 'u***e@example.com');
  assert.strictEqual(mailer.sent.length, 1);
  const email = mailer.sent[0];
  assert.strictEqual(email.to, consent.email);
  assert.match(email.text, /\b\d{6}\b/);
  assert.match(email.text, /legal guardian/);
  assert.match(email.text, /\+250 788 123 456/);
}));

test('the right code verifies the consent', () => withServer(async ({ call, lastCode }) => {
  await call('POST', '/api/consent', consent);
  const res = await call('POST', `/api/consent/${consent.consentId}/verify`, { code: lastCode() });
  assert.strictEqual(res.status, 200);
  const body = await res.json();
  assert.strictEqual(body.verified, true);
  assert.ok(body.verifiedAt);
  const status = await (await call('GET', `/api/consent/${consent.consentId}`)).json();
  assert.strictEqual(status.verified, true);
}));

test('a wrong code is refused, and after 5 tries a new code is needed', () => withServer(async ({ call, lastCode }) => {
  await call('POST', '/api/consent', consent);
  const wrong = lastCode() === '000000' ? '111111' : '000000';
  for (let i = 0; i < 5; i++) {
    const res = await call('POST', `/api/consent/${consent.consentId}/verify`, { code: wrong });
    assert.strictEqual(res.status, 400);
  }
  const blocked = await call('POST', `/api/consent/${consent.consentId}/verify`, { code: lastCode() });
  assert.strictEqual(blocked.status, 429);
}));

test('a code expires after 24 hours', () => withServer(async ({ call, clock, lastCode }) => {
  await call('POST', '/api/consent', consent);
  clock.t += 25 * 60 * 60 * 1000;
  const res = await call('POST', `/api/consent/${consent.consentId}/verify`, { code: lastCode() });
  assert.strictEqual(res.status, 410);
}));

test('resending waits a minute and sends a new working code', () => withServer(async ({ call, clock, mailer, lastCode }) => {
  await call('POST', '/api/consent', consent);
  const first = lastCode();
  assert.strictEqual((await call('POST', `/api/consent/${consent.consentId}/resend`)).status, 429);
  clock.t += 61 * 1000;
  const res = await call('POST', `/api/consent/${consent.consentId}/resend`);
  assert.strictEqual(res.status, 200);
  assert.strictEqual(mailer.sent.length, 2);
  const second = lastCode();
  if (first !== second) {
    assert.strictEqual((await call('POST', `/api/consent/${consent.consentId}/verify`, { code: first })).status, 400);
  }
  assert.strictEqual((await call('POST', `/api/consent/${consent.consentId}/verify`, { code: second })).status, 200);
}));

test('sending the same consent again (an offline retry) sends no second email', () => withServer(async ({ call, mailer }) => {
  await call('POST', '/api/consent', consent);
  const again = await call('POST', '/api/consent', consent);
  assert.strictEqual(again.status, 200);
  assert.strictEqual((await again.json()).emailSent, false);
  assert.strictEqual(mailer.sent.length, 1);
}));

test('a corrected email address gets a new code', () => withServer(async ({ call, mailer }) => {
  await call('POST', '/api/consent', consent);
  const res = await call('POST', '/api/consent', { ...consent, email: 'marie.uwase@example.com' });
  assert.strictEqual(res.status, 200);
  assert.strictEqual(mailer.sent.length, 2);
  assert.strictEqual(mailer.sent[1].to, 'marie.uwase@example.com');
}));

test('consent needs every agreement, a valid email and a phone with country code', () => withServer(async ({ call }) => {
  for (const bad of [
    { ...consent, agreements: { ...consent.agreements, childUse: false } },
    { ...consent, email: 'not-an-email' },
    { ...consent, phone: '12' },
    { ...consent, parentName: '' },
    { ...consent, childName: 'Aline' },
  ]) {
    const res = await call('POST', '/api/consent', bad);
    assert.strictEqual(res.status, 400, JSON.stringify(bad));
  }
}));

test('the status reply carries no name, phone or full email', () => withServer(async ({ call }) => {
  await call('POST', '/api/consent', consent);
  const text = await (await call('GET', `/api/consent/${consent.consentId}`)).text();
  assert.ok(!text.includes('Uwase'));
  assert.ok(!text.includes('788'));
  assert.ok(!text.includes(consent.email));
}));

test('withdrawing deletes the consent and the device progress', () => withServer(async ({ call, store }) => {
  await call('POST', '/api/consent', consent);
  await store.upsert({ deviceId: consent.deviceId, ageBand: '6-8' });
  const res = await call('DELETE', `/api/consent/${consent.consentId}`);
  assert.strictEqual(res.status, 204);
  assert.strictEqual((await call('GET', `/api/consent/${consent.consentId}`)).status, 404);
  assert.strictEqual(await store.get(consent.deviceId), null);
}));

test('the Kinyarwanda email is in Kinyarwanda', () => withServer(async ({ call, mailer }) => {
  await call('POST', '/api/consent', { ...consent, language: 'rw' });
  assert.match(mailer.sent[0].subject, /uruhushya/);
  assert.match(mailer.sent[0].text, /Muraho Uwase Marie/);
}));

test('names with HTML are escaped in the email', () => withServer(async ({ call, mailer }) => {
  await call('POST', '/api/consent', { ...consent, parentName: "Marie O'Neil" });
  assert.ok(mailer.sent[0].html.includes('O&#39;Neil'));
}));

// The terms and privacy notice as the app showed them (ConsentService sends ContentLibrary.Terms).
const terms = [
  { termId: 'what', title: 'What SafeTouch is', body: 'SafeTouch is an educational app that teaches children aged 6 to 11 about body safety.' },
  { termId: 'child', title: 'What SafeTouch keeps about your child', body: 'Stars, badges and lesson answers, on this phone.' },
];

test('the consent email shows the terms the parent read and agreed to, and they are kept with the consent', () => withServer(async ({ call, mailer, store }) => {
  const res = await call('POST', '/api/consent', { ...consent, terms });
  assert.strictEqual(res.status, 201);
  const mail = mailer.sent[0];
  assert.match(mail.text, /The terms you read and agreed to \(version 1\.0\)/);
  for (const s of terms) {
    assert.ok(mail.text.includes(s.title) && mail.text.includes(s.body), `the text email has "${s.title}"`);
    assert.ok(mail.html.includes(s.title) && mail.html.includes(s.body), `the HTML email has "${s.title}"`);
  }
  assert.deepStrictEqual((await store.getConsent(consent.consentId)).terms, terms.map(({ title, body }) => ({ title, body })));

  // A corrected email keeps the terms, and its new email shows them too.
  await call('POST', '/api/consent', { ...consent, email: 'marie.new@example.com', terms });
  assert.ok(mailer.sent[1].text.includes(terms[1].body));
}));

test('terms in the wrong shape are refused', () => withServer(async ({ call }) => {
  for (const bad of ['text', [{ title: 'x' }], [{ title: 'x', body: 'y', extra: 1 }], [{ title: 'x', body: 'y'.repeat(4001) }]]) {
    assert.strictEqual((await call('POST', '/api/consent', { ...consent, terms: bad })).status, 400, JSON.stringify(bad).slice(0, 40));
  }
}));

test('Unity\'s empty terms list and empty termId are accepted', () => withServer(async ({ call, mailer }) => {
  assert.strictEqual((await call('POST', '/api/consent', { ...consent, terms: [] })).status, 201);
  assert.doesNotMatch(mailer.sent[0].text, /terms you read/, 'no terms section when none were sent');
  const other = { ...consent, consentId: '7c2e7d3f-9a1b-4d6c-8e8f-2b3c4d5e6f70', terms: [{ termId: '', title: 'T', body: 'B' }] };
  assert.strictEqual((await call('POST', '/api/consent', other)).status, 201);
}));
