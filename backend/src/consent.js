// Parental consent: a parent agrees to the terms, gives their name, phone,
// email and country, and verifies by typing the 6-digit code emailed (and, when
// an SMS provider is set, texted) to them.
// The child can use the app before that (a 7-day grace period in the app);
// verification proves it was the parent who agreed (Law N° 058/2021, Article 9).
//
//   POST   /api/consent                     submit (or correct) a consent, email a code
//   POST   /api/consent/:consentId/verify   { code } -> verified
//   POST   /api/consent/:consentId/resend   new code (at most once a minute, 5 a day)
//   POST   /api/consent/:consentId/login    { email } -> a login code by email (and SMS) when the email matches
//   POST   /api/consent/:consentId/login/verify  { code } -> { loggedIn: true }
//   POST   /api/consent/:consentId/age      { ageYears: 9..11 } -> a code to the parent to confirm the child's age
//   POST   /api/consent/:consentId/age/verify  { code } -> { ageConfirmed: true }: the Guardian (9-11) lessons may open
//   GET    /api/consent/:consentId          status only: no name, phone or full email
//   DELETE /api/consent/:consentId          withdraw: deletes the consent and the device's progress

const crypto = require('crypto');
const { validateConsent } = require('./validate');
const { consentEmail, loginEmail, ageEmail } = require('./mailer');
const { consentSms, loginSms, ageSms } = require('./sms');

const CODE_TTL_MS = 24 * 60 * 60 * 1000;
const LOGIN_TTL_MS = 10 * 60 * 1000;   // a login code is for now, not for tomorrow
const MAX_LOGINS_PER_DAY = 10;
const EMAIL = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const MAX_ATTEMPTS = 5;
const RESEND_GAP_MS = 60 * 1000;
const MAX_SENDS_PER_DAY = 5;
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function hashCode(consentId, code) {
  return crypto.createHash('sha256').update(`${consentId}:${code}`).digest('hex');
}

function newCode() {
  return String(crypto.randomInt(0, 1000000)).padStart(6, '0');
}

/** "uwase@example.com" -> "u***e@example.com": enough for the parent to recognise, not enough to read. */
function maskEmail(email) {
  const [user, domain] = String(email).split('@');
  if (!domain) return '***';
  const shown = user.length <= 2 ? user[0] : `${user[0]}***${user[user.length - 1]}`;
  return `${shown}@${domain}`;
}

function status(record) {
  return { consentId: record.consentId, verified: !!record.verified, verifiedAt: record.verifiedAt || null, emailMasked: maskEmail(record.email) };
}

