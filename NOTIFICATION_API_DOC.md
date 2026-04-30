# Notification Service API Documentation

Last updated: 2026-04-30

## Overview

The Notification Service stores and serves in-app notifications and also handles email delivery.

Current responsibilities:

- list notifications for the signed-in user
- return one notification by id
- delete one owned notification
- send direct email notifications
- consume queued notification events from RabbitMQ and persist them

Supported channels:

- `Email`
- `Push`
- `Sms`

Important current behavior:

- public/backend access should use the API Gateway route on `myada.site`
- read and delete endpoints require JWT bearer auth
- direct email sending is exposed through HTTP
- queue-based notifications are used by backend services for most in-app notifications
- queued notifications are now stored for `recipientUserId` only; there is no fallback to `actorUserId`

## Base URL

Primary production backend URL:

- `https://myada.site/notification`

Useful local URLs:

- gateway: `http://localhost:5000/notification`
- service direct: `http://localhost:5004`
- docker internal: `http://notificationservice:8080`

Production examples:

- list notifications: `GET https://myada.site/notification/api/v1/notifications`
- current user notifications: `GET https://myada.site/notification/api/v1/notifications/me`
- send email: `POST https://myada.site/notification/api/v1/notifications/email`

## Authentication

These endpoints require `Authorization: Bearer <access-token>`:

- `GET /api/v1/notifications`
- `GET /api/v1/notifications/me`
- `GET /api/v1/notifications/{id}`
- `DELETE /api/v1/notifications/{id}`

Behavior:

- notification reads are always scoped to the signed-in user
- visible records are notifications where `recipientUserId == currentUserId`
- delete only works for records where `recipientUserId == currentUserId`

This endpoint does not require bearer auth:

- `POST /api/v1/notifications/email`

If `POST /email` is called without auth and without `recipientUserId`, the stored notification is created with `recipientUserId = null`.

## Notification Shape

Stored notification fields:

- `id` (`Guid`)
- `type` (`string`)
- `message` (`string`)
- `notificationType` (`Email | Push | Sms`)
- `recipientUserId` (`Guid?`)
- `createdAt` (`DateTime`, UTC)

HTTP response item shape:

```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001",
  "type": "club",
  "message": "New event alert",
  "channel": "Push",
  "recipientUserId": "00000000-0000-0000-0000-000000000001",
  "createdAt": "2026-04-30T09:30:00Z"
}
```

## HTTP Endpoints

### `GET /api/v1/notifications`

Returns paginated notifications ordered from newest to oldest.

Query parameters:

- `type` (`string`, optional, default `all`)
- `channel` (`string`, optional, default `all`)
- `recipientUserId` (`Guid`, optional)
- `page` (`int`, optional, default `1`)
- `limit` (`int`, optional, default `20`, clamped to `1..100`)

Filter behavior:

- `type` uses case-insensitive substring matching
- `channel` accepts `all`, `Email`, `Push`, or `Sms`
- invalid `channel` returns `400 Bad Request`
- `recipientUserId` only narrows the already user-visible set; it does not bypass auth scoping

Example request:

```http
GET /api/v1/notifications?type=club&channel=Push&page=1&limit=10
Authorization: Bearer <access-token>
```

Example response:

