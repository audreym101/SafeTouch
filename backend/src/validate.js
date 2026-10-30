// Validates the anonymous payload sent by CloudSyncService.cs.
// Anything that could identify a child (name, photo, location, free text) is rejected.

const AGE_BANDS = ['6-8', '9-11'];
const LANGUAGES = ['en', 'rw'];
const ANSWERS = ['A', 'B', 'C'];
const LESSON_COUNT = 5; // four lessons + the Memory Quest (ProgressManager.LessonCount)
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const ID = /^[a-z0-9_]{1,32}$/i;
const ALLOWED_KEYS = new Set([
  'deviceId', 'ageBand', 'language', 'completedLevels', 'quizScores',
  'badges', 'trustedAdultRoles', 'scenarioResponses',
]);

function isIntList(v, min, max) {
  return Array.isArray(v) && v.length <= 50 && v.every((n) => Number.isInteger(n) && n >= min && n <= max);
}

function isIdList(v) {
  return Array.isArray(v) && v.length <= 50 && v.every((s) => typeof s === 'string' && ID.test(s));
}

function validateProgress(body) {
  const errors = [];
  if (!body || typeof body !== 'object' || Array.isArray(body)) return ['Body must be a JSON object.'];

  for (const key of Object.keys(body)) {
    if (!ALLOWED_KEYS.has(key)) errors.push(`Field "${key}" is not accepted.`);
  }
  if (typeof body.deviceId !== 'string' || !UUID.test(body.deviceId)) errors.push('deviceId must be a random UUID.');
  if (!AGE_BANDS.includes(body.ageBand)) errors.push('ageBand must be "6-8" or "9-11".');
  if (body.language !== undefined && !LANGUAGES.includes(body.language)) errors.push('language must be "en" or "rw".');
  if (body.completedLevels !== undefined && !isIntList(body.completedLevels, 1, LESSON_COUNT)) errors.push(`completedLevels must be lesson numbers 1-${LESSON_COUNT}.`);
  if (body.quizScores !== undefined && !isIntList(body.quizScores, 0, 100)) errors.push('quizScores must be whole numbers 0-100.');
  if (body.badges !== undefined && !isIdList(body.badges)) errors.push('badges must be badge ids.');
  if (body.trustedAdultRoles !== undefined && !isIdList(body.trustedAdultRoles)) errors.push('trustedAdultRoles must be role ids, not names.');

  if (body.scenarioResponses !== undefined) {
    const ok = Array.isArray(body.scenarioResponses) && body.scenarioResponses.length <= 500 &&
      body.scenarioResponses.every((r) => r && /^S\d{2}$/.test(r.scenarioId) && ANSWERS.includes(r.selectedAnswer) &&
        typeof r.isCorrect === 'boolean' && !Number.isNaN(Date.parse(r.answeredAt)));
    if (!ok) errors.push('scenarioResponses must list {scenarioId, selectedAnswer, isCorrect, answeredAt}.');
  }
  return errors;
}

module.exports = { validateProgress };

// ---- Parental consent (ConsentService.cs) ----
// The parent's own contact details, given with their agreement, to verify the
// consent by email (Law N° 058/2021, Article 9). Nothing about the child.

const CONSENT_KEYS = new Set([
  'consentId', 'deviceId', 'parentName', 'phone', 'email', 'country', 'language', 'termsVersion', 'agreements', 'terms',
]);
const AGREEMENTS = ['guardian', 'terms', 'childUse', 'dataUse'];
const NAME = /^[\p{L}][\p{L}\p{M} .'-]{1,79}$/u;
const PHONE = /^\+?[0-9][0-9 ()-]{6,19}$/;
const EMAIL = /^[^\s@<>()[\]\,;:"]+@[^\s@<>()[\]\,;:"]+\.[A-Za-z]{2,}$/;
const COUNTRY = /^[\p{L}][\p{L}\p{M} .'()-]{1,55}$/u;
const VERSION = /^[0-9A-Za-z.-]{1,20}$/;

function validateConsent(body) {
  const errors = [];
  if (!body || typeof body !== 'object' || Array.isArray(body)) return ['Body must be a JSON object.'];
  for (const key of Object.keys(body)) {
    if (!CONSENT_KEYS.has(key)) errors.push(`Field "${key}" is not accepted.`);
  }
  if (typeof body.consentId !== 'string' || !UUID.test(body.consentId)) errors.push('consentId must be a random UUID.');
  if (typeof body.deviceId !== 'string' || !UUID.test(body.deviceId)) errors.push('deviceId must be a random UUID.');
  if (typeof body.parentName !== 'string' || !NAME.test(body.parentName.trim())) errors.push('parentName must be the parent\'s name (2-80 letters).');
  const digits = typeof body.phone === 'string' ? body.phone.replace(/\D/g, '') : '';
  if (typeof body.phone !== 'string' || !PHONE.test(body.phone.trim()) || digits.length < 8 || digits.length > 15) {
    errors.push('phone must be a phone number with its country code, e.g. +250 788 123 456.');
  }
  if (typeof body.email !== 'string' || body.email.length > 254 || !EMAIL.test(body.email.trim())) errors.push('email must be a valid email address.');
  if (typeof body.country !== 'string' || !COUNTRY.test(body.country.trim())) errors.push('country must be a country name.');
  if (body.language !== undefined && !LANGUAGES.includes(body.language)) errors.push('language must be "en" or "rw".');
  if (typeof body.termsVersion !== 'string' || !VERSION.test(body.termsVersion)) errors.push('termsVersion must be the version of the terms shown.');
  // The terms and privacy notice exactly as the parent read them in the app (titles and text), so the email
  // and the record show what was agreed to. Optional for older app versions (Unity sends an empty list as []).
  if (body.terms !== undefined) {
    const t = body.terms;
    const ok = Array.isArray(t) && t.length <= 20 &&
      t.every((s) => s && typeof s === 'object' && !Array.isArray(s) && Object.keys(s).every((k) => ['termId', 'title', 'body'].includes(k)) &&
        typeof s.title === 'string' && s.title.trim().length >= 1 && s.title.length <= 200 &&
        typeof s.body === 'string' && s.body.length <= 4000 && (s.termId === undefined || (typeof s.termId === 'string' && (s.termId === '' || ID.test(s.termId))))) &&
      t.reduce((n, s) => n + s.title.length + s.body.length, 0) <= 30000;
    if (!ok) errors.push('terms must list up to 20 sections of {title, body} as shown in the app.');
  }
  const a = body.agreements;
  if (!a || typeof a !== 'object' || Array.isArray(a) || !AGREEMENTS.every((k) => a[k] === true) || Object.keys(a).some((k) => !AGREEMENTS.includes(k))) {
    errors.push(`agreements must confirm ${AGREEMENTS.join(', ')} (all true).`);
  }
  return errors;
}

module.exports.validateConsent = validateConsent;
module.exports.AGREEMENTS = AGREEMENTS;
