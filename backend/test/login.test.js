const test = require('node:test');
const assert = require('node:assert');
const { createApp } = require('../src/app');
const { MemoryStore } = require('../src/store');
const { createMailer } = require('../src/mailer');
const { createSmsSender } = require('../src/sms');

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
const LOGIN = `/api/consent/${consent.consentId}/login`;
const VERIFY = `${LOGIN}/verify`;
const codeIn = (text) => text.match(/\b(\d{6})\b/)[1];

// A server with a consent that is already verified, so login can be tried.
async function withVerifiedParent(fn, { sms = false } = {}) {
  const clock = { t: Date.parse('2026-10-20T08:00:00Z') };
  const mailer = createMailer({ MAIL_PROVIDER: 'console', QUIET_MAILER: '1' });
  const texts = createSmsSender(sms ? { SMS_PROVIDER: 'console', QUIET_MAILER: '1' } : {}, () => clock.t);
  const server = createApp(new MemoryStore(), { mailer, sms: texts, now: () => clock.t }).listen(0);
  const base = `http://127.0.0.1:${server.address().port}`;
  const call = (method, path, body) => fetch(`${base}${path}`, {
    method, headers: { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body),
  });
  try {
    await call('POST', '/api/consent', consent);
    await call('POST', `/api/consent/${consent.consentId}/verify`, { code: codeIn(mailer.sent[0].text) });
    mailer.sent.length = 0;
    if (texts.sent) texts.sent.length = 0;
    await fn({ call, mailer, texts, clock });
  } finally { server.close(); }
}

test('the parent logs in with their email and the code sent to it', async () => {
  await withVerifiedParent(async ({ call, mailer }) => {
    const asked = await call('POST', LOGIN, { email: 'uwase.marie@example.com' });
    assert.strictEqual(asked.status, 200);
    assert.deepStrictEqual(await asked.json(), { sent: true });
    assert.strictEqual(mailer.sent.length, 1);
    assert.match(mailer.sent[0].subject, /login code/);
    assert.strictEqual(mailer.sent[0].to, 'uwase.marie@example.com');
    assert.doesNotMatch(mailer.sent[0].text, /agreed|\+250/, 'a login email is just the code, not the consent copy');

    const res = await call('POST', VERIFY, { code: codeIn(mailer.sent[0].text) });
    assert.strictEqual(res.status, 200);
    const body = await res.json();
    assert.strictEqual(body.loggedIn, true);
    assert.strictEqual(body.parentName, undefined, 'no name or phone in the reply');
    assert.strictEqual(body.phone, undefined);
  });
});

test('the email match ignores case and spaces', async () => {
  await withVerifiedParent(async ({ call, mailer }) => {
    await call('POST', LOGIN, { email: '  Uwase.Marie@Example.COM ' });
    assert.strictEqual(mailer.sent.length, 1);
  });
});

test('a wrong email gets the same reply, and no code is sent', async () => {
  await withVerifiedParent(async ({ call, mailer }) => {
    const res = await call('POST', LOGIN, { email: 'someone.else@example.com' });
    assert.strictEqual(res.status, 200);
    assert.deepStrictEqual(await res.json(), { sent: true });
    assert.strictEqual(mailer.sent.length, 0);
    const unknown = await call('POST', '/api/consent/11111111-2222-4333-8444-555555555555/login', { email: 'uwase.marie@example.com' });
    assert.deepStrictEqual(await unknown.json(), { sent: true }, 'an unknown consent looks the same');
    assert.strictEqual(mailer.sent.length, 0);
  });
});

test('a bad request is refused', async () => {
  await withVerifiedParent(async ({ call }) => {
    assert.strictEqual((await call('POST', LOGIN, { email: 'not-an-email' })).status, 400);
    assert.strictEqual((await call('POST', LOGIN, {})).status, 400);
    assert.strictEqual((await call('POST', '/api/consent/not-a-uuid/login', { email: 'a@b.co' })).status, 400);
  });
});

