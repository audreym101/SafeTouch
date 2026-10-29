// Firebase Cloud Functions entry point: the same Express API as server.js,
// served at https://<region>-<project>.cloudfunctions.net/safetouch, which is the
// Base Url to put on ConsentService and CloudSyncService in Unity.
//
// Settings come from backend/.env when deploying (API_REGION, MAIL_PROVIDER,
// MAIL_FROM). Firebase reserves PORT, FUNCTION_* and FIREBASE_* there, so
// laptop-only settings go in .env.local, which is never deployed. The email API key is a secret, never in .env:
//   firebase functions:secrets:set MAIL_API_KEY
// It is only required once MAIL_PROVIDER is brevo or resend; with the default
// "console" provider, consent emails are written to the function's logs.

const { onRequest } = require('firebase-functions/v2/https');
const { setGlobalOptions } = require('firebase-functions/v2');
const { defineSecret } = require('firebase-functions/params');
const { createApp } = require('./app');
const { createStore } = require('./store');
const { createMailer } = require('./mailer');

setGlobalOptions({
  region: process.env.API_REGION || 'europe-west1',
  maxInstances: 3,   // a study-sized limit that also caps cost
});

const sendsRealEmail = (process.env.MAIL_PROVIDER || 'console').toLowerCase() !== 'console';
const mailApiKey = defineSecret('MAIL_API_KEY');

let app;   // built on the first request, then reused while the instance is warm

exports.safetouch = onRequest(sendsRealEmail ? { secrets: [mailApiKey] } : {}, async (req, res) => {
  if (!app) {
    const env = sendsRealEmail ? { ...process.env, MAIL_API_KEY: mailApiKey.value() } : process.env;
    app = createApp(await createStore(), { mailer: createMailer(env) });
  }
  return app(req, res);
});
