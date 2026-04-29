# Notification Service API Documentation

Last updated: 2026-04-30

## Overview

The Notification Service is responsible for:

- listing stored notifications
- returning a single notification by id
- deleting a stored notification
- sending direct email notifications over SMTP
- consuming queued notification messages from RabbitMQ and persisting them

Supported notification channels in the domain model are:

- `Email`
- `Push`
- `Sms`

Important current behavior:

- The user-facing HTTP API now requires JWT auth for notification reads.
- The primary frontend read route is `GET /api/v1/notifications/me`.
- `GET /api/v1/notifications` is also user-scoped and returns the signed-in user's visible notifications.
- The HTTP API only creates notifications through `POST /api/v1/notifications/email`.
- `Push` and `Sms` notifications are normally created through RabbitMQ or seeded demo data.
- Direct email requests store `type`, `message`, `channel`, `createdAt`, and optional `recipientUserId` in the database, but do not store recipient email or email subject.

## Base URLs

Use the API Gateway for frontend/public access:

- Production gateway base: `https://myada.site/notification`
- Local gateway base: `http://localhost:5000/notification`
- Gateway path prefix: `/notification`

Use the service directly only for local debugging or internal service-to-service calls:

- Production direct base: `http://13.60.31.141:5004`
- Local direct base: `http://localhost:5004`
- Docker internal base: `http://notificationservice:8080`

Examples:

- Gateway list endpoint: `GET https://myada.site/notification/api/v1/notifications`
- Direct list endpoint: `GET http://13.60.31.141:5004/api/v1/notifications`

Swagger is registered by the Notification Service only in Development environment.

In the current repo `docker-compose.yml`:

- `notificationservice` runs with `ASPNETCORE_ENVIRONMENT=Development`
- `apigateway` runs with `ASPNETCORE_ENVIRONMENT=Production`

That means the upstream service exposes Swagger, and the gateway can proxy it at the notification route prefix.

Known swagger URLs:

- Production gateway swagger: `https://myada.site/notification/swagger/index.html`
- Production direct swagger: `http://13.60.31.141:5004/swagger/index.html`
- Local gateway swagger: `http://localhost:5000/notification/swagger/index.html`
- Local direct swagger: `http://localhost:5004/swagger/index.html`

Important routing note:

- the deployed notification gateway route is `https://myada.site/notification/...`
- `https://myada.site/` by itself is the site root, not the Notification Service base path

## Authentication

Notification Service now validates JWT bearer tokens using the AuthService JWKS endpoint for user-scoped read operations.

That means:

- `GET /api/v1/notifications`
- `GET /api/v1/notifications/me`
- `GET /api/v1/notifications/{id}`
- `DELETE /api/v1/notifications/{id}`

all require a bearer token and are scoped to the signed-in user.

`POST /api/v1/notifications/email` remains available for internal callers such as AuthService.

## CORS

Notification Service currently registers an `AllowAll` CORS policy and applies it globally.

That means the service allows:

- any origin
- any header
- any HTTP method

If stricter browser access rules are needed later, they would need to be added in this service or enforced at the gateway.

## Stored Notification Shape

Each stored notification contains:

- `id` (`Guid`)
- `type` (`string`)
- `message` (`string`)
- `notificationType` (`Email | Push | Sms`)
- `recipientUserId` (`Guid?`)
- `createdAt` (`DateTime`, UTC)

## HTTP Endpoints

### `GET /api/v1/notifications`

### `GET /api/v1/notifications/me`

Returns paginated notifications ordered by newest first for the signed-in user.

Authentication:

- requires `Authorization: Bearer <token>`

Query parameters:

- `type` (`string`, optional, default `all`)
- `channel` (`string`, optional, default `all`)
- `page` (`int`, optional, default `1`, minimum effective value `1`)
- `limit` (`int`, optional, default `20`, clamped to `1..100`)

Filter behavior:

- `type` uses case-insensitive substring matching
- `channel` accepts `all`, `Email`, `Push`, or `Sms`
- invalid `channel` returns `400 Bad Request`
- results are scoped to notifications whose `recipientUserId` matches the current user
- legacy notifications with `recipientUserId = null` are also visible for backward compatibility

Example request:

```http
GET /api/v1/notifications?type=club&channel=Push&page=1&limit=10
```

Example success response:

```json
{
  "items": [
    {
      "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001",
      "type": "club",
      "message": "ADA Entrepreneurship Club: Startup Weekend registration closes this Friday at 18:00 Baku time.",
      "channel": "Push",
      "createdAt": "2026-04-22T10:00:00Z"
    }
  ],
  "total": 1,
  "page": 1,
  "limit": 10
}
```

Example invalid channel response:

```json
{
  "error": "Invalid channel value. Allowed: all, Email, Push, Sms"
}
```

### `GET /api/v1/notifications/{id}`

Returns a single visible notification for the signed-in user.

Authentication:

- requires `Authorization: Bearer <token>`

Success response:

```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001",
  "type": "club",
  "message": "ADA Entrepreneurship Club: Startup Weekend registration closes this Friday at 18:00 Baku time.",
  "channel": "Push",
  "createdAt": "2026-04-22T10:00:00Z"
}
```

Status codes:

- `200 OK` when found
- `401 Unauthorized` when the bearer token is missing or invalid
- `404 Not Found` when no notification exists for the id

### `DELETE /api/v1/notifications/{id}`

Deletes a stored notification owned by the signed-in user.

Authentication:

- requires `Authorization: Bearer <token>`

Status codes:

- `204 No Content` when deleted
- `401 Unauthorized` when the bearer token is missing or invalid
- `404 Not Found` when no notification exists for the id

Important delete rule:

