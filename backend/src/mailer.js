// Sends the parental consent email: a 6-digit code to verify the consent and a
// copy of what the parent agreed to. Uses an email API over HTTPS (no extra
// dependencies):
//   MAIL_PROVIDER=brevo   MAIL_API_KEY=...   (brevo.com, free tier)
//   MAIL_PROVIDER=resend  MAIL_API_KEY=...   (resend.com, free tier)
//   MAIL_PROVIDER=console (default)          prints emails, for development and tests
// MAIL_FROM is the sender address (verified with the provider).

const AGREEMENT_TEXT = {
  en: {
    guardian: 'I am the parent or legal guardian of the child who will use SafeTouch.',
    terms: 'I have read and agree to the SafeTouch terms and privacy notice.',
    childUse: 'I give permission for my child to use SafeTouch, with an adult nearby.',
    dataUse: 'I agree that SafeTouch keeps my name, phone number, email and country to verify this consent and contact me about it.',
  },
  rw: {
    guardian: "Ndi umubyeyi cyangwa umurera wemewe n'amategeko w'umwana uzakoresha SafeTouch.",
    terms: "Nasomye kandi nemeye amabwiriza ya SafeTouch n'itangazo ryerekeye ibanga.",
    childUse: 'Nemereye umwana wanjye gukoresha SafeTouch, ari kumwe n\'umuntu mukuru.',
    dataUse: "Nemeye ko SafeTouch ibika izina ryanjye, nimero ya telefoni, imeyili n'igihugu kugira ngo yemeze uru ruhushya kandi impamagare cyangwa inyandikire kuri rwo.",
  },
};

const TEXT = {
  en: {
    subject: 'SafeTouch: please confirm consent for your child',
    hello: (name) => `Hello ${name},`,
    intro: 'Thank you for allowing your child to learn with SafeTouch. To confirm that it was you who gave permission, type this code in the app:',
    agreed: 'You agreed that:',
    termsRead: (v) => `The terms you read and agreed to (version ${v}):`,
    details: 'Details you gave',
    name: 'Name', phone: 'Phone', email: 'Email', country: 'Country', date: 'Date', terms: 'Terms version',
    withdraw: 'You can withdraw at any time in the app (Grown-ups > Settings > Withdraw consent), which deletes these details and your child\'s progress. The code expires in 24 hours.',
    ignore: 'If you did not fill in this form, you can ignore this email.',
  },
  rw: {
    subject: "SafeTouch: emeza uruhushya rw'umwana wawe",
    hello: (name) => `Muraho ${name},`,
    intro: "Murakoze kwemerera umwana wanyu kwigira kuri SafeTouch. Kugira ngo twemeze ko ari mwe mwatanze uruhushya, andika iyi kode muri porogaramu:",
    agreed: 'Mwemeye ko:',
    termsRead: (v) => `Amabwiriza mwasomye mukemera (verisiyo ${v}):`,
    details: 'Amakuru mwatanze',
    name: 'Izina', phone: 'Telefoni', email: 'Imeyili', country: 'Igihugu', date: 'Itariki', terms: "Verisiyo y'amabwiriza",
    withdraw: "Mushobora gukuraho uru ruhushya igihe icyo ari cyo cyose muri porogaramu (Ababyeyi > Igenamiterere > Kuraho uruhushya), bigasiba aya makuru n'iterambere ry'umwana wanyu. Iyi kode irangira nyuma y'amasaha 24.",
    ignore: 'Niba atari mwe mwujuje iyi fomu, mushobora kwirengagiza iyi meyili.',
  },
};

