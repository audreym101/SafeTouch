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
const AGE = `/api/consent/${consent.consentId}/age`;
const VERIFY = `${AGE}/verify`;
const codeIn = (text) => text.match(/\b(\d{6})\b/)[1];

async function withParent({ verified = true, sms = false } = {}, fn) {
  const clock = { t: Date.parse('2026-10-20T08:00:00Z') };
  const mailer = createMailer({ MAIL_PROVIDER: 'console', QUIET_MAILER: '1' });
  const texts = createSmsSender(sms ? { SMS_PROVIDER: 'console', QUIET_MAILER: '1' } : {}, () => clock.t);
  const store = new MemoryStore();
  const server = createApp(store, { mailer, sms: texts, now: () => clock.t }).listen(0);
  const base = `http://127.0.0.1:${server.address().port}`;
  const call = (method, path, body) => fetch(`${base}${path}`, {
    method, headers: { 'Content-Type': 'application/json' }, body: body === undefined ? undefined : JSON.stringify(body),
  });
  try {
    await call('POST', '/api/consent', consent);
    if (verified) await call('POST', `/api/consent/${consent.consentId}/verify`, { code: codeIn(mailer.sent[0].text) });
    mailer.sent.length = 0;
    if (texts.sent) texts.sent.length = 0;
    await fn({ call, mailer, texts, clock, store });
  } finally { server.close(); }
}

test('the parent gets a code for a child aged 9 to 11, and the code opens the Guardian lessons', async () => {
  await withParent({}, async ({ call, mailer, store }) => {
    const asked = await call('POST', AGE, { ageYears: 10 });
    assert.strictEqual(asked.status, 200);
    const body = await asked.json();
    assert.strictEqual(body.sent, true);
    assert.strictEqual(body.emailMasked, 'u***e@example.com');
    assert.strictEqual(mailer.sent.length, 1);
    assert.match(mailer.sent[0].subject, /confirm your child's age/);
    assert.match(mailer.sent[0].text, /10 years old/);
    assert.strictEqual(mailer.sent[0].to, 'uwase.marie@example.com');

    const res = await call('POST', VERIFY, { code: codeIn(mailer.sent[0].text) });
    assert.strictEqual(res.status, 200);
    assert.deepStrictEqual(await res.json(), { ageConfirmed: true, ageBand: '9-11' });
    const stored = await store.getConsent(consent.consentId);
    assert.strictEqual(stored.ageBand, '9-11');
    assert.ok(stored.ageConfirmedAt);
    assert.strictEqual(stored.ageCodeHash, undefined, 'the code is gone once used');
  });
});

test('only ages 9 to 11 get a code; younger children are pointed to the Explorer lessons', async () => {
  await withParent({}, async ({ call, mailer }) => {
    const young = await call('POST', AGE, { ageYears: 7 });
    assert.strictEqual(young.status, 400);
    const body = await young.json();
    assert.match(body.errors[0], /ages 9 to 11/);
    assert.strictEqual(body.ageBand, '6-8');
    assert.strictEqual((await call('POST', AGE, { ageYears: 12 })).status, 400);
    assert.strictEqual((await call('POST', AGE, { ageYears: 8 })).status, 400);
    assert.strictEqual((await call('POST', AGE, { ageYears: 9 })).status, 200);
    assert.strictEqual(mailer.sent.length, 1, 'only the valid age sent an email');
  });
});

test('a birth date, or anything else, is refused: only the age in years is accepted', async () => {
  await withParent({}, async ({ call, mailer }) => {
    const res = await call('POST', AGE, { ageYears: 10, birthDate: '2016-03-04' });
    assert.strictEqual(res.status, 400);
    assert.match((await res.json()).errors[0], /birthDate/);
    assert.strictEqual((await call('POST', AGE, { birthDate: '2016-03-04' })).status, 400);
    assert.strictEqual((await call('POST', AGE, { ageYears: '10' })).status, 400, 'a string is not a number');
    assert.strictEqual((await call('POST', AGE, { ageYears: 10.5 })).status, 400);
    assert.strictEqual((await call('POST', AGE, {})).status, 400);
    assert.strictEqual(mailer.sent.length, 0);
  });
});

test('the parent must have confirmed their email first', async () => {
  await withParent({ verified: false }, async ({ call, mailer }) => {
    assert.strictEqual((await call('POST', AGE, { ageYears: 10 })).status, 403);
    assert.strictEqual(mailer.sent.length, 0);
  });
});

test('an unknown consent and a bad id are refused', async () => {
  await withParent({}, async ({ call }) => {
    assert.strictEqual((await call('POST', '/api/consent/11111111-2222-4333-8444-555555555555/age', { ageYears: 10 })).status, 404);
    assert.strictEqual((await call('POST', '/api/consent/nope/age', { ageYears: 10 })).status, 400);
    assert.strictEqual((await call('POST', '/api/consent/nope/age/verify', { code: '123456' })).status, 400);
  });
});

test('a wrong code is refused, 5 tries per code, and a new code starts again', async () => {
  await withParent({}, async ({ call, mailer, clock }) => {
    await call('POST', AGE, { ageYears: 10 });
    const good = codeIn(mailer.sent[0].text);
    const wrong = good === '000000' ? '111111' : '000000';
    const first = await call('POST', VERIFY, { code: wrong });
    assert.strictEqual(first.status, 400);
    assert.strictEqual((await first.json()).attemptsLeft, 4);
    for (let i = 0; i < 4; i++) await call('POST', VERIFY, { code: wrong });
    assert.strictEqual((await call('POST', VERIFY, { code: good })).status, 429, 'locked out even with the right code');
    clock.t += 61 * 1000;
    await call('POST', AGE, { ageYears: 10 });
    assert.strictEqual((await call('POST', VERIFY, { code: codeIn(mailer.sent[1].text) })).status, 200);
  });
});

test('an age code lasts 24 hours and a code that was never asked for is refused', async () => {
  await withParent({}, async ({ call, mailer, clock }) => {
    assert.strictEqual((await call('POST', VERIFY, { code: '123456' })).status, 410, 'no code was asked for');
    await call('POST', AGE, { ageYears: 11 });
    const code = codeIn(mailer.sent[0].text);
    clock.t += 23 * 60 * 60 * 1000;
    const stillGood = await call('POST', VERIFY, { code });
    assert.strictEqual(stillGood.status, 200, 'still valid after 23 hours');
  });
  await withParent({}, async ({ call, mailer, clock }) => {
    await call('POST', AGE, { ageYears: 11 });
    const code = codeIn(mailer.sent[0].text);
    clock.t += 24 * 60 * 60 * 1000 + 1000;
    assert.strictEqual((await call('POST', VERIFY, { code })).status, 410);
  });
});

test('codes can be asked for at most once a minute and 10 times a day', async () => {
  await withParent({}, async ({ call, mailer, clock }) => {
    assert.strictEqual((await call('POST', AGE, { ageYears: 10 })).status, 200);
    assert.strictEqual((await call('POST', AGE, { ageYears: 10 })).status, 429);
    for (let i = 1; i < 10; i++) {
      clock.t += 61 * 1000;
      assert.strictEqual((await call('POST', AGE, { ageYears: 10 })).status, 200, `code ${i + 1}`);
    }
    clock.t += 61 * 1000;
    assert.strictEqual((await call('POST', AGE, { ageYears: 10 })).status, 429);
    assert.strictEqual(mailer.sent.length, 10);
  });
});

test('the age code is separate from the consent and login codes', async () => {
  await withParent({}, async ({ call, mailer, clock }) => {
    await call('POST', AGE, { ageYears: 10 });
    const ageCode = codeIn(mailer.sent[0].text);
    // The age code does not log anyone in.
    assert.strictEqual((await call('POST', `/api/consent/${consent.consentId}/login/verify`, { code: ageCode })).status, 410);
    // A login code does not confirm an age.
    clock.t += 61 * 1000;
    await call('POST', `/api/consent/${consent.consentId}/login`, { email: consent.email });
    const loginCode = codeIn(mailer.sent[1].text);
    const wrong = loginCode === ageCode ? '654321' : loginCode;
    if (wrong !== ageCode) assert.strictEqual((await call('POST', VERIFY, { code: wrong })).status, 400);
  });
});

test('the code is also texted when SMS is on, and the text alone is enough if the email fails', async () => {
  await withParent({ sms: true }, async ({ call, mailer, texts }) => {
    await call('POST', AGE, { ageYears: 10 });
    assert.strictEqual(texts.sent.length, 1);
    assert.match(texts.sent[0].text, /age code/);
    assert.strictEqual(codeIn(texts.sent[0].text), codeIn(mailer.sent[0].text));
    assert.strictEqual((await call('POST', VERIFY, { code: codeIn(texts.sent[0].text) })).status, 200);
  });
  const failingMailer = { provider: 'brevo', sent: [], async send() { throw new Error('mail down'); } };
  const sms = createSmsSender({ SMS_PROVIDER: 'console', QUIET_MAILER: '1' });
  const store = new MemoryStore();
  await store.saveConsent({ ...consent, verified: true });
  const server = createApp(store, { mailer: failingMailer, sms }).listen(0);
  try {
    const url = `http://127.0.0.1:${server.address().port}${AGE}`;
    const post = (body) => fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });
    assert.strictEqual((await post({ ageYears: 10 })).status, 200, 'the text alone is enough');
    assert.strictEqual(sms.sent.length, 1);
  } finally { server.close(); }
});

