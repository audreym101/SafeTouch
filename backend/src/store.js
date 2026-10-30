// Storage for anonymous progress and parental consent.
// MongoDB (Atlas or local) when MONGODB_URI is set, otherwise an in-memory map
// (local development and tests).

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

// MongoDB: two collections, with a unique index on each id:
//   progress  { deviceId, ... }   anonymous lesson results
//   consents  { consentId, ... }  the parent's consent, kept apart from the progress
// Mongo's own _id is never returned, so records look the same as from the other stores.
class MongoStore {
  constructor(db, client) {
    this.client = client;
    this.progress = db.collection('progress');
    this.consents = db.collection('consents');
  }
  async connect() {
    await this.progress.createIndex({ deviceId: 1 }, { unique: true });
    await this.consents.createIndex({ consentId: 1 }, { unique: true });
    return this;
  }
  async close() { if (this.client) await this.client.close(); }

  async upsert(doc) { await this.progress.replaceOne({ deviceId: doc.deviceId }, { ...doc }, { upsert: true }); return doc; }
  async get(deviceId) { return this.progress.findOne({ deviceId }, { projection: { _id: 0 } }); }
  async summary() { return summarise(await this.progress.find({}, { projection: { _id: 0 } }).toArray()); }
  async remove(deviceId) { return (await this.progress.deleteOne({ deviceId })).deletedCount === 1; }

  async saveConsent(record) { await this.consents.replaceOne({ consentId: record.consentId }, { ...record }, { upsert: true }); return record; }
  async getConsent(consentId) { return this.consents.findOne({ consentId }, { projection: { _id: 0 } }); }
  async removeConsent(consentId) { return (await this.consents.deleteOne({ consentId })).deletedCount === 1; }
  async consentCounts() {
    return { total: await this.consents.countDocuments(), verified: await this.consents.countDocuments({ verified: true }) };
  }
}

/** A connected MongoStore for MONGODB_URI (database MONGODB_DB, default "safetouch"). */
async function mongoFromEnv(env) {
  const { MongoClient } = require('mongodb');
  const client = new MongoClient(env.MONGODB_URI, { ignoreUndefined: true, serverSelectionTimeoutMS: 10000 });
  await client.connect();
  return new MongoStore(client.db(env.MONGODB_DB || 'safetouch'), client).connect();
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

async function createStore(env = process.env) {
  if (env.MONGODB_URI) return mongoFromEnv(env);
  return new MemoryStore();
}

module.exports = { createStore, MemoryStore, MongoStore, summarise };