```json
{
  "items": [
    {
      "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001",
      "type": "New Event Alert",
      "message": "AI Career Fair",
      "channel": "Push",
      "recipientUserId": "00000000-0000-0000-0000-000000000001",
      "createdAt": "2026-04-30T09:30:00Z"
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

### `GET /api/v1/notifications/me`

Returns notifications for the user identified by the bearer access token.

Query parameters:

- `type` (`string`, optional, default `all`)
- `channel` (`string`, optional, default `all`)
- `page` (`int`, optional, default `1`)
- `limit` (`int`, optional, default `20`, clamped to `1..100`)

Important behavior:

- this route automatically resolves the current user from the access token
- it does not need `recipientUserId`
- supported user id claim lookup includes `sub`, `user_id`, `nameid`, and `ClaimTypes.NameIdentifier`

### `GET /api/v1/notifications/{id}`

Returns one visible notification for the signed-in user.

Success response:

```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaa0001",
  "type": "New Event Alert",
  "message": "AI Career Fair",
  "channel": "Push",
  "recipientUserId": "00000000-0000-0000-0000-000000000001",
  "createdAt": "2026-04-30T09:30:00Z"
}
```

Status codes:

- `200 OK`
- `401 Unauthorized`
- `404 Not Found`

### `DELETE /api/v1/notifications/{id}`

Deletes one owned notification.

Status codes:

- `204 No Content`
- `401 Unauthorized`
- `404 Not Found`

Important delete rule:

- legacy notifications with `recipientUserId = null` are readable but not deletable through this route

### `POST /api/v1/notifications/email`

Sends a plain-text email through SMTP and stores a notification with channel `Email`.

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
- `toEmail` (`string`, required, valid email)
- `subject` (`string`, required, max `200`)
- `message` (`string`, required, max `4000`)

Behavior:

- `toEmail`, `subject`, and `message` are trimmed
- if `type` is missing or blank, stored type becomes `Direct Email`
- if `recipientUserId` is provided, that user owns the stored notification
- if `recipientUserId` is omitted and the request is authenticated, ownership falls back to the authenticated user
- if `recipientUserId` is omitted and the request is unauthenticated, ownership is stored as `null`
- the outgoing email body is plain text
- notifications created with `recipientUserId = null` are not returned from authenticated read endpoints

Success response:

```json
{
  "message": "Email notification sent successfully."
}
```

Common error cases:

- `400 Bad Request` for invalid payloads, invalid email format, or empty trimmed fields
- `502 Bad Gateway` when SMTP configuration is invalid
- `502 Bad Gateway` when the SMTP server rejects the message

## Queue-Based Notifications

The service runs a RabbitMQ background consumer for internal backend notifications.

Queue details:

- queue name: `notification-queue`
- config key: `RabbitMQ:Host`

Primary message contract:

```json
{
  "type": "New Event Alert",
  "message": "AI Career Fair",
  "channel": "Push",
  "source": "club-management",
  "action": "event.created_alert",
  "entityType": "event",
  "entityId": "123",
  "actorUserId": "11111111-1111-1111-1111-111111111111",
  "recipientUserId": "22222222-2222-2222-2222-222222222222",
  "recipientEmail": "student@example.com",
  "emailSubject": "New Event Alert",
  "occurredAtUtc": "2026-04-30T09:30:00Z"
}
```

Supported fields:

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

- if `channel` parses to a valid enum, that channel is used
- otherwise, if `recipientEmail` exists, channel resolves to `Email`
- otherwise, channel resolves to `Push`

Queue processing behavior:

- the notification is persisted first
- persisted `recipientUserId` is exactly `queueMessage.recipientUserId`
- there is no fallback to `actorUserId`
- if resolved channel is `Email` and `recipientEmail` exists, SMTP delivery is attempted
- if email sending fails, the stored notification remains in the database and the failure is only logged
- if an email notification has no `recipientEmail`, it is still persisted and email sending is skipped

Legacy compatibility:

- `LostItemCreatedEvent` is still supported
- legacy lost-and-found events are stored as `Push` notifications with type `New Lost Item`

## Current Backend Producers

Current backend integrations in this repo:

- `AuthService`
  - uses `POST /api/v1/notifications/email` for password reset mail flows
- `MyAdaSupportService`
  - publishes queue notifications for task assignment, cancellation, and status changes
- `LostFoundService`
  - publishes queue notifications for owner alerts such as `Your Item is Found` and `You can Get your Item`
- `ClubManagementService`
  - publishes queue notifications for club, event, proposal, vacancy, reminder, logo, employee, and membership flows

Examples of notification titles currently produced by backend services:

- `New Task Assigned`
- `Request Canceled`
- `Your Item is Found`
- `New Vacancy at {Club Name}`
- `Your Club Proposal is approved`
- `New Club Alert`
- `Your Event Proposal is approved`
- `All seats are taken for the event`
- `Club Logo Change has been Approved`
- `Club Logo Change has been Disapproved`

## Startup and Seeding

On startup the service:

- applies EF Core migrations automatically
- seeds demo notifications if the table is empty

The current seed creates sample notifications for:

- `club`
- `academic`
- `campus`
- `system`

## Runtime Configuration

Main configuration keys:

- `ConnectionStrings:Develop`
- `RabbitMQ:Host`
- `Auth:JwksUrl`
- `Auth:Issuer`
- `Auth:Audience`
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

Docker mapping in this repo:

- host `5004` -> container `8080`
- gateway route prefix `/notification` -> Notification Service

## Swagger

Swagger is enabled only when the Notification Service itself is running in `Development`.

In this repo's local Docker compose:

- `notificationservice` runs as `Development`
- `apigateway` runs as `Production`

Local swagger URLs:

- `http://localhost:5004/swagger/index.html`
- `http://localhost:5000/notification/swagger/index.html`

For production, use the gateway API routes on `https://myada.site/notification/...`. If swagger is exposed through the deployment gateway, the expected path is:

- `https://myada.site/notification/swagger/index.html`

## Quick Test Examples

List current user notifications through production gateway:

```bash
curl "https://myada.site/notification/api/v1/notifications/me?page=1&limit=20" \
  -H "Authorization: Bearer <access-token>"
```

Filter by type and channel:

```bash
curl "https://myada.site/notification/api/v1/notifications?type=club&channel=Push&page=1&limit=10" \
  -H "Authorization: Bearer <access-token>"
```

Send email through production gateway:

```bash
curl -X POST "https://myada.site/notification/api/v1/notifications/email" \
  -H "Content-Type: application/json" \
  -d "{\"type\":\"manual.test\",\"toEmail\":\"student@example.com\",\"subject\":\"Test\",\"message\":\"Hello from Notification Service\"}"
```
