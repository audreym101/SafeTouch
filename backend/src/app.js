const express = require('express');
const { validateProgress } = require('./validate');

// Builds the Express app around a store, so tests can pass an in-memory one.
function createApp(store) {
  const app = express();
  app.disable('x-powered-by');
  app.use(express.json({ limit: '64kb' }));

  app.get('/api/health', (req, res) => res.json({ status: 'ok' }));

  // Save or replace one device's anonymous progress (called by CloudSyncService.cs).
  app.post('/api/progress', async (req, res, next) => {
    try {
      const errors = validateProgress(req.body);
      if (errors.length) return res.status(400).json({ errors });
      const doc = { ...req.body, receivedAt: new Date().toISOString() };
      await store.upsert(doc);
      res.status(201).json({ saved: true, deviceId: doc.deviceId });
    } catch (err) { next(err); }
  });

  app.get('/api/progress/:deviceId', async (req, res, next) => {
    try {
      const doc = await store.get(req.params.deviceId);
      if (!doc) return res.status(404).json({ error: 'No progress for this device.' });
      res.json(doc);
    } catch (err) { next(err); }
  });

  // A parent can withdraw: switching sync off in the app deletes the record.
  app.delete('/api/progress/:deviceId', async (req, res, next) => {
    try {
      const removed = await store.remove(req.params.deviceId);
      res.status(removed ? 204 : 404).end();
    } catch (err) { next(err); }
  });

  // Totals only, for the evaluation report.
  app.get('/api/summary', async (req, res, next) => {
    try { res.json(await store.summary()); } catch (err) { next(err); }
  });

  app.use((err, req, res, next) => {
    if (err.type === 'entity.parse.failed') return res.status(400).json({ errors: ['Body is not valid JSON.'] });
    console.error(err);
    res.status(500).json({ error: 'Server error.' });
  });

  return app;
}

module.exports = { createApp };
