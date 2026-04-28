export function unwrapApiBody(json) {
  // Many services use AutoWrapper and return:
  // { statusCode, message, result: <payload> }
  // Some return the payload directly.
  if (!json || typeof json !== 'object') return json;
  if (Object.prototype.hasOwnProperty.call(json, 'result')) return json.result;
  return json;
}

