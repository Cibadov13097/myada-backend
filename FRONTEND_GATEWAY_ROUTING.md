# Frontend Gateway Routing Guide (Avoid 404)

Last updated: 2026-04-25

## Why 404 happens

Most 404 issues come from mixing:
- Gateway host: `http://13.60.31.141:500`
- Direct service paths like `/api/...`

If you use the deployed gateway host (`:500`), you must use the correct gateway prefix per service.

## Base URLs

- API Gateway (recommended for frontend): `http://13.60.31.141:500`
- Direct services (diagnostics only):
  - Auth: `http://13.60.31.141:5001`
  - LostFound: `http://13.60.31.141:5002`
  - Club: `http://13.60.31.141:5003`
  - Notification: `http://13.60.31.141:5004`
  - Location: `http://13.60.31.141:5005`
  - Call: `http://13.60.31.141:5006`
  - Support: `http://13.60.31.141:5008`

These direct service URLs are for diagnostics only and may vary by environment. Frontend traffic should use the gateway base above.

## Gateway route map

When host is `http://13.60.31.141:500`, use:

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
- `http://13.60.31.141:5008/api/SupportStaffStatuses/member/{memberId}`

Gateway equivalent:
- `http://13.60.31.141:500/support/api/SupportStaffStatuses/member/{memberId}`

Wrong (will 404 on gateway):
- `http://13.60.31.141:500/api/SupportStaffStatuses/member/{memberId}`

### Call Hub

Gateway:
- `http://13.60.31.141:500/call/hub`

Direct:
- `http://13.60.31.141:5006/hub`

### Auth users-by-role

Gateway:
- `http://13.60.31.141:500/api/auth/users-by-role/{role}`

Direct:
- `http://13.60.31.141:5001/api/auth/users-by-role/{role}`

### LostFound and Club (common 404 case)

Wrong on gateway (missing service prefix):
- `http://13.60.31.141:500/api/v1/found-items`
- `http://13.60.31.141:500/api/v1/categories`

Correct on gateway:
- `http://13.60.31.141:500/lostfound/api/v1/found-items` (if endpoint exists in service)
- `http://13.60.31.141:500/club/api/v1/categories`

Direct service examples:
- `http://13.60.31.141:5002/api/v1/found-items`
- `http://13.60.31.141:5003/api/v1/categories`

## Swagger URLs

- Gateway Swagger:
  - Auth: `http://13.60.31.141:500/auth/swagger/index.html`
  - Support: `http://13.60.31.141:500/support/swagger/index.html`
  - Call: `http://13.60.31.141:500/call/swagger/index.html`
- Direct Swagger:
  - Auth: `http://13.60.31.141:5001/swagger/index.html`
  - Support: `http://13.60.31.141:5008/swagger/index.html`
  - Call: `http://13.60.31.141:5006/swagger/index.html`

## Frontend config recommendation

Use one gateway base URL and service-specific prefixes.

```ts
const GATEWAY_BASE = "http://13.60.31.141:500";

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

1. Confirm host is correct (`:500` deployed gateway vs direct service port).
2. If using gateway, confirm service prefix exists (`/support`, `/call`, etc.).
3. Do not copy direct service path into gateway URL without adding gateway prefix.
4. For protected endpoints, ensure `Authorization: Bearer <token>` is present (otherwise 401/403, not 404).
