// Sends the parental consent code by SMS, as well as by email, so a parent can
// type whichever arrives first. The same 6-digit code is in both.
//   SMS_PROVIDER=brevo    SMS_API_KEY=... (defaults to MAIL_API_KEY)  SMS_SENDER=SafeTouch
//   SMS_PROVIDER=console  prints the texts, for development and tests
//   SMS_PROVIDER=off      (default) no SMS: email only
// Brevo SMS is paid by credit (brevo.com > SMS > buy credits). SMS_SENDER is the
// name shown to the parent: at most 11 letters and digits.
//
// A fake or stolen consent form could otherwise make the API text strangers and
// spend credit, so every sender is capped: SMS_PER_PHONE_DAILY texts to one
// number (default 3) and SMS_DAILY_LIMIT in total (default 100) in 24 hours.
// The counters live in memory, so they reset when the server restarts.

const DAY_MS = 24 * 60 * 60 * 1000;

const TEXT = {
  en: (code) => `SafeTouch code: ${code}. Type it in the app to confirm your consent for your child. It expires in 24 hours. If this was not you, ignore this text.`,
  rw: (code) => `SafeTouch kode: ${code}. Yandike muri porogaramu kwemeza uruhushya rw'umwana wawe. Irangira nyuma y'amasaha 24. Niba atari wowe, irengagize iyi nyandiko.`,
};

/** The code as a short text, in the parent's language. */
function consentSms(record, code) {
  return (TEXT[record.language === 'rw' ? 'rw' : 'en'])(code);
}

/** "+250 788 123 456" -> "250788123456" (digits only, with the country code), or null if it can't be a phone number. */
function normalisePhone(phone) {
  let digits = String(phone || '').replace(/\D/g, '');
  if (digits.startsWith('00')) digits = digits.slice(2);
  return digits.length >= 8 && digits.length <= 15 ? digits : null;
}

function createSmsSender(env = process.env, now = () => Date.now()) {
  const provider = (env.SMS_PROVIDER || 'off').toLowerCase();
  const perPhone = Number(env.SMS_PER_PHONE_DAILY) || 3;
  const total = Number(env.SMS_DAILY_LIMIT) || 100;
  const sender = String(env.SMS_SENDER || 'SafeTouch').replace(/[^A-Za-z0-9]/g, '').slice(0, 11) || 'SafeTouch';
  const key = env.SMS_API_KEY || env.MAIL_API_KEY;
  const sentAt = new Map();   // phone -> times of texts in the last 24 hours

  if (provider === 'off') return { provider, async send() { return false; } };

  let transport;
  if (provider === 'brevo') {
    transport = async (recipient, content) => {
      const res = await fetch('https://api.brevo.com/v3/transactionalSMS/send', {
        method: 'POST',
        headers: { 'api-key': key, 'content-type': 'application/json', accept: 'application/json' },
        body: JSON.stringify({ sender, recipient, content, type: 'transactional' }),
      });
      if (!res.ok) {
        const reason = await res.json().then((b) => b.message, () => '');
        throw new Error(`Brevo refused the SMS (${res.status}${reason ? `: ${reason}` : ''}).`);
      }
    };
  } else {
    // Development and tests: keep the texts and print them instead of sending.
    transport = async (recipient, content) => {
      sender_.sent.push({ to: recipient, text: content });
      if (!env.QUIET_MAILER) console.log(`[sms to +${recipient}] ${content}\n`);
    };
  }

  const sender_ = {
    provider,
    sent: [],
    /** Texts the phone number. Returns true if it was sent, false if the number or the limits stopped it; throws if the provider failed. */
    async send({ phone, text }) {
      const recipient = normalisePhone(phone);
      if (!recipient) return false;
      const t = now();
      for (const [p, times] of sentAt) {
        const recent = times.filter((x) => x > t - DAY_MS);
        if (recent.length) sentAt.set(p, recent); else sentAt.delete(p);
      }
      const forPhone = sentAt.get(recipient) || [];
      const all = [...sentAt.values()].reduce((n, times) => n + times.length, 0);
      if (forPhone.length >= perPhone || all >= total) return false;
      sentAt.set(recipient, forPhone.concat(t));   // counted before sending: a failing provider can't be hammered either
      await transport(recipient, text);
      return true;
    },
  };
  return sender_;
}

module.exports = { createSmsSender, consentSms, normalisePhone };
