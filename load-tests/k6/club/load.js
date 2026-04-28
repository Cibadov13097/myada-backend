import http from 'k6/http';
import { check, sleep } from 'k6';
import { getConfig, randomThinkTimeMs } from '../lib/config.js';
import { login, authHeaders } from '../lib/auth.js';

export const options = (() => {
  const cfg = getConfig();
  return {
    scenarios: {
      club_event_load: {
        executor: 'constant-vus',
        vus: cfg.loadVus,
        duration: cfg.loadDuration,
      },
    },
    thresholds: {
      http_req_failed: [`rate<${cfg.maxFailRate}`],
      http_req_duration: [`p(95)<${cfg.p95Ms}`],
      checks: [`rate>0.95`],
    },
  };
})();

function parseCsvEnv(name) {
  const raw = __ENV[name];
  if (!raw) return [];
  return String(raw)
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}

function getEventIds(cfg, accessToken) {
  const explicitEventId = (__ENV.EVENT_ID || '').trim();
  if (explicitEventId) {
    return [Number(explicitEventId)];
  }

  const eventsUrl = `${cfg.baseUrl}${cfg.clubPrefix}/api/v1/events?myClubsOnly=false&page=1&limit=0`;
  const res = http.get(eventsUrl, {
    headers: authHeaders(accessToken),
    tags: { name: 'club_events_list' },
  });
  check(res, { 'events list status 200': (r) => r.status === 200 });
  const body = res.json();
  const items = body && body.items ? body.items : [];
  const eventIds = items.map((e) => e.id).filter((x) => Number.isFinite(x));
  if (eventIds.length === 0) {
    throw new Error(`No events found at ${eventsUrl}. Create/seed events first, or set EVENT_ID.`);
  }

  return eventIds;
}

export function setup() {
  const cfg = getConfig();

  const adminUser = __ENV.ADMIN_USERNAME || 'Admin';
  const adminPass = __ENV.ADMIN_PASSWORD || 'Admin123*';

  const studentUsernames = parseCsvEnv('STUDENT_USERNAMES');
  const studentPasswords = parseCsvEnv('STUDENT_PASSWORDS');
  if (studentUsernames.length === 0 || studentUsernames.length !== studentPasswords.length) {
    throw new Error(
      'Set STUDENT_USERNAMES and STUDENT_PASSWORDS as comma-separated lists of equal length (e.g. STUDENT_USERNAMES="seed_user1,seed_user02").'
    );
  }

  const admin = login(cfg.baseUrl, adminUser, adminPass);
  const students = studentUsernames.map((u, i) => login(cfg.baseUrl, u, studentPasswords[i]));
  const eventIds = getEventIds(cfg, admin.accessToken);

  return { cfg, admin: admin.accessToken, students: students.map((s) => s.accessToken), eventIds };
}

function pick(arr, i) {
  return arr[i % arr.length];
}

export default function (data) {
  const cfg = data.cfg;
  const studentToken = pick(data.students, __ITER);
  const eventId = pick(data.eventIds, __ITER);

  // 1) Register to event (focus: registration flow).
  const registerUrl = `${cfg.baseUrl}${cfg.clubPrefix}/api/v1/events/${eventId}/registrations`;
  const regRes = http.post(registerUrl, null, { headers: authHeaders(studentToken), tags: { name: 'club_event_register' } });

  const regOk = check(regRes, {
    'register status 200 or 409': (r) => r.status === 200 || r.status === 409,
  });

  sleep(randomThinkTimeMs(cfg) / 1000);

  // If seats are full, we still keep load going but skip check-in.
  if (!regOk || regRes.status !== 200) return;

  const regBody = regRes.json();
  const ticketJwt = regBody && regBody.jwt ? String(regBody.jwt) : '';
  check(regRes, { 'registration includes jwt': () => ticketJwt.length > 0 });
  if (!ticketJwt) return;

  // 2) Ticket scan/check-in (focus: QR ticket scan endpoint).
  const checkinUrl = `${cfg.baseUrl}${cfg.clubPrefix}/api/v1/events/${eventId}/check-in`;
  const checkinRes = http.post(
    checkinUrl,
    JSON.stringify({ jwt: ticketJwt }),
    { headers: authHeaders(data.admin), tags: { name: 'club_event_checkin' } }
  );
  check(checkinRes, {
    'check-in status 200': (r) => r.status === 200,
    'check-in has success field': (r) => r.json() && typeof r.json().success === 'boolean',
  });

  sleep(randomThinkTimeMs(cfg) / 1000);
}

