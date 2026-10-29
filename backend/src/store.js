// Storage for anonymous progress and parental consent.
// Cloud Firestore (Firebase) when credentials are available: always inside
// Cloud Functions, and locally with GOOGLE_APPLICATION_CREDENTIALS,
// SAFETOUCH_SERVICE_ACCOUNT or the Firestore emulator. Otherwise an in-memory
// map (local development and tests).

class MemoryStore {
  constructor() { this.docs = new Map(); this.consents = new Map(); }
  async upsert(doc) { this.docs.set(doc.deviceId, { ...doc }); return doc; }
  async get(deviceId) { return this.docs.get(deviceId) || null; }
  async summary() { return summarise([...this.docs.values()]); }
  async remove(deviceId) { return this.docs.delete(deviceId); }

  // Parental consent records (kept apart from the anonymous progress).
  async saveConsent(record) { this.consents.set(record.consentId, { ...record }); return record; }
  async getConsent(consentId) { const r = this.consents.get(consentId); return r ? { ...r } : null; }
  async removeConsent(consentId) { return this.consents.delete(consentId); }
  async consentCounts() { const all = [...this.consents.values()]; return { total: all.length, verified: all.filter((r) => r.verified).length }; }
}

// Two collections, each document keyed by its id, so no indexes are needed:
//   progress/{deviceId}   anonymous lesson results
//   consents/{consentId}  the parent's consent, kept apart from the progress
// firestore.rules refuses all access from phones and browsers: only this API,
// through the Admin SDK, reads or writes.
class FirestoreStore {
  constructor(db) {
    this.progress = db.collection('progress');
    this.consents = db.collection('consents');
  }
  async upsert(doc) { await this.progress.doc(doc.deviceId).set(doc); return doc; }
  async get(deviceId) { return read(this.progress.doc(deviceId)); }
  async summary() { return summarise((await this.progress.get()).docs.map((d) => d.data())); }
  async remove(deviceId) { return removeDoc(this.progress.doc(deviceId)); }

  async saveConsent(record) { await this.consents.doc(record.consentId).set(record); return record; }
  async getConsent(consentId) { return read(this.consents.doc(consentId)); }
  async removeConsent(consentId) { return removeDoc(this.consents.doc(consentId)); }
  async consentCounts() {
    const total = (await this.consents.count().get()).data().count;
    const verified = (await this.consents.where('verified', '==', true).count().get()).data().count;
    return { total, verified };
  }
}

async function read(ref) {
  const snap = await ref.get();
  return snap.exists ? snap.data() : null;
}

async function removeDoc(ref) {
  const snap = await ref.get();
  if (!snap.exists) return false;
  await ref.delete();
  return true;
}

// Aggregate numbers for the evaluation chapter: no per-device data leaves the API.
function summarise(docs) {
  const byLesson = [1, 2, 3, 4].map((level) => ({
    level,
    completed: docs.filter((d) => (d.completedLevels || []).includes(level)).length,
  }));
  const responses = docs.flatMap((d) => d.scenarioResponses || []);
  const correct = responses.filter((r) => r.isCorrect).length;
  return {
    devices: docs.length,
    byAgeBand: { '6-8': docs.filter((d) => d.ageBand === '6-8').length, '9-11': docs.filter((d) => d.ageBand === '9-11').length },
    byLesson,
    scenarioAnswers: responses.length,
    scenarioAccuracy: responses.length ? Math.round((correct / responses.length) * 100) : null,
  };
}

/** True when this process can reach Firestore: in Cloud Functions, or with local credentials or the emulator. */
function firestoreAvailable(env) {
  if (env.FIRESTORE === 'off') return false;
  return !!(env.FUNCTION_TARGET || env.K_SERVICE || env.GOOGLE_APPLICATION_CREDENTIALS
    || env.SAFETOUCH_SERVICE_ACCOUNT || env.FIRESTORE_EMULATOR_HOST);
}

/** The Admin SDK's Firestore, initialised once. */
function firestoreFromEnv(env) {
  const admin = require('firebase-admin');
  if (!admin.apps.length) {
    // The project comes from the credentials (or GOOGLE_CLOUD_PROJECT with the emulator).
    const options = {};
    if (env.SAFETOUCH_SERVICE_ACCOUNT) options.credential = admin.credential.cert(JSON.parse(env.SAFETOUCH_SERVICE_ACCOUNT));
    admin.initializeApp(options);
    admin.firestore().settings({ ignoreUndefinedProperties: true });
  }
  return admin.firestore();
}

async function createStore(env = process.env) {
  if (firestoreAvailable(env)) return new FirestoreStore(firestoreFromEnv(env));
  return new MemoryStore();
}

module.exports = { createStore, MemoryStore, FirestoreStore, firestoreAvailable, summarise };