- this delete route only deletes notifications whose `recipientUserId` matches the current user
- legacy notifications with `recipientUserId = null` are readable but not deletable through this user route

### `POST /api/v1/notifications/email`

Sends a plain-text email through SMTP and also stores a notification record with channel `Email`.

Request body:

```json
{
  "type": "auth.password_reset",
  "recipientUserId": "22222222-2222-2222-2222-222222222222",
  "toEmail": "student@example.com",
  "subject": "Reset your password",
  "message": "Use the link in the app to reset your password."
}
```

Request fields:

- `type` (`string`, optional)
- `recipientUserId` (`Guid`, optional)
- `toEmail` (`string`, required, must be a valid email address)
- `subject` (`string`, required, max length `200`)
- `message` (`string`, required, max length `4000`)

Behavior:

- leading and trailing whitespace is trimmed from `toEmail`, `subject`, and `message`
- if `type` is missing or blank, stored notification type becomes `Direct Email`
- if `recipientUserId` is supplied, the stored notification is owned by that user
- if `recipientUserId` is omitted and the request is authenticated, the stored notification falls back to the authenticated user id
- email body is sent as plain text, not HTML

Success response:

```json
{
  "message": "Email notification sent successfully."
}
```

Common error cases:

- `400 Bad Request` for invalid model shape, invalid email format, or oversized fields
- validation errors from `[ApiController]` use the standard ASP.NET Core validation response shape
- `400 Bad Request` with a custom error object when required fields become empty after trimming
- `502 Bad Gateway` if SMTP configuration is invalid
- `502 Bad Gateway` if the SMTP server rejects authentication or sender details

## Internal Queue-Based Notifications

The service also runs a hosted RabbitMQ consumer for internal notifications.

Queue details:

- queue name: `notification-queue`
- host config key: `RabbitMQ:Host`

Primary message contract:

```json
{
  "type": "club.event_created",
  "message": "New event is now open for registration.",
  "channel": "Email",
  "source": "ClubManagementService",
  "action": "Created",
  "entityType": "Event",
  "entityId": "123",
  "actorUserId": "11111111-1111-1111-1111-111111111111",
  "recipientUserId": "22222222-2222-2222-2222-222222222222",
  "recipientEmail": "student@example.com",
  "emailSubject": "New event available",
  "occurredAtUtc": "2026-04-24T09:00:00Z"
}
```

Message contract fields:

- `type`
- `message`
- `channel`
- `source`
- `action`
- `entityType`
- `entityId`
- `actorUserId`
- `recipientUserId`
- `recipientEmail`
- `emailSubject`
- `occurredAtUtc`

Queue resolution rules:

- if `channel` is a valid enum value, that channel is used
- otherwise, if `recipientEmail` is present, the notification resolves to `Email`
- otherwise, the notification resolves to `Push`

Queue processing behavior:

- every supported queue message is first persisted to the `Notifications` table
- persisted notifications use `recipientUserId = recipientUserId ?? actorUserId`
- if the resolved channel is `Email`, the service then attempts SMTP delivery
- if SMTP delivery fails, the notification stays persisted and the failure is only logged
- if an `Email` notification has no `recipientEmail`, the notification is persisted and email sending is skipped
- unsupported payloads are acknowledged and logged as warnings

Legacy compatibility:

- the consumer still supports `LostItemCreatedEvent`
- legacy lost-and-found messages are converted to a `Push` notification with type `New Lost Item`

## Current Service Integrations

Known integrations in this repo:

- `AuthService` calls `POST /api/v1/notifications/email` for password reset email flows
- `ClubManagementService` publishes `NotificationQueueMessage` payloads to RabbitMQ

## Startup and Seeding

On startup, Notification Service:

- applies Entity Framework migrations automatically
- seeds sample notifications if the `Notifications` table is empty

The seed currently inserts demo notifications such as:

- club
- academic
- campus
- system

## Runtime Configuration

Main configuration keys:

- `ConnectionStrings:Develop`
- `RabbitMQ:Host`
- `Smtp:Host`
- `Smtp:Port`
- `Smtp:Username`
- `Smtp:Password`
- `Smtp:FromEmail`
- `Smtp:FromName`
- `Smtp:EnableSsl`
- `Smtp:UseDefaultCredentials`

SMTP rules from the current implementation:

- `Smtp:Host` must be set
- `Smtp:Port` must be greater than `0`
- if `Smtp:UseDefaultCredentials=false`, both `Smtp:Username` and `Smtp:Password` are required
- if `Smtp:FromEmail` is empty, the service falls back to `Smtp:Username`

Docker compose mapping in this repo:

- host port `5004` -> container port `8080`
- gateway prefix `/notification` -> Notification Service

SMTP deployment note:

- this repo's Docker stack expects SMTP values from environment variables such as `NOTIFICATION_SMTP_HOST`, `NOTIFICATION_SMTP_PORT`, `NOTIFICATION_SMTP_USERNAME`, `NOTIFICATION_SMTP_PASSWORD`, and `NOTIFICATION_SMTP_FROM_EMAIL`
- for Gmail, `NOTIFICATION_SMTP_PASSWORD` must be an app password, not the normal mailbox password

## Quick Test Examples

List notifications through gateway:

```bash
curl "https://myada.site/notification/api/v1/notifications/me?page=1&limit=20" \
  -H "Authorization: Bearer <access-token>"
```

Send email directly to the service:

```bash
curl -X POST "http://13.60.31.141:5004/api/v1/notifications/email" \
  -H "Content-Type: application/json" \
  -d "{\"type\":\"manual.test\",\"toEmail\":\"student@example.com\",\"subject\":\"Test\",\"message\":\"Hello from Notification Service\"}"
```
