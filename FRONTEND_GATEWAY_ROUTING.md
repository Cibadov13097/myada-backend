# Frontend Gateway Routing Guide (Avoid 404)

Last updated: 2026-04-16

## Why 404 happens

Most 404 issues come from mixing:
- Gateway host: `http://51.20.193.29:5000`
- Direct service paths like `/api/...`

If you use gateway host (`:5000`), you must use the correct gateway prefix per service.

## Base URLs

- API Gateway (recommended for frontend): `http://51.20.193.29:5000`
- Direct services (diagnostics only):
  - Auth: `http://51.20.193.29:5001`
  - LostFound: `http://51.20.193.29:5002`
  - Club: `http://51.20.193.29:5003`
  - Notification: `http://51.20.193.29:5004`
  - Location: `http://51.20.193.29:5005`
  - Call: `http://51.20.193.29:5006`
  - Support: `http://51.20.193.29:5008`

## Gateway route map

When host is `http://51.20.193.29:5000`, use:

- Auth:
  - `/api/auth/...` (supported)
  - `/auth/...` (supported proxy route)
- LostFound: `/lostfound/...`
- Club: `/club/...`
- Notification: `/notification/...`
- Location: `/location/...`
- Call: `/call/...`
- Support: `/support/...`

## Direct vs gateway examples

### Support Staff Statuses

Direct service:
- `http://51.20.193.29:5008/api/SupportStaffStatuses/member/{memberId}`

Gateway equivalent:
- `http://51.20.193.29:5000/support/api/SupportStaffStatuses/member/{memberId}`

Wrong (will 404 on gateway):
- `http://51.20.193.29:5000/api/SupportStaffStatuses/member/{memberId}`

### Call Hub

Gateway:
- `http://51.20.193.29:5000/call/hub`

Direct:
- `http://51.20.193.29:5006/hub`

### Auth users-by-role

Gateway:
- `http://51.20.193.29:5000/api/auth/users-by-role/{role}`

Direct:
- `http://51.20.193.29:5001/api/auth/users-by-role/{role}`

## Swagger URLs

- Gateway Swagger:
  - Auth: `http://51.20.193.29:5000/auth/swagger/index.html`
  - Support: `http://51.20.193.29:5000/support/swagger/index.html`
  - Call: `http://51.20.193.29:5000/call/swagger/index.html`
- Direct Swagger:
  - Auth: `http://51.20.193.29:5001/swagger/index.html`
  - Support: `http://51.20.193.29:5008/swagger/index.html`
  - Call: `http://51.20.193.29:5006/swagger/index.html`

## Frontend config recommendation

Use one gateway base URL and service-specific prefixes.

```ts
const GATEWAY_BASE = "http://51.20.193.29:5000";

export const endpoints = {
  auth: `${GATEWAY_BASE}/api/auth`,
  support: `${GATEWAY_BASE}/support/api`,
  callHub: `${GATEWAY_BASE}/call/hub`,
  lostfound: `${GATEWAY_BASE}/lostfound`,
  club: `${GATEWAY_BASE}/club`,
  location: `${GATEWAY_BASE}/location`,
  notification: `${GATEWAY_BASE}/notification`
};
```

## 404 troubleshooting checklist

1. Confirm host is correct (`:5000` gateway vs direct service port).
2. If using gateway, confirm service prefix exists (`/support`, `/call`, etc.).
3. Do not copy direct service path into gateway URL without adding gateway prefix.
4. For protected endpoints, ensure `Authorization: Bearer <token>` is present (otherwise 401/403, not 404).

