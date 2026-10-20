const fs = require('fs');
const path = require('path');
const { createApp } = require('./app');
const { createStore } = require('./store');
const { createMailer } = require('./mailer');

// Minimal .env loader so the API has no extra dependency.
const envFile = path.join(__dirname, '..', '.env');
if (fs.existsSync(envFile)) {
  for (const line of fs.readFileSync(envFile, 'utf8').split('\n')) {
    const m = line.match(/^\s*([A-Z_]+)\s*=\s*(.*)\s*$/);
    if (m && !process.env[m[1]]) process.env[m[1]] = m[2];
  }
}

(async () => {
  const store = await createStore();
  const port = Number(process.env.PORT) || 3000;
  const mailer = createMailer();
  createApp(store, { mailer }).listen(port, () => {
    console.log(`SafeTouch API on http://localhost:${port} (${process.env.MONGODB_URI ? 'MongoDB Atlas' : 'in-memory store'}, consent emails: ${mailer.provider})`);
  });
})();
