import http from 'k6/http';
import { check, sleep } from 'k6';
import { getConfig, randomThinkTimeMs } from '../lib/config.js';
import { login, authHeaders } from '../lib/auth.js';

export const options = (() => {
  const cfg = getConfig();
  return {
    scenarios: {
      club_event_stress: {
        executor: 'ramping-vus',
        startVUs: cfg.stressStartVus,
        stages: [
          { duration: cfg.stressRampUp, target: cfg.stressMaxVus },
          { duration: cfg.stressHold, target: cfg.stressMaxVus },
          { duration: cfg.stressRampDown, target: 0 },
        ],
        gracefulRampDown: '30s',
      },
    },
    thresholds: {
      http_req_failed: [`rate<${cfg.maxFailRate}`],
      http_req_duration: [`p(95)<${cfg.p95Ms}`],
      checks: [`rate>0.90`],
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

export function setup() {
  const cfg = getConfig();

  const adminUser = __ENV.ADMIN_USERNAME || 'Admin';
  const adminPass = __ENV.ADMIN_PASSWORD || 'Admin123*';

  const studentUsernames = parseCsvEnv('STUDENT_USERNAMES');
  const studentPasswords = parseCsvEnv('STUDENT_PASSWORDS');
  if (studentUsernames.length === 0 || studentUsernames.length !== studentPasswords.length) {
    throw new Error('Set STUDENT_USERNAMES and STUDENT_PASSWORDS (comma-separated) with equal length.');
  }

  const admin = login(cfg.baseUrl, adminUser, adminPass);
  const students = studentUsernames.map((u, i) => login(cfg.baseUrl, u, studentPasswords[i]));

  const eventsUrl = `${cfg.baseUrl}${cfg.clubPrefix}/api/v1/events?page=1&limit=50`;
  const res = http.get(eventsUrl, { headers: authHeaders(admin.accessToken), tags: { name: 'club_events_list' } });
  check(res, { 'events list status 200': (r) => r.status === 200 });
  const body = res.json();
  const items = body && body.items ? body.items : [];
  const eventIds = items.map((e) => e.id).filter((x) => Number.isFinite(x));
  if (eventIds.length === 0) throw new Error('No events found. Seed events first.');

  return { cfg, admin: admin.accessToken, students: students.map((s) => s.accessToken), eventIds };
}

function pick(arr, i) {
  return arr[i % arr.length];
}

export default function (data) {
  const cfg = data.cfg;
  const studentToken = pick(data.students, __ITER);
  const eventId = pick(data.eventIds, __ITER);

  const registerUrl = `${cfg.baseUrl}${cfg.clubPrefix}/api/v1/events/${eventId}/registrations`;
  const regRes = http.post(registerUrl, null, { headers: authHeaders(studentToken), tags: { name: 'club_event_register' } });
  check(regRes, { 'register status 200 or 409': (r) => r.status === 200 || r.status === 409 });

  if (regRes.status === 200) {
    const ticketJwt = (regRes.json() && regRes.json().jwt) ? String(regRes.json().jwt) : '';
    if (ticketJwt) {
      const checkinUrl = `${cfg.baseUrl}${cfg.clubPrefix}/api/v1/events/${eventId}/check-in`;
      const checkinRes = http.post(
        checkinUrl,
        JSON.stringify({ jwt: ticketJwt }),
        { headers: authHeaders(data.admin), tags: { name: 'club_event_checkin' } }
      );
      check(checkinRes, { 'check-in status 200': (r) => r.status === 200 });
    }
  }

  sleep(randomThinkTimeMs(cfg) / 1000);
}