test('when neither the email nor the text can be sent, the app is told', async () => {
  const failingMailer = { provider: 'brevo', sent: [], async send() { throw new Error('mail down'); } };
  const store = new MemoryStore();
  await store.saveConsent({ ...consent, verified: true });
  const server = createApp(store, { mailer: failingMailer, sms: createSmsSender({}) }).listen(0);
  try {
    const res = await fetch(`http://127.0.0.1:${server.address().port}${AGE}`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ ageYears: 10 }),
    });
    assert.strictEqual(res.status, 502);
  } finally { server.close(); }
});

test('asking again once the age is confirmed sends nothing', async () => {
  await withParent({}, async ({ call, mailer, clock }) => {
    await call('POST', AGE, { ageYears: 10 });
    await call('POST', VERIFY, { code: codeIn(mailer.sent[0].text) });
    clock.t += 61 * 1000;
    const again = await (await call('POST', AGE, { ageYears: 10 })).json();
    assert.deepStrictEqual(again, { sent: false, ageConfirmed: true });
    assert.strictEqual(mailer.sent.length, 1);
  });
});

test('the status never shows age codes, and withdrawing the consent removes the confirmation', async () => {
  await withParent({}, async ({ call, mailer, store }) => {
    await call('POST', AGE, { ageYears: 10 });
    const code = codeIn(mailer.sent[0].text);
    const status = await (await call('GET', `/api/consent/${consent.consentId}`)).json();
    assert.ok(!JSON.stringify(status).includes(code));
    await call('POST', VERIFY, { code });
    assert.strictEqual((await call('DELETE', `/api/consent/${consent.consentId}`)).status, 204);
    assert.strictEqual(await store.getConsent(consent.consentId), null);
  });
});
