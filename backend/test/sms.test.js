const test = require('node:test');
const assert = require('node:assert');
const { createApp } = require('../src/app');
const { MemoryStore } = require('../src/store');
const { createMailer } = require('../src/mailer');
const { createSmsSender, consentSms, normalisePhone } = require('../src/sms');

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

async function withServer({ smsEnv = { SMS_PROVIDER: 'console', QUIET_MAILER: '1' }, mailer, sms } = {}, fn) {
  const clock = { t: Date.parse('2026-10-20T08:00:00Z') };
  const theMailer = mailer || createMailer({ MAIL_PROVIDER: 'console', QUIET_MAILER: '1' });
  const theSms = sms || createSmsSender(smsEnv, () => clock.t);
  const server = createApp(new MemoryStore(), { mailer: theMailer, sms: theSms, now: () => clock.t }).listen(0);
  const base = `http://127.0.0.1:${server.address().port}`;
  const call = (method, path, body) => fetch(`${base}${path}`, {
    method, headers: { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body),
  });
  try { await fn({ call, mailer: theMailer, sms: theSms, clock }); } finally { server.close(); }
}

const codeIn = (text) => text.match(/\b(\d{6})\b/)[1];

test('phone numbers become digits with the country code', () => {
  assert.strictEqual(normalisePhone('+250 788 123 456'), '250788123456');
  assert.strictEqual(normalisePhone('00250-788-123-456'), '250788123456');
  assert.strictEqual(normalisePhone('(250) 788 123 456'), '250788123456');
  assert.strictEqual(normalisePhone('123'), null);
  assert.strictEqual(normalisePhone(undefined), null);
});

test('the text is short, has the code, and follows the parent\'s language', () => {
  assert.match(consentSms({ language: 'en' }, '123456'), /^SafeTouch code: 123456\./);
  assert.match(consentSms({ language: 'rw' }, '123456'), /^SafeTouch kode: 123456\./);
  assert.ok(consentSms({ language: 'en' }, '123456').length <= 160, 'one SMS part');
});

test('the same code is emailed and texted, and either one verifies', async () => {
  await withServer({}, async ({ call, mailer, sms }) => {
    const res = await call('POST', '/api/consent', consent);
    assert.strictEqual(res.status, 201);
    const body = await res.json();
    assert.strictEqual(body.emailSent, true);
    assert.strictEqual(body.smsSent, true);

    assert.strictEqual(sms.sent.length, 1);
    assert.strictEqual(sms.sent[0].to, '250788123456');
    const smsCode = codeIn(sms.sent[0].text);
    assert.strictEqual(smsCode, codeIn(mailer.sent[0].text), 'one code in both messages');

    const verify = await call('POST', `/api/consent/${consent.consentId}/verify`, { code: smsCode });
    assert.strictEqual((await verify.json()).verified, true);
  });
});

test('with SMS off, only the email goes out', async () => {
  await withServer({ smsEnv: {} }, async ({ call, mailer }) => {
    const body = await (await call('POST', '/api/consent', consent)).json();
    assert.strictEqual(body.emailSent, true);
    assert.strictEqual(body.smsSent, false);
    assert.strictEqual(mailer.sent.length, 1);
  });
});

test('a failing SMS provider does not stop the email', async () => {
  const failing = { provider: 'brevo', async send() { throw new Error('Brevo refused the SMS (402: not enough credits).'); } };
  await withServer({ sms: failing }, async ({ call, mailer }) => {
    const res = await call('POST', '/api/consent', consent);
    assert.strictEqual(res.status, 201);
    const body = await res.json();
    assert.strictEqual(body.emailSent, true);
    assert.strictEqual(body.smsSent, false);
    assert.strictEqual(mailer.sent.length, 1);
  });
});

