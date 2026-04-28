import http from 'k6/http';
import { check } from 'k6';
import { unwrapApiBody } from './unwrap.js';

export function login(baseUrl, username, password) {
  const url = `${baseUrl}/api/auth/login`;
  const payload = JSON.stringify({ username, password });
  const res = http.post(url, payload, {
    headers: { 'Content-Type': 'application/json' },
    tags: { name: 'auth_login' },
  });

  const ok = check(res, {
    'login status 200': (r) => r.status === 200,
  });
  if (!ok) {
    throw new Error(`Login failed for '${username}'. status=${res.status} body=${res.body}`);
  }
  const body = unwrapApiBody(res.json());
  if (!body || !body.accessToken) throw new Error(`Login response missing accessToken for '${username}'.`);
  return {
    accessToken: body.accessToken,
    refreshToken: body.refreshToken,
    organizationalId: body.organizationalId,
  };
}

export function authHeaders(accessToken) {
  return {
    Authorization: `Bearer ${accessToken}`,
    'Content-Type': 'application/json',
  };
}