test('a parent who has not verified the consent cannot get a login code', async () => {
  const mailer = createMailer({ MAIL_PROVIDER: 'console', QUIET_MAILER: '1' });
  const server = createApp(new MemoryStore(), { mailer, sms: createSmsSender({}) }).listen(0);
  const base = `http://127.0.0.1:${server.address().port}`;
  const post = (path, body) => fetch(`${base}${path}`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
  try {
    await post('/api/consent', consent);
    mailer.sent.length = 0;
    assert.deepStrictEqual(await (await post(LOGIN, { email: consent.email })).json(), { sent: true });
    assert.strictEqual(mailer.sent.length, 0);
  } finally { server.close(); }
});

test('a wrong code is refused, 5 tries per code, and a new code starts again', async () => {
  await withVerifiedParent(async ({ call, mailer, clock }) => {
    await call('POST', LOGIN, { email: consent.email });
    const good = codeIn(mailer.sent[0].text);
    const wrong = good === '000000' ? '111111' : '000000';
    const first = await call('POST', VERIFY, { code: wrong });
    assert.strictEqual(first.status, 400);
    assert.strictEqual((await first.json()).attemptsLeft, 4);
    for (let i = 0; i < 4; i++) await call('POST', VERIFY, { code: wrong });
    assert.strictEqual((await call('POST', VERIFY, { code: good })).status, 429, 'locked out even with the right code');

    clock.t += 61 * 1000;
    await call('POST', LOGIN, { email: consent.email });
    const fresh = codeIn(mailer.sent[1].text);
    assert.strictEqual((await call('POST', VERIFY, { code: fresh })).status, 200);
  });
});

test('a login code expires after 10 minutes and works once', async () => {
  await withVerifiedParent(async ({ call, mailer, clock }) => {
    await call('POST', LOGIN, { email: consent.email });
    const code = codeIn(mailer.sent[0].text);
    clock.t += 10 * 60 * 1000 + 1000;
    assert.strictEqual((await call('POST', VERIFY, { code })).status, 410);

    clock.t += 61 * 1000;
    await call('POST', LOGIN, { email: consent.email });
    const next = codeIn(mailer.sent[1].text);
    assert.strictEqual((await call('POST', VERIFY, { code: next })).status, 200);
    assert.strictEqual((await call('POST', VERIFY, { code: next })).status, 410, 'a used code is gone');
  });
});

test('codes can be asked for at most once a minute and 10 times a day', async () => {
  await withVerifiedParent(async ({ call, mailer, clock }) => {
    assert.strictEqual((await call('POST', LOGIN, { email: consent.email })).status, 200);
    assert.strictEqual((await call('POST', LOGIN, { email: consent.email })).status, 429);
    for (let i = 1; i < 10; i++) {
      clock.t += 61 * 1000;
      assert.strictEqual((await call('POST', LOGIN, { email: consent.email })).status, 200, `code ${i + 1}`);
    }
    clock.t += 61 * 1000;
    assert.strictEqual((await call('POST', LOGIN, { email: consent.email })).status, 429, 'the 11th today is refused');
    assert.strictEqual(mailer.sent.length, 10);
  });
});

test('the login code does not open the consent verification, and the consent code does not log in', async () => {
  await withVerifiedParent(async ({ call, mailer }) => {
    await call('POST', LOGIN, { email: consent.email });
    const loginCode = codeIn(mailer.sent[0].text);
    const other = loginCode === '123456' ? '654321' : '123456';
    assert.strictEqual((await call('POST', VERIFY, { code: other })).status, 400);
  });
});

test('the login code is also texted when SMS is on, and either channel can fail alone', async () => {
  await withVerifiedParent(async ({ call, mailer, texts }) => {
    await call('POST', LOGIN, { email: consent.email });
    assert.strictEqual(texts.sent.length, 1);
    assert.strictEqual(texts.sent[0].to, '250788123456');
    assert.match(texts.sent[0].text, /login code/);
    assert.strictEqual(codeIn(texts.sent[0].text), codeIn(mailer.sent[0].text), 'one code in both');
    assert.strictEqual((await call('POST', VERIFY, { code: codeIn(texts.sent[0].text) })).status, 200);
  }, { sms: true });

  const failingMailer = { provider: 'brevo', sent: [], async send() { throw new Error('mail down'); } };
  const sms = createSmsSender({ SMS_PROVIDER: 'console', QUIET_MAILER: '1' });
  const store = new MemoryStore();
  await store.saveConsent({ ...consent, verified: true });
  const server = createApp(store, { mailer: failingMailer, sms }).listen(0);
  try {
    const res = await fetch(`http://127.0.0.1:${server.address().port}${LOGIN}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: consent.email }),
    });
    assert.strictEqual(res.status, 200, 'the text alone is enough');
    assert.strictEqual(sms.sent.length, 1);
  } finally { server.close(); }
});

test('when neither the email nor the text can be sent, the app is told', async () => {
  const failingMailer = { provider: 'brevo', sent: [], async send() { throw new Error('mail down'); } };
  const store = new MemoryStore();
  await store.saveConsent({ ...consent, verified: true });
  const server = createApp(store, { mailer: failingMailer, sms: createSmsSender({}) }).listen(0);
  try {
    const res = await fetch(`http://127.0.0.1:${server.address().port}${LOGIN}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: consent.email }),
    });
    assert.strictEqual(res.status, 502);
  } finally { server.close(); }
});

test('the status never shows login codes', async () => {
  await withVerifiedParent(async ({ call, mailer }) => {
    await call('POST', LOGIN, { email: consent.email });
    const status = await (await call('GET', `/api/consent/${consent.consentId}`)).json();
    assert.deepStrictEqual(Object.keys(status).sort(), ['consentId', 'emailMasked', 'verified', 'verifiedAt']);
    assert.ok(!JSON.stringify(status).includes(codeIn(mailer.sent[0].text)));
  });
});
