# MyAda Support API Documentation for Frontend

Last updated: 2026-04-14

## Base URLs

Recommended via API Gateway:
- Production: `http://51.20.193.29:5000`
- Local: `http://localhost:5000`

Support service gateway prefix:
- `/support`

Final frontend URL format:
- `http://51.20.193.29:5000/support/api/...`

Direct service (diagnostics only):
- `http://51.20.193.29:5008/api/...`

## Authentication

All Support endpoints require JWT bearer token.

Header:

```http
Authorization: Bearer <accessToken>
```

Token validation settings:
- Issuer: `myada-auth`
- Audience: `myada-api`
- Signing source: AuthService JWKS (`/.well-known/jwks.json`)

## Swagger

- Gateway: `http://51.20.193.29:5000/support/swagger/index.html`
- Direct: `http://51.20.193.29:5008/swagger/index.html`

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

- `GET /support/api/SupportStaffStatuses/member/{memberId}`
- `GET /support/api/SupportStaffStatuses/status/{status}`
- `PUT /support/api/SupportStaffStatuses/member/{memberId}`

Example update payload:

```json
{
  "status": "OnBreak"
}
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
curl -X GET "http://51.20.193.29:5000/support/api/Categories" \
  -H "accept: application/json" \
  -H "Authorization: Bearer <accessToken>"
```

## Reference

Detailed internal endpoint descriptions and DTO examples:
- `Services/MyAdaSupportService/API_ENDPOINTS.md`