test('a failing email does not stop the SMS', async () => {
  const failingMailer = { provider: 'brevo', sent: [], async send() { throw new Error('mail down'); } };
  await withServer({ mailer: failingMailer }, async ({ call, sms }) => {
    const body = await (await call('POST', '/api/consent', consent)).json();
    assert.strictEqual(body.emailSent, false);
    assert.strictEqual(body.smsSent, true);
    assert.strictEqual(sms.sent.length, 1);
  });
});

test('resending texts a new code, and the old one stops working', async () => {
  await withServer({}, async ({ call, sms, clock }) => {
    await call('POST', '/api/consent', consent);
    const first = codeIn(sms.sent[0].text);
    clock.t += 61 * 1000;
    const body = await (await call('POST', `/api/consent/${consent.consentId}/resend`, {})).json();
    assert.strictEqual(body.smsSent, true);
    const second = codeIn(sms.sent[1].text);
    assert.notStrictEqual(first, second);
    const old = await call('POST', `/api/consent/${consent.consentId}/verify`, { code: first });
    assert.strictEqual(old.status, 400);
  });
});

test('the SMS does not repeat for a retry of the same details', async () => {
  await withServer({}, async ({ call, sms }) => {
    await call('POST', '/api/consent', consent);
    const again = await (await call('POST', '/api/consent', consent)).json();
    assert.strictEqual(again.smsSent, false);
    assert.strictEqual(sms.sent.length, 1);
  });
});

test('a corrected phone number gets a new code by SMS', async () => {
  await withServer({}, async ({ call, sms }) => {
    await call('POST', '/api/consent', consent);
    const body = await (await call('POST', '/api/consent', { ...consent, phone: '+250 788 999 888' })).json();
    assert.strictEqual(body.smsSent, true);
    assert.strictEqual(sms.sent[1].to, '250788999888');
  });
});

test('one number gets at most 3 texts a day, then the limit lifts', async () => {
  const clock = { t: Date.parse('2026-10-20T08:00:00Z') };
  const sms = createSmsSender({ SMS_PROVIDER: 'console', QUIET_MAILER: '1' }, () => clock.t);
  const text = (n) => sms.send({ phone: '+250 788 123 456', text: `code ${n}` });
  assert.strictEqual(await text(1), true);
  assert.strictEqual(await text(2), true);
  assert.strictEqual(await text(3), true);
  assert.strictEqual(await text(4), false, 'fourth text in a day is refused');
  assert.strictEqual(await sms.send({ phone: '+250 788 000 111', text: 'other number' }), true);
  clock.t += 25 * 60 * 60 * 1000;
  assert.strictEqual(await text(5), true, 'a day later it works again');
});

test('the total daily limit caps the SMS bill', async () => {
  const sms = createSmsSender({ SMS_PROVIDER: 'console', QUIET_MAILER: '1', SMS_DAILY_LIMIT: '2' });
  assert.strictEqual(await sms.send({ phone: '+250 788 000 001', text: 'a' }), true);
  assert.strictEqual(await sms.send({ phone: '+250 788 000 002', text: 'b' }), true);
  assert.strictEqual(await sms.send({ phone: '+250 788 000 003', text: 'c' }), false);
});

test('Brevo gets the number without a plus, the sender name and the text', async () => {
  const calls = [];
  const realFetch = global.fetch;
  global.fetch = async (url, init) => { calls.push({ url, init }); return { ok: true, status: 201 }; };
  try {
    const sms = createSmsSender({ SMS_PROVIDER: 'brevo', SMS_API_KEY: 'k', SMS_SENDER: 'Safe Touch!' });
    assert.strictEqual(await sms.send({ phone: '+250 788 123 456', text: 'hello' }), true);
  } finally { global.fetch = realFetch; }
  assert.strictEqual(calls[0].url, 'https://api.brevo.com/v3/transactionalSMS/send');
  assert.strictEqual(calls[0].init.headers['api-key'], 'k');
  assert.deepStrictEqual(JSON.parse(calls[0].init.body), { sender: 'SafeTouch', recipient: '250788123456', content: 'hello', type: 'transactional' });
});
