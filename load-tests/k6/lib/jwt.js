import encoding from 'k6/encoding';

function base64UrlDecodeToString(input) {
  // k6 encoding expects standard base64; JWT uses base64url without padding.
  let b64 = String(input).replace(/-/g, '+').replace(/_/g, '/');
  const pad = b64.length % 4;
  if (pad === 2) b64 += '==';
  else if (pad === 3) b64 += '=';
  else if (pad !== 0) throw new Error(`Invalid base64url length: ${b64.length}`);
  return encoding.b64decode(b64, 'std', 's');
}

export function decodeJwtPayload(accessToken) {
  if (!accessToken) throw new Error('Missing accessToken');
  const parts = String(accessToken).split('.');
  if (parts.length < 2) throw new Error('Invalid JWT format');
  const payloadJson = base64UrlDecodeToString(parts[1]);
  return JSON.parse(payloadJson);
}

export function getUserIdFromAccessToken(accessToken) {
  const payload = decodeJwtPayload(accessToken);
  const id = payload.sub || payload.user_id || payload.nameid || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
  if (!id) throw new Error('Could not determine user id claim (sub/user_id/nameid)');
  return String(id);
}

