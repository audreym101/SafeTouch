// Validates the anonymous payload sent by CloudSyncService.cs.
// Anything that could identify a child (name, photo, location, free text) is rejected.

const AGE_BANDS = ['6-8', '9-11'];
const LANGUAGES = ['en', 'rw'];
const ANSWERS = ['A', 'B', 'C'];
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
  if (body.completedLevels !== undefined && !isIntList(body.completedLevels, 1, 4)) errors.push('completedLevels must be lesson numbers 1-4.');
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
