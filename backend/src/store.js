// Storage for anonymous progress. MongoDB Atlas when MONGODB_URI is set,
// otherwise an in-memory map (local development and tests).

class MemoryStore {
  constructor() { this.docs = new Map(); }
  async upsert(doc) { this.docs.set(doc.deviceId, { ...doc }); return doc; }
  async get(deviceId) { return this.docs.get(deviceId) || null; }
  async summary() { return summarise([...this.docs.values()]); }
  async remove(deviceId) { return this.docs.delete(deviceId); }
}

class MongoStore {
  constructor(uri, dbName) {
    const { MongoClient } = require('mongodb');
    this.client = new MongoClient(uri);
    this.dbName = dbName;
  }
  async connect() {
    await this.client.connect();
    this.col = this.client.db(this.dbName).collection('progress');
    await this.col.createIndex({ deviceId: 1 }, { unique: true });
    return this;
  }
  async upsert(doc) {
    await this.col.replaceOne({ deviceId: doc.deviceId }, doc, { upsert: true });
    return doc;
  }
  async get(deviceId) { return this.col.findOne({ deviceId }, { projection: { _id: 0 } }); }
  async summary() { return summarise(await this.col.find({}, { projection: { _id: 0 } }).toArray()); }
  async remove(deviceId) { return (await this.col.deleteOne({ deviceId })).deletedCount === 1; }
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
  if (env.MONGODB_URI) return new MongoStore(env.MONGODB_URI, env.MONGODB_DB || 'safetouch').connect();
  return new MemoryStore();
}

module.exports = { createStore, MemoryStore, summarise };
