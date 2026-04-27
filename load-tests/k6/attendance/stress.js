import http from 'k6/http';
import { check, sleep } from 'k6';
import { getConfig, randomThinkTimeMs } from '../lib/config.js';
import { login, authHeaders } from '../lib/auth.js';
import { getUserIdFromAccessToken } from '../lib/jwt.js';

export const options = (() => {
  const cfg = getConfig();
  return {
    scenarios: {
      attendance_qr_stress: {
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

  const instructorUser = __ENV.INSTRUCTOR_USERNAME;
  const instructorPass = __ENV.INSTRUCTOR_PASSWORD;
  if (!instructorUser || !instructorPass) {
    throw new Error('Set INSTRUCTOR_USERNAME and INSTRUCTOR_PASSWORD for attendance QR flow.');
  }

  const studentUsernames = parseCsvEnv('STUDENT_USERNAMES');
  const studentPasswords = parseCsvEnv('STUDENT_PASSWORDS');
  if (studentUsernames.length === 0 || studentUsernames.length !== studentPasswords.length) {
    throw new Error('Set STUDENT_USERNAMES and STUDENT_PASSWORDS (comma-separated) with equal length.');
  }

  const instructor = login(cfg.baseUrl, instructorUser, instructorPass);
  const instructorId = getUserIdFromAccessToken(instructor.accessToken);

  const students = studentUsernames.map((u, i) => {
    const s = login(cfg.baseUrl, u, studentPasswords[i]);
    return { accessToken: s.accessToken, studentId: getUserIdFromAccessToken(s.accessToken) };
  });

  const lessonsUrl = `${cfg.baseUrl}${cfg.attendancePrefix}/api/instructors/${instructorId}/lessons`;
  const lessonsRes = http.get(lessonsUrl, { headers: authHeaders(instructor.accessToken), tags: { name: 'attendance_instructor_lessons' } });
  check(lessonsRes, { 'instructor lessons 200': (r) => r.status === 200 });
  const lessons = lessonsRes.json() || [];
  if (!Array.isArray(lessons) || lessons.length === 0) throw new Error('No lessons for instructor.');
  const lessonId = lessons[0].id;

  const sessionsUrl = `${cfg.baseUrl}${cfg.attendancePrefix}/api/instructors/${instructorId}/lessons/${lessonId}/sessions`;
  const sessionsRes = http.get(sessionsUrl, { headers: authHeaders(instructor.accessToken), tags: { name: 'attendance_instructor_sessions' } });
  check(sessionsRes, { 'instructor sessions 200': (r) => r.status === 200 });
  const sessions = sessionsRes.json() || [];
  if (!Array.isArray(sessions) || sessions.length === 0) throw new Error('No sessions for lesson.');
  const sessionId = sessions[0].id;

  const activateUrl = `${cfg.baseUrl}${cfg.attendancePrefix}/api/instructors/${instructorId}/sessions/${sessionId}/attendance/activate/1`;
  const activateRes = http.post(activateUrl, null, { headers: authHeaders(instructor.accessToken), tags: { name: 'attendance_activate_round1' } });
  check(activateRes, { 'activate responds 200 or 400': (r) => r.status === 200 || r.status === 400 });

  return { cfg, instructorToken: instructor.accessToken, instructorId, sessionId, students };
}

function pick(arr, i) {
  return arr[i % arr.length];
}

export default function (data) {
  const cfg = data.cfg;
  const student = pick(data.students, __ITER);

  const tokenUrl = `${cfg.baseUrl}${cfg.attendancePrefix}/api/instructors/${data.instructorId}/sessions/${data.sessionId}/qr-token`;
  const tokenRes = http.post(tokenUrl, null, { headers: authHeaders(data.instructorToken), tags: { name: 'attendance_issue_qr_token' } });
  check(tokenRes, { 'qr-token status 200': (r) => r.status === 200 });
  const tokenBody = tokenRes.json();
  const qrToken = tokenBody && tokenBody.token ? String(tokenBody.token) : '';
  if (!qrToken) return;

  const scanUrl = `${cfg.baseUrl}${cfg.attendancePrefix}/api/students/${student.studentId}/attendance/scan`;
  const scanRes = http.post(
    scanUrl,
    JSON.stringify({ studentId: student.studentId, token: qrToken }),
    { headers: authHeaders(student.accessToken), tags: { name: 'attendance_student_scan' } }
  );
  check(scanRes, { 'scan status 200': (r) => r.status === 200 });

  sleep(randomThinkTimeMs(cfg) / 1000);
}

