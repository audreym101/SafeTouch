const fs = require('fs');
const path = require('path');
const { createApp } = require('./app');
const { createStore, FirestoreStore, MongoStore } = require('./store');
const { createMailer } = require('./mailer');

// Minimal .env loader so the API has no extra dependency. .env.local (laptop
// only: port, credentials, email key) wins over .env (which is also deployed
// with the Cloud Function).
for (const name of ['.env.local', '.env']) {
  const envFile = path.join(__dirname, '..', name);
  if (!fs.existsSync(envFile)) continue;
  for (const line of fs.readFileSync(envFile, 'utf8').split('\n')) {
    const m = line.match(/^\s*([A-Z_]+)\s*=\s*(.*?)\s*$/);
    if (m && m[2] !== '' && !process.env[m[1]]) process.env[m[1]] = m[2];
  }
}

(async () => {
  const store = await createStore();
  const where = store instanceof MongoStore ? `MongoDB database "${process.env.MONGODB_DB || 'safetouch'}"`
    : !(store instanceof FirestoreStore) ? 'in-memory store'
    : process.env.FIRESTORE_EMULATOR_HOST ? `Firestore emulator at ${process.env.FIRESTORE_EMULATOR_HOST}` : 'Cloud Firestore';
  const port = Number(process.env.PORT) || 3000;
  const mailer = createMailer();
  createApp(store, { mailer }).listen(port, () => {
    console.log(`SafeTouch API on http://localhost:${port} (${where}, consent emails: ${mailer.provider})`);
  });
})();
