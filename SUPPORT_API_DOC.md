# MyAda Support API Documentation for Frontend

Last updated: 2026-04-25

## Common 404 Mistake (Important)

If you call through gateway (`http://13.60.31.141:500`), you must include `/support` prefix.

- Correct: `http://13.60.31.141:500/support/api/...`
- Wrong: `http://13.60.31.141:500/api/...` (this returns 404 for Support endpoints)

Direct service (no gateway prefix):
- `http://13.60.31.141:5008/api/...`

## Base URLs

Recommended via API Gateway:
- Production: `http://13.60.31.141:500`
- Local: `http://localhost:5000`

Support service gateway prefix:
- `/support`

Final frontend URL format:
- `http://13.60.31.141:500/support/api/...`

Direct service (diagnostics only):
- Production: `http://13.60.31.141:5008/api/...`
- Local: `http://localhost:5008/api/...`

## Authentication

All Support endpoints require JWT bearer token, except internal provisioning endpoint used by backend-to-backend integration.

Header:

```http
Authorization: Bearer <accessToken>
```

Token validation settings:
- Issuer: `myada-auth`
- Audience: `myada-api`
- Signing source: AuthService JWKS (`/.well-known/jwks.json`)

## Swagger

- Gateway: `http://13.60.31.141:500/support/swagger/index.html`
- Direct: `http://13.60.31.141:5008/swagger/index.html`

## Response Envelope

Support API uses AutoWrapper. Most successful responses are wrapped:

```json
{
  "message": "Request successful.",
  "isError": false,
  "responseException": null,
  "result": {}
}
```

`204 No Content` endpoints return empty body.

## Categories

Base path: `/support/api/Categories`

- `GET /support/api/Categories`
- `GET /support/api/Categories/module/{module}`
- `POST /support/api/Categories`
- `DELETE /support/api/Categories/{id}`

Example create payload:

```json
{
  "module": "IT",
  "name": "Hardware"
}
```

## Attachments

Base path: `/support/api/Attachments`

- `POST /support/api/Attachments`
- `GET /support/api/Attachments/request/{requestId}`
- `DELETE /support/api/Attachments/{attachmentId}`

Example create payload:

```json
{
  "requestId": 123,
  "url": "https://example.com/image.jpg"
}
```

## Support Staff Statuses

Base path: `/support/api/SupportStaffStatuses`

- `POST /support/api/SupportStaffStatuses`
- `GET /support/api/SupportStaffStatuses/member/{memberId}`
- `GET /support/api/SupportStaffStatuses/status/{status}`
- `PUT /support/api/SupportStaffStatuses/member/{memberId}`
- `POST /support/api/SupportStaffStatuses/internal/provision` (internal backend use only)

Access roles:
- `support`
- `dispatcher`
- `staff`
- `admin`
- `GET /status/{status}`: any authenticated user
- `POST`, `GET /member/{memberId}`, `PUT /member/{memberId}`:
  - `admin`: can manage any `memberId`
  - `support/dispatcher/staff`: ownership check (`memberId` must match JWT `sub`)
- `POST /internal/provision`: no JWT; requires `X-Internal-Key` header matching server config

`memberId` format:
- GUID (must match authenticated user `sub` claim)
- If `memberId` in POST body is `00000000-0000-0000-0000-000000000000`, backend uses authenticated user id (`sub`).

Auto-create behavior:
- `GET /member/{memberId}` automatically creates a status row when it does not exist.
- `PUT /member/{memberId}` also auto-creates when missing, using the provided status.
- AuthService `POST /api/auth/create-user-with-role` auto-provisions this table for support roles.
- In that flow, `SupportStaffStatus.MemberId` is always the Auth user id (JWT `sub`) and initial status is `Offline`.

Example create payload:

```json
{
  "memberId": "7d75bf0d-c467-4ce7-8872-c58495a6ca8a",
  "status": "Online"
}
```

Typical responses:
- `201 Created` (POST)
- `200 OK` (GET)
- `204 No Content` (PUT)
- `403 Forbidden` when `memberId` does not match authenticated `sub` for non-admin roles
- `401 Unauthorized` when token/user id claim is missing
- `400 Bad Request` when `sub` is not a valid GUID or status value is invalid

Example update payload:

```json
{
  "status": "OnBreak"
}
```

Internal provision payload (backend only):

```json
{
  "memberId": "7d75bf0d-c467-4ce7-8872-c58495a6ca8a",
  "status": "Offline"
}
```

Internal provision headers:

```http
X-Internal-Key: <shared-internal-key>
```

## Support Requests

Base path: `/support/api/SupportRequests`

- `POST /support/api/SupportRequests/uploads` (`multipart/form-data`)
- `POST /support/api/SupportRequests/member/{memberId}`
- `GET /support/api/SupportRequests/member/{memberId}`
- `GET /support/api/SupportRequests/staff/{staffId}`
- `GET /support/api/SupportRequests`
- `GET /support/api/SupportRequests/{requestId}`
- `GET /support/api/SupportRequests/{requestId}/timeline`
- `PUT /support/api/SupportRequests/{requestId}/cancel/member/{memberId}`
- `PUT /support/api/SupportRequests/{requestId}/assign/dispatcher/{dispatcherId}`
- `PUT /support/api/SupportRequests/{requestId}/reassign/dispatcher/{dispatcherId}`
- `PUT /support/api/SupportRequests/{requestId}/dispatcher-instructions/dispatcher/{dispatcherId}`
- `PUT /support/api/SupportRequests/{requestId}/start/staff/{staffId}`
- `PUT /support/api/SupportRequests/{requestId}/in-progress/staff/{staffId}`
- `PUT /support/api/SupportRequests/{requestId}/complete/staff/{staffId}`

Upload constraints (`/uploads`):
- Max file size: `10 MB`
- Allowed extensions: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`

Example create request payload:

```json
{
  "area": "it",
  "categoryId": 3,
  "locationType": "building",
  "buildingId": 1,
  "placeType": "room",
  "roomId": 204,
  "areaDetails": null,
  "description": "Projector is not turning on",
  "urgency": "standard",
  "attachmentUrls": [
    "https://example.com/photo1.jpg"
  ]
}
```

## Realtime (SignalR)

Hub endpoint:
- Gateway: `/support/hubs/support-updates`
- Direct: `/hubs/support-updates`

JWT token is required for hub access.

## Quick cURL Example

```bash
curl -X GET "http://13.60.31.141:500/support/api/Categories" \
  -H "accept: application/json" \
  -H "Authorization: Bearer <accessToken>"
```

## Reference

Detailed internal endpoint descriptions and DTO examples:
- `Services/MyAdaSupportService/API_ENDPOINTS.md`
