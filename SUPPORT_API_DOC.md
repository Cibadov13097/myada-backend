# MyAda Support API Documentation for Frontend

Last updated: 2026-04-26

## Base URLs

- Gateway (production): `http://13.60.31.141`
- Gateway (local): `http://localhost:5000`
- Direct support service (production diagnostics): `http://13.60.31.141:5008`
- Direct support service (local launch profile): `http://localhost:5184`
- Direct support service (docker-compose): `http://localhost:5008`

Gateway prefix for support:
- `/support`

Front-end URL format through the gateway:
- `http://13.60.31.141/support/api/...`
- `http://localhost:5000/support/api/...`

## Common 404 Mistake

If you call support through the gateway, you must include the `/support` prefix.

- Correct: `http://13.60.31.141/support/api/...`
- Wrong: `http://13.60.31.141/api/...`
- Direct service calls do not use `/support`.

## Authentication

All support endpoints require a JWT bearer token, except the internal staff-status provisioning endpoint used for backend-to-backend integration.

```http
Authorization: Bearer <accessToken>
```

Token validation:
- Issuer: `myada-auth`
- Audience: `myada-api`
- Signing source: AuthService JWKS (`/.well-known/jwks.json`)

## Swagger

- Gateway: `http://13.60.31.141/support/swagger/index.html`
- Local gateway: `http://localhost:5000/support/swagger/index.html`
- Direct prod: `http://13.60.31.141:5008/swagger/index.html`
- Direct local: `http://localhost:5184/swagger/index.html`

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

`204 No Content` endpoints return an empty body.

## Categories

Base path: `/support/api/Categories`

- `GET /support/api/Categories`
- `GET /support/api/Categories/module/{module}`
- `POST /support/api/Categories`
- `DELETE /support/api/Categories/{id}`

Notes:
- `module` is the `SupportArea` enum in code.
- Route filtering expects `IT` or `FM`.
- On the wire, category DTOs use the API's default enum JSON behavior. If your client does not use string enum conversion, send the numeric enum value.

Example create payload:

```json
{
  "module": 0,
  "name": "Hardware"
}
```

`0 = IT`, `1 = FM`.

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
- `POST /support/api/SupportStaffStatuses/internal/provision`

Access rules:
- `GET /status/{status}`: any authenticated user
- `POST`, `GET /member/{memberId}`, `PUT /member/{memberId}`:
  - `admin`: may manage any `memberId`
  - `support`, `dispatcher`, `staff`: `memberId` must match the JWT `sub` claim
- `POST /internal/provision`: no JWT; requires `X-Internal-Key`

`memberId` format:
- GUID
- If `memberId` in the POST body is `00000000-0000-0000-0000-000000000000`, the backend uses the authenticated user id (`sub`)

Auto-create behavior:
- `GET /member/{memberId}` auto-creates a status row if one does not exist
- `PUT /member/{memberId}` also auto-creates when missing, using the requested status
- AuthService `POST /api/auth/create-user-with-role` auto-provisions this table for support roles

Example create payload:

```json
{
  "memberId": "7d75bf0d-c467-4ce7-8872-c58495a6ca8a",
  "status": "Online"
}
```

Example update payload:

```json
{
  "status": "OnBreak"
}
```

Internal provision payload:

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

Typical responses:
- `201 Created` for `POST`
- `200 OK` for `GET`
- `204 No Content` for `PUT`
- `403 Forbidden` when a non-admin user targets another `memberId`
- `401 Unauthorized` when the token or user id claim is missing
- `400 Bad Request` when `sub` is not a valid GUID or status is invalid

## Support Requests

Base path: `/support/api/SupportRequests`

Legacy alias:
- `/support/api/Requests`

Routes:
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

ID types:
- `requestId`: int
- `memberId`, `staffId`, `dispatcherId`: GUID

Upload constraints:
- Max file size: `10 MB`
- Allowed extensions: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`
- Form field name: `files`

Example upload result:

```json
[
  "https://example-bucket/support/requests/attachments/photo1.jpg"
]
```

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

Query filter note:
- The `GET /support/api/SupportRequests` endpoint currently applies `status`, `categoryId`, `service`, `memberId`, and `staffId`.
- The `RequestFilterDto` also exposes `search`, `priority`, `sortBy`, `page`, and `pageSize`, but the current service implementation does not use them yet.
- `status` accepts `open`, `New`, `Assigned`, `InProgress`, `Completed`, and `Cancelled` values in a case-insensitive way.

## Realtime (SignalR)

Hub endpoint:
- Gateway: `/support/hubs/support-updates`
- Local gateway: `http://localhost:5000/support/hubs/support-updates`
- Direct local: `http://localhost:5184/hubs/support-updates`
- Direct prod: `http://13.60.31.141:5008/hubs/support-updates`

JWT token is required for hub access.

## Quick cURL Example

```bash
curl -X GET "http://13.60.31.141/support/api/Categories" \
  -H "accept: application/json" \
  -H "Authorization: Bearer <accessToken>"
```

## Reference

Detailed internal endpoint descriptions and DTO examples:
- `Services/MyAdaSupportService/API_ENDPOINTS.md`