function escapeHtml(s) {
  return String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

/** Builds the consent email (subject, HTML and plain text) in the parent's language. */
function consentEmail(record, code) {
  const lang = record.language === 'rw' ? 'rw' : 'en';
  const t = TEXT[lang];
  const agreements = Object.keys(AGREEMENT_TEXT[lang]).map((k) => AGREEMENT_TEXT[lang][k]);
  // The terms exactly as the parent read them in the app (sent with the consent), so the email shows what was agreed to.
  const terms = Array.isArray(record.terms) ? record.terms : [];
  const termsText = terms.length ? [t.termsRead(record.termsVersion), '', ...terms.flatMap((s) => [`  ${s.title}`, `  ${s.body}`, ''])] : [];
  const termsHtml = terms.length
    ? `<p><b>${escapeHtml(t.termsRead(record.termsVersion))}</b></p>
<div style="border-left:4px solid #EAF4FB;padding-left:12px">${terms.map((s) => `<p style="margin:0 0 4px"><b>${escapeHtml(s.title)}</b></p><p style="margin:0 0 12px;color:#1F2A44">${escapeHtml(s.body)}</p>`).join('')}</div>`
    : '';
  const rows = [
    [t.name, record.parentName], [t.phone, record.phone], [t.email, record.email],
    [t.country, record.country], [t.date, record.submittedAt], [t.terms, record.termsVersion],
  ];
  const text = [
    t.hello(record.parentName), '', t.intro, '', `    ${code}`, '', t.agreed,
    ...agreements.map((a) => `  - ${a}`), '', ...termsText, `${t.details}:`, ...rows.map(([k, v]) => `  ${k}: ${v}`), '', t.withdraw, '', t.ignore,
  ].join('\n');
  const html = `<div style="font-family:Arial,sans-serif;color:#1F2A44;max-width:560px">
<h2 style="color:#2474C6">SafeTouch</h2>
<p>${escapeHtml(t.hello(record.parentName))}</p>
<p>${escapeHtml(t.intro)}</p>
<p style="font-size:32px;font-weight:bold;letter-spacing:8px;background:#EAF4FB;padding:16px;text-align:center">${escapeHtml(code)}</p>
<p><b>${escapeHtml(t.agreed)}</b></p>
<ul>${agreements.map((a) => `<li>${escapeHtml(a)}</li>`).join('')}</ul>
${termsHtml}
<p><b>${escapeHtml(t.details)}</b></p>
<table>${rows.map(([k, v]) => `<tr><td style="padding-right:12px;color:#5F6F86">${escapeHtml(k)}</td><td>${escapeHtml(v)}</td></tr>`).join('')}</table>
<p style="color:#5F6F86">${escapeHtml(t.withdraw)}</p>
<p style="color:#5F6F86">${escapeHtml(t.ignore)}</p>
</div>`;
  return { subject: t.subject, html, text };
}

const LOGIN_TEXT = {
  en: {
    subject: 'SafeTouch: your login code',
    hello: (name) => `Hello ${name},`,
    intro: 'Type this code in the SafeTouch app to open it. It expires in 10 minutes.',
    ignore: 'If you did not try to open SafeTouch, ignore this email: nobody can get in without this code.',
  },
  rw: {
    subject: 'SafeTouch: kode yo kwinjira',
    hello: (name) => `Muraho ${name},`,
    intro: 'Andika iyi kode muri porogaramu ya SafeTouch kugira ngo uyifungure. Irangira nyuma y\'iminota 10.',
    ignore: 'Niba atari mwe mwagerageje gufungura SafeTouch, irengagize iyi meyili: nta wundi winjira adafite iyi kode.',
  },
};

/** The login email: just the code, in the parent's language. */
function loginEmail(record, code) {
  const t = LOGIN_TEXT[record.language === 'rw' ? 'rw' : 'en'];
  const text = [t.hello(record.parentName), '', t.intro, '', `    ${code}`, '', t.ignore].join('\n');
  const html = `<div style="font-family:Arial,sans-serif;color:#1F2A44;max-width:560px">
<h2 style="color:#2474C6">SafeTouch</h2>
<p>${escapeHtml(t.hello(record.parentName))}</p>
<p>${escapeHtml(t.intro)}</p>
<p style="font-size:32px;font-weight:bold;letter-spacing:8px;background:#EAF4FB;padding:16px;text-align:center">${escapeHtml(code)}</p>
<p style="color:#5F6F86">${escapeHtml(t.ignore)}</p>
</div>`;
  return { subject: t.subject, html, text };
}

const AGE_TEXT = {
  en: {
    subject: 'SafeTouch: confirm your child\'s age',
    hello: (name) => `Hello ${name},`,
    intro: (age) => `You told SafeTouch that your child is ${age} years old and asked for the Guardian lessons (ages 9 to 11). To confirm their age, type this code in the app:`,
    why: 'The Guardian lessons talk openly about things like online strangers and private parts of the body, so they are only for children aged 9 to 11. Younger children use the Explorer lessons.',
    expires: 'The code expires in 24 hours.',
    ignore: 'If you did not ask for this, ignore this email: the Guardian lessons stay closed.',
  },
  rw: {
    subject: 'SafeTouch: emeza imyaka y\'umwana wawe',
    hello: (name) => `Muraho ${name},`,
    intro: (age) => `Mwabwiye SafeTouch ko umwana wanyu afite imyaka ${age} kandi musabye amasomo y'Umurinzi (imyaka 9 kugeza 11). Kugira ngo twemeze imyaka ye, andika iyi kode muri porogaramu:`,
    why: 'Amasomo y\'Umurinzi avuga ku buryo bweruye ibintu nk\'abantu batazwi kuri interineti n\'ibice by\'umubiri by\'ibanga, ni yo mpamvu ari ay\'abana bafite imyaka 9 kugeza 11 gusa. Abato bakoresha amasomo y\'Umuvumbuzi.',
    expires: 'Iyi kode irangira nyuma y\'amasaha 24.',
    ignore: 'Niba atari mwe mwabisabye, irengagize iyi meyili: amasomo y\'Umurinzi azakomeza gufunga.',
  },
};

/** The email that asks the parent to confirm their child's age (9 to 11) before the Guardian lessons open. */
function ageEmail(record, ageYears, code) {
  const t = AGE_TEXT[record.language === 'rw' ? 'rw' : 'en'];
  const text = [t.hello(record.parentName), '', t.intro(ageYears), '', `    ${code}`, '', t.why, '', t.expires, '', t.ignore].join('\n');
  const html = `<div style="font-family:Arial,sans-serif;color:#1F2A44;max-width:560px">
<h2 style="color:#2474C6">SafeTouch</h2>
<p>${escapeHtml(t.hello(record.parentName))}</p>
<p>${escapeHtml(t.intro(ageYears))}</p>
<p style="font-size:32px;font-weight:bold;letter-spacing:8px;background:#EAF4FB;padding:16px;text-align:center">${escapeHtml(code)}</p>
<p>${escapeHtml(t.why)}</p>
<p style="color:#5F6F86">${escapeHtml(t.expires)}</p>
<p style="color:#5F6F86">${escapeHtml(t.ignore)}</p>
</div>`;
  return { subject: t.subject, html, text };
}
const EMAIL_TEXT = {
  en: {
    subject: 'SafeTouch: confirm your new email',
    hello: (name) => `Hello ${name},`,
    intro: 'You asked SafeTouch to use this email from now on. To confirm it is yours, type this code in the app:',
    expires: 'The code expires in 30 minutes.',
    ignore: 'If you did not ask for this, ignore this email: nothing changes.',
  },
  rw: {
    subject: 'SafeTouch: emeza imeyili yanyu nshya',
    hello: (name) => `Muraho ${name},`,
    intro: 'Mwasabye SafeTouch gukoresha iyi meyili guhera ubu. Kugira ngo twemeze ko ari iyanyu, andika iyi kode muri porogaramu:',
    expires: 'Iyi kode irangira nyuma y\'iminota 30.',
    ignore: 'Niba atari mwe mwabisabye, irengagize iyi meyili: nta gihinduka.',
  },
};

/** The code that proves the parent can read their new email, sent to the new address only. */
function emailChangeEmail(record, code) {
  const t = EMAIL_TEXT[record.language === 'rw' ? 'rw' : 'en'];
  const text = [t.hello(record.parentName), '', t.intro, '', `    ${code}`, '', t.expires, '', t.ignore].join('\n');
  const html = `<div style="font-family:Arial,sans-serif;color:#1F2A44;max-width:560px">
<h2 style="color:#2474C6">SafeTouch</h2>
<p>${escapeHtml(t.hello(record.parentName))}</p>
<p>${escapeHtml(t.intro)}</p>
<p style="font-size:32px;font-weight:bold;letter-spacing:8px;background:#EAF4FB;padding:16px;text-align:center">${escapeHtml(code)}</p>
<p style="color:#5F6F86">${escapeHtml(t.expires)}</p>
<p style="color:#5F6F86">${escapeHtml(t.ignore)}</p>
</div>`;
  return { subject: t.subject, html, text };
}

const CHANGED_TEXT = {
  en: {
    subject: 'SafeTouch: your email was changed',
    hello: (name) => `Hello ${name},`,
    body: (masked) => `SafeTouch will send your codes to ${masked} from now on, instead of this email.`,
    help: 'If you did not do this, open SafeTouch on your child\'s phone, go to Grown-ups > Settings, and withdraw your consent or change the email back.',
  },
  rw: {
    subject: 'SafeTouch: imeyili yanyu yahinduwe',
    hello: (name) => `Muraho ${name},`,
    body: (masked) => `Guhera ubu SafeTouch izajya yohereza kode zanyu kuri ${masked}, aho kuba kuri iyi meyili.`,
    help: 'Niba atari mwe mwabikoze, mufungure SafeTouch kuri telefoni y\'umwana wanyu, mujye ahabugenewe abantu bakuru > Igenamiterere, mukureho uruhushya cyangwa musubizeho imeyili ya mbere.',
  },
};

/** A notice to the old email after a change: no code, just so the parent can spot a change they did not make. */
function emailChangedNotice(record, newMasked) {
  const t = CHANGED_TEXT[record.language === 'rw' ? 'rw' : 'en'];
  const text = [t.hello(record.parentName), '', t.body(newMasked), '', t.help].join('\n');
  const html = `<div style="font-family:Arial,sans-serif;color:#1F2A44;max-width:560px">
<h2 style="color:#2474C6">SafeTouch</h2>
<p>${escapeHtml(t.hello(record.parentName))}</p>
<p>${escapeHtml(t.body(newMasked))}</p>
<p style="color:#5F6F86">${escapeHtml(t.help)}</p>
</div>`;
  return { subject: t.subject, html, text };
}


function createMailer(env = process.env) {
  const provider = (env.MAIL_PROVIDER || 'console').toLowerCase();
  const from = env.MAIL_FROM || 'no-reply@safetouch.example';
  const key = env.MAIL_API_KEY;

  if (provider === 'brevo') {
    return {
      provider,
      async send({ to, toName, subject, html, text }) {
        const res = await fetch('https://api.brevo.com/v3/smtp/email', {
          method: 'POST',
          headers: { 'api-key': key, 'content-type': 'application/json', accept: 'application/json' },
          body: JSON.stringify({ sender: { email: from, name: 'SafeTouch' }, to: [{ email: to, name: toName }], subject, htmlContent: html, textContent: text }),
        });
        if (!res.ok) throw new Error(`Brevo refused the email (${res.status}).`);
      },
    };
  }
  if (provider === 'resend') {
    return {
      provider,
      async send({ to, subject, html, text }) {
        const res = await fetch('https://api.resend.com/emails', {
          method: 'POST',
          headers: { authorization: `Bearer ${key}`, 'content-type': 'application/json' },
          body: JSON.stringify({ from: `SafeTouch <${from}>`, to: [to], subject, html, text }),
        });
        if (!res.ok) throw new Error(`Resend refused the email (${res.status}).`);
      },
    };
  }
  // Development and tests: keep the emails and print them instead of sending.
  const sent = [];
  return {
    provider: 'console',
    sent,
    async send(message) {
      sent.push(message);
      if (!env.QUIET_MAILER) console.log(`[mail to ${message.to}] ${message.subject}\n${message.text}\n`);
    },
  };
}

module.exports = { createMailer, consentEmail, loginEmail, ageEmail, emailChangeEmail, emailChangedNotice, AGREEMENT_TEXT };