function registerConsentRoutes(app, store, mailer, now = () => Date.now(), sms = { async send() { return false; } }) {
  // Creates a code, keeps only its hash, and emails it with the consent copy.
  // The same code is texted to the parent's phone when an SMS provider is set;
  // either channel can fail without stopping the other. Returns what was sent.
  async function sendCode(record) {
    const code = newCode();
    const t = now();
    const dayAgo = t - 24 * 60 * 60 * 1000;
    record.sends = (record.sends || []).filter((s) => s > dayAgo).concat(t);
    record.codeHash = hashCode(record.consentId, code);
    record.codeExpiresAt = t + CODE_TTL_MS;
    record.attempts = 0;
    await store.saveConsent(record);
    const [emailSent, smsSent] = await Promise.all([
      (async () => {
        try {
          await mailer.send({ to: record.email, toName: record.parentName, ...consentEmail(record, code) });
          return true;
        } catch (err) { console.error('Consent email failed:', err.message); return false; }
      })(),
      (async () => {
        try { return await sms.send({ phone: record.phone, country: record.country, text: consentSms(record, code) }); }
        catch (err) { console.error('Consent SMS failed:', err.message); return false; }
      })(),
    ]);
    return { emailSent, smsSent };
  }

  function findOr404(handler) {
    return async (req, res, next) => {
      try {
        if (!UUID.test(req.params.consentId)) return res.status(400).json({ errors: ['consentId must be a UUID.'] });
        const record = await store.getConsent(req.params.consentId);
        if (!record) return res.status(404).json({ error: 'No consent with this id.' });
        await handler(record, req, res);
      } catch (err) { next(err); }
    };
  }

  app.post('/api/consent', async (req, res, next) => {
    try {
      const errors = validateConsent(req.body);
      if (errors.length) return res.status(400).json({ errors });
      const b = req.body;
      const existing = await store.getConsent(b.consentId);
      if (existing && existing.verified) return res.status(200).json({ ...status(existing), emailSent: false, smsSent: false });
      // The app retries while offline: the same details again need no second email.
      if (existing && existing.email === b.email.trim() && existing.phone === b.phone.trim()) return res.status(200).json({ ...status(existing), emailSent: false, smsSent: false });

      const record = {
        ...(existing || {}),
        consentId: b.consentId,
        deviceId: b.deviceId,
        parentName: b.parentName.trim(),
        phone: b.phone.trim(),
        email: b.email.trim(),
        country: b.country.trim(),
        language: b.language || 'en',
        termsVersion: b.termsVersion,
        agreements: { ...b.agreements },
        submittedAt: (existing && existing.submittedAt) || new Date(now()).toISOString(),
        verified: false,
      };
      const sent = await sendCode(record);
      res.status(existing ? 200 : 201).json({ ...status(record), ...sent });
    } catch (err) { next(err); }
  });

  app.post('/api/consent/:consentId/verify', findOr404(async (record, req, res) => {
    if (record.verified) return res.json(status(record));
    const code = req.body && typeof req.body.code === 'string' ? req.body.code.replace(/\s/g, '') : '';
    if (!/^\d{6}$/.test(code)) return res.status(400).json({ errors: ['code must be the 6 digits from the email or text.'] });
    if ((record.attempts || 0) >= MAX_ATTEMPTS) return res.status(429).json({ error: 'Too many tries. Ask for a new code.' });
    if (!record.codeExpiresAt || now() > record.codeExpiresAt) return res.status(410).json({ error: 'This code has expired. Ask for a new code.' });
    const given = Buffer.from(hashCode(record.consentId, code), 'hex');
    const expected = Buffer.from(record.codeHash || '', 'hex');
    if (given.length !== expected.length || !crypto.timingSafeEqual(given, expected)) {
      record.attempts = (record.attempts || 0) + 1;
      await store.saveConsent(record);
      return res.status(400).json({ errors: ['That code is not right.'], attemptsLeft: Math.max(0, MAX_ATTEMPTS - record.attempts) });
    }
    record.verified = true;
    record.verifiedAt = new Date(now()).toISOString();
    delete record.codeHash;
    delete record.codeExpiresAt;
    await store.saveConsent(record);
    res.json(status(record));
  }));

  app.post('/api/consent/:consentId/resend', findOr404(async (record, req, res) => {
    if (record.verified) return res.json({ ...status(record), emailSent: false, smsSent: false });
    const sends = (record.sends || []).filter((s) => s > now() - 24 * 60 * 60 * 1000);
    if (sends.length && now() - sends[sends.length - 1] < RESEND_GAP_MS) return res.status(429).json({ error: 'Please wait a minute before asking again.' });
    if (sends.length >= MAX_SENDS_PER_DAY) return res.status(429).json({ error: 'Too many emails today. Try again tomorrow.' });
    const sent = await sendCode(record);
    res.json({ ...status(record), ...sent });
  }));

  // Parent login: every time the app opens, the parent types their email and then a fresh code.
  // The reply is the same whether or not the email matches, so the API can't be used to find out
  // which emails are registered; the code only goes to the email on record.
  app.post('/api/consent/:consentId/login', async (req, res, next) => {
    try {
      if (!UUID.test(req.params.consentId)) return res.status(400).json({ errors: ['consentId must be a UUID.'] });
      const email = req.body && typeof req.body.email === 'string' ? req.body.email.trim().toLowerCase() : '';
      if (!EMAIL.test(email)) return res.status(400).json({ errors: ['email must be an email address.'] });
      const record = await store.getConsent(req.params.consentId);
      if (!record || !record.verified || String(record.email).toLowerCase() !== email) return res.json({ sent: true });

      const t = now();
      const sends = (record.loginSends || []).filter((s) => s > t - 24 * 60 * 60 * 1000);
      if (sends.length && t - sends[sends.length - 1] < RESEND_GAP_MS) return res.status(429).json({ error: 'Please wait a minute before asking again.' });
      if (sends.length >= MAX_LOGINS_PER_DAY) return res.status(429).json({ error: 'Too many codes today. Try again tomorrow.' });

      const code = newCode();
      record.loginSends = sends.concat(t);
      record.loginCodeHash = hashCode(`login-${record.consentId}`, code);
      record.loginExpiresAt = t + LOGIN_TTL_MS;
      record.loginAttempts = 0;
      await store.saveConsent(record);
      const [emailSent, smsSent] = await Promise.all([
        (async () => {
          try { await mailer.send({ to: record.email, toName: record.parentName, ...loginEmail(record, code) }); return true; }
          catch (err) { console.error('Login email failed:', err.message); return false; }
        })(),
        (async () => {
          try { return await sms.send({ phone: record.phone, country: record.country, text: loginSms(record, code) }); }
          catch (err) { console.error('Login SMS failed:', err.message); return false; }
        })(),
      ]);
      if (!emailSent && !smsSent) return res.status(502).json({ error: 'Could not send the code. Try again later.' });
      res.json({ sent: true });
    } catch (err) { next(err); }
  });

  app.post('/api/consent/:consentId/login/verify', findOr404(async (record, req, res) => {
    const code = req.body && typeof req.body.code === 'string' ? req.body.code.replace(/\s/g, '') : '';
    if (!/^\d{6}$/.test(code)) return res.status(400).json({ errors: ['code must be the 6 digits from the email or text.'] });
    if (!record.loginCodeHash || !record.loginExpiresAt || now() > record.loginExpiresAt) return res.status(410).json({ error: 'This code has expired. Ask for a new code.' });
    if ((record.loginAttempts || 0) >= MAX_ATTEMPTS) return res.status(429).json({ error: 'Too many tries. Ask for a new code.' });
    const given = Buffer.from(hashCode(`login-${record.consentId}`, code), 'hex');
    const expected = Buffer.from(record.loginCodeHash, 'hex');
    if (given.length !== expected.length || !crypto.timingSafeEqual(given, expected)) {
      record.loginAttempts = (record.loginAttempts || 0) + 1;
      await store.saveConsent(record);
      return res.status(400).json({ errors: ['That code is not right.'], attemptsLeft: Math.max(0, MAX_ATTEMPTS - record.loginAttempts) });
    }
    delete record.loginCodeHash;
    delete record.loginExpiresAt;
    delete record.loginAttempts;
    record.lastLoginAt = new Date(now()).toISOString();
    await store.saveConsent(record);
    res.json({ loggedIn: true, ...status(record) });
  }));

  // The Guardian lessons (ages 9-11) talk openly about online strangers and private parts of the body,
  // so a grown-up confirms the child's age first. The app sends only the age in whole years, worked out
  // on the phone from a date that is never kept or sent: no birth date, and nothing about the child,
  // reaches the API. The code goes to the parent's email (and phone) on record.
  app.post('/api/consent/:consentId/age', async (req, res, next) => {
    try {
      if (!UUID.test(req.params.consentId)) return res.status(400).json({ errors: ['consentId must be a UUID.'] });
      const extra = Object.keys(req.body || {}).filter((k) => k !== 'ageYears');
      if (extra.length) return res.status(400).json({ errors: [`Unknown field: ${extra[0]}. Only ageYears is accepted: no birth date is kept.`] });
      const age = req.body ? req.body.ageYears : undefined;
      if (!Number.isInteger(age)) return res.status(400).json({ errors: ['ageYears must be a whole number.'] });
      if (age < 9 || age > 11) return res.status(400).json({ errors: ['The Guardian lessons are for ages 9 to 11.'], ageBand: age < 9 ? '6-8' : null });
      const record = await store.getConsent(req.params.consentId);
      if (!record) return res.status(404).json({ error: 'No consent with this id.' });
      if (!record.verified) return res.status(403).json({ error: 'Please confirm your email first.' });
      if (record.ageBand === '9-11') return res.json({ sent: false, ageConfirmed: true });

      const t = now();
      const sends = (record.ageSends || []).filter((s) => s > t - 24 * 60 * 60 * 1000);
      if (sends.length && t - sends[sends.length - 1] < RESEND_GAP_MS) return res.status(429).json({ error: 'Please wait a minute before asking again.' });
      if (sends.length >= MAX_LOGINS_PER_DAY) return res.status(429).json({ error: 'Too many codes today. Try again tomorrow.' });

      const code = newCode();
      record.ageSends = sends.concat(t);
      record.ageCodeHash = hashCode(`age-${record.consentId}`, code);
      record.ageCodeExpiresAt = t + CODE_TTL_MS;
      record.ageAttempts = 0;
      await store.saveConsent(record);
      const [emailSent, smsSent] = await Promise.all([
        (async () => {
          try { await mailer.send({ to: record.email, toName: record.parentName, ...ageEmail(record, age, code) }); return true; }
          catch (err) { console.error('Age email failed:', err.message); return false; }
        })(),
        (async () => {
          try { return await sms.send({ phone: record.phone, country: record.country, text: ageSms(record, code) }); }
          catch (err) { console.error('Age SMS failed:', err.message); return false; }
        })(),
      ]);
      if (!emailSent && !smsSent) return res.status(502).json({ error: 'Could not send the code. Try again later.' });
      res.json({ sent: true, emailMasked: maskEmail(record.email) });
    } catch (err) { next(err); }
  });

  app.post('/api/consent/:consentId/age/verify', findOr404(async (record, req, res) => {
    const code = req.body && typeof req.body.code === 'string' ? req.body.code.replace(/\s/g, '') : '';
    if (!/^\d{6}$/.test(code)) return res.status(400).json({ errors: ['code must be the 6 digits from the email or text.'] });
    if (!record.ageCodeHash || !record.ageCodeExpiresAt || now() > record.ageCodeExpiresAt) return res.status(410).json({ error: 'This code has expired. Ask for a new code.' });
    if ((record.ageAttempts || 0) >= MAX_ATTEMPTS) return res.status(429).json({ error: 'Too many tries. Ask for a new code.' });
    const given = Buffer.from(hashCode(`age-${record.consentId}`, code), 'hex');
    const expected = Buffer.from(record.ageCodeHash, 'hex');
    if (given.length !== expected.length || !crypto.timingSafeEqual(given, expected)) {
      record.ageAttempts = (record.ageAttempts || 0) + 1;
      await store.saveConsent(record);
      return res.status(400).json({ errors: ['That code is not right.'], attemptsLeft: Math.max(0, MAX_ATTEMPTS - record.ageAttempts) });
    }
    delete record.ageCodeHash;
    delete record.ageCodeExpiresAt;
    delete record.ageAttempts;
    record.ageBand = '9-11';
    record.ageConfirmedAt = new Date(now()).toISOString();
    await store.saveConsent(record);
    res.json({ ageConfirmed: true, ageBand: record.ageBand });
  }));

  app.get('/api/consent/:consentId', findOr404(async (record, req, res) => res.json(status(record))));

  // Withdrawing consent removes the parent's details and the device's anonymous progress.
  app.delete('/api/consent/:consentId', findOr404(async (record, req, res) => {
    await store.removeConsent(record.consentId);
    await store.remove(record.deviceId);
    res.status(204).end();
  }));
}

module.exports = { registerConsentRoutes, maskEmail, hashCode, MAX_ATTEMPTS };
