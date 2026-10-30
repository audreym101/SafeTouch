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
const EMAIL = `/api/consent/${consent.consentId}/email`;
const VERIFY = `${EMAIL}/verify`;
const LOGIN = `/api/consent/${consent.consentId}/login`;
const codeIn = (text) => text.match(/\b(\d{6})\b/)[1];

// A server with a consent the parent has just confirmed.
async function withVerifiedParent(fn, { verify = true } = {}) {
  const clock = { t: Date.parse('2026-10-20T08:00:00Z') };
  const mailer = createMailer({ MAIL_PROVIDER: 'console', QUIET_MAILER: '1' });
  const store = new MemoryStore();
  const server = createApp(store, { mailer, now: () => clock.t }).listen(0);
  const base = `http://127.0.0.1:${server.address().port}`;
  const call = (method, path, body) => fetch(`${base}${path}`, {
    method, headers: { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body),
  });
  try {
    await call('POST', '/api/consent', consent);
    if (verify) await call('POST', `/api/consent/${consent.consentId}/verify`, { code: codeIn(mailer.sent[0].text) });
    mailer.sent.length = 0;
    await fn({ call, mailer, clock, store });
  } finally { server.close(); }
}

test('a new email gets the code, and after it the codes go to the new email', async () => {
  await withVerifiedParent(async ({ call, mailer, clock }) => {
    const asked = await call('POST', EMAIL, { email: 'marie.new@example.com' });
    assert.strictEqual(asked.status, 200);
    assert.deepStrictEqual(await asked.json(), { sent: true, emailMasked: 'm***w@example.com' });
    assert.strictEqual(mailer.sent.length, 1);
    assert.strictEqual(mailer.sent[0].to, 'marie.new@example.com', 'the code goes to the new email, not the old one');
    assert.match(mailer.sent[0].subject, /new email/);

    const done = await call('POST', VERIFY, { code: codeIn(mailer.sent[0].text) });
    assert.strictEqual(done.status, 200);
    const body = await done.json();
    assert.strictEqual(body.emailChanged, true);
    assert.strictEqual(body.emailMasked, 'm***w@example.com');
    assert.strictEqual(mailer.sent[1].to, 'uwase.marie@example.com', 'the old email is told');
    assert.match(mailer.sent[1].subject, /changed/);
    assert.doesNotMatch(mailer.sent[1].text, /\b\d{6}\b/, 'the notice has no code');

    // Logging in now works with the new email only.
    mailer.sent.length = 0;
    clock.t += 2 * 60 * 1000;
    await call('POST', LOGIN, { email: 'uwase.marie@example.com' });
    assert.strictEqual(mailer.sent.length, 0, 'the old email no longer gets codes');
    await call('POST', LOGIN, { email: 'Marie.New@example.com' });
    assert.strictEqual(mailer.sent.length, 1);
    assert.strictEqual(mailer.sent[0].to, 'marie.new@example.com');
  });
});

test('the email stays the same until the code from the new email is typed', async () => {
  await withVerifiedParent(async ({ call, mailer, store }) => {
    await call('POST', EMAIL, { email: 'marie.new@example.com' });
    assert.strictEqual((await store.getConsent(consent.consentId)).email, 'uwase.marie@example.com');
    const wrong = await call('POST', VERIFY, { code: codeIn(mailer.sent[0].text) === '000000' ? '111111' : '000000' });
    assert.strictEqual(wrong.status, 400);
    assert.strictEqual((await wrong.json()).attemptsLeft, 4);
    assert.strictEqual((await store.getConsent(consent.consentId)).email, 'uwase.marie@example.com');
  });
});

test('asking again replaces the earlier new email', async () => {
  await withVerifiedParent(async ({ call, mailer, clock }) => {
    await call('POST', EMAIL, { email: 'typo@example.com' });
    const first = codeIn(mailer.sent[0].text);
    clock.t += 61 * 1000;
    await call('POST', EMAIL, { email: 'marie.new@example.com' });
    assert.strictEqual(mailer.sent[1].to, 'marie.new@example.com');
    if (first !== codeIn(mailer.sent[1].text)) assert.strictEqual((await call('POST', VERIFY, { code: first })).status, 400, 'the code sent to the typo no longer works');
    const done = await call('POST', VERIFY, { code: codeIn(mailer.sent[1].text) });
    assert.strictEqual((await done.json()).emailMasked, 'm***w@example.com');
  });
});

test('changing the email needs a confirmed consent and a recent login', async () => {
  await withVerifiedParent(async ({ call }) => {
    assert.strictEqual((await call('POST', EMAIL, { email: 'marie.new@example.com' })).status, 403);
  }, { verify: false });
  await withVerifiedParent(async ({ call, mailer, clock }) => {
    clock.t += 2 * 60 * 60 * 1000;
    assert.strictEqual((await call('POST', EMAIL, { email: 'marie.new@example.com' })).status, 401, 'two hours after the last login');
    await call('POST', LOGIN, { email: consent.email });
    await call('POST', `${LOGIN}/verify`, { code: codeIn(mailer.sent[0].text) });
    assert.strictEqual((await call('POST', EMAIL, { email: 'marie.new@example.com' })).status, 200, 'after logging in again');
  });
});

test('bad requests are refused', async () => {
  await withVerifiedParent(async ({ call, clock }) => {
    assert.strictEqual((await call('POST', EMAIL, { email: 'not-an-email' })).status, 400);
    assert.strictEqual((await call('POST', EMAIL, { email: 'UWASE.marie@example.com' })).status, 400, 'the same email');
    assert.strictEqual((await call('POST', VERIFY, { code: '123456' })).status, 410, 'nothing asked yet');
    assert.strictEqual((await call('POST', EMAIL, { email: 'a@example.com' })).status, 200);
    assert.strictEqual((await call('POST', EMAIL, { email: 'b@example.com' })).status, 429, 'once a minute');
    clock.t += 31 * 60 * 1000;
    assert.strictEqual((await call('POST', VERIFY, { code: '123456' })).status, 410, 'the code lasts 30 minutes');
  });
});
