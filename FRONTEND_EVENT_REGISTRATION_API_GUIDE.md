# Event Registration & Tickets API — Frontend integration guide

This document describes the **event registration + ticket QR + “My Tickets”** contracts implemented in this project.

Base URL is assumed to be `{CLUB_API_BASE}/api/v1` (no trailing slash). API versioning uses `v1` in the path.

For convenience, the backend also exposes **unversioned aliases** for the same endpoints under `{CLUB_API_BASE}/api/...`:

- Preferred: `.../api/v1/...`
- Also supported: `.../api/...`

Unless noted otherwise, responses are wrapped by **AutoWrapper** as `{ "result": ... }` or `{ "data": ... }` (depending on configuration). Your `unwrapApiResponse` should read **`result`** or **`data`**.

Errors often surface as `{ "message": "..." }`. Prefer the first available of **`message`**, **`title`**, or **`detail`** for user-facing text.

---

## 1) Authentication

All endpoints in this document require:

- `Authorization: Bearer <access_token>`

Backend identifies the current user from JWT claim:

- `sub` (preferred) or `user_id` (fallback)

---

## 2) Data model (what the backend persists)

Backend stores **one registration per user per event** (unique by `(eventId, userId)`).

When a student registers, backend persists:

- **Ticket identity**
  - `ticketId` (stable, unique)
  - `jwt` (stable; returned again by `GET /ticket` so the frontend can regenerate the QR deterministically)
- **Event snapshot** (for “My Tickets” cards)
  - `eventNameSnapshot`
  - `eventImageUrlSnapshot` (**this is the “persistent ticket image” requirement**)
  - `eventStartTimeSnapshot`, `eventEndTimeSnapshot`
  - `eventLocationSnapshot`
  - `eventSeatLimitSnapshot`
- **Check-in fields**
  - `isCheckedIn`, `checkedInAt`, `checkedInBy`, `scanCount`, `lastScannedAt`

---

## 3) Ticket JWT (QR content)

The **QR should contain the JWT** returned by the backend.

### Required claims

JWT claims include at least:

- `eventId`
- `userId` (**attendee id**)
- `ticketId`
- `iat`
- `exp`
- Optional: `iss`, `aud`

### Security note (important)

Even if the JWT is valid, the scanner must still call the backend to validate against DB state:

- ticket/registration may have been cancelled/invalidated
- ticket may already be checked in

So the real flow is: **JWT valid → DB lookup → business checks → check-in update**

---

## 4) Endpoints (student)

### 4.1 Register for an event (creates or returns existing ticket)

**Endpoint**

- `POST /api/v1/events/{eventId}/registrations`
- Alias: `POST /api/events/{eventId}/registrations`

**Auth**

- Required

**Request body**

- No body required.

**Behavior**

- Reject if event is full (seat limit reached).
- **No duplicates**:
  - If the user is already registered, backend returns the **existing** ticket again (same `ticketId` and same `jwt`).

**Success response shape (inside AutoWrapper `result`)**

```json
{
  "ticketId": "TCK-9F2C3B1A0D4E5F67",
  "eventId": "42",
  "userId": "9b2f1b3a-aaaa-bbbb-cccc-1234567890ab",
  "jwt": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "attendeeId": "9b2f1b3a-aaaa-bbbb-cccc-1234567890ab",
  "event": {
    "id": "42",
    "name": "AI Workshop",
    "date": "2026-04-30",
    "startTime": "10:00",
    "endTime": "12:00",
    "location": "Room 302",
    "clubName": null,
    "imageUrl": "https://.../events/42/cover.png",
    "seatLimit": 150,
    "registeredCount": 120
  }
}
```

**Error cases**

- **HTTP 404**: event not found
- **HTTP 409**: seat limit reached

---

### 4.2 Get my tickets (for “My Tickets” page)

**Endpoint**

- `GET /api/v1/users/me/event-registrations`
- Alias: `GET /api/users/me/event-registrations`

**Auth**

- Required

**Behavior**

- Returns a list of the current user’s registrations.
- Each item contains an **event snapshot** including `imageUrl`, so the UI can render ticket cards **without refetching** event details.

**Success response shape (inside AutoWrapper `result`)**

```json
{
  "items": [
    {
      "eventId": "42",
      "ticketId": "TCK-9F2C3B1A0D4E5F67",
      "registeredAt": "2026-04-22T10:15:00.0000000Z",
      "event": {
        "id": "42",
        "name": "AI Workshop",
        "imageUrl": "https://.../events/42/cover.png",
        "startTime": "10:00",
        "endTime": "12:00",
        "location": "Room 302",
        "seatLimit": 150,
        "registeredCount": 120
      }
    }
  ]
}
```

**Notes**

- `registeredAt` is taken from the registration record creation time (UTC).
- `registeredCount` is computed server-side per event.

---

### 4.3 Get ticket again (when user opens the ticket screen)

**Endpoint**

- `GET /api/v1/events/{eventId}/ticket`
- Alias: `GET /api/events/{eventId}/ticket`

**Auth**

- Required

**Behavior**

- Returns the **same ticket every time** for the same `(eventId, userId)`.
- Returns the stored `jwt` again so the frontend can regenerate the QR.

**Success response shape (inside AutoWrapper `result`)**

```json
{
  "ticketId": "TCK-9F2C3B1A0D4E5F67",
  "eventId": "42",
  "userId": "9b2f1b3a-aaaa-bbbb-cccc-1234567890ab",
  "jwt": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "attendeeId": "9b2f1b3a-aaaa-bbbb-cccc-1234567890ab",
  "eventImageUrl": "https://.../events/42/cover.png"
}
```

**Error cases**

- **HTTP 404**: user is not registered for this event

---

## 5) Endpoints (staff scanner)

### 5.1 Check-in a ticket (scan validation + DB enforcement)

**Endpoint**

- `POST /api/v1/events/{eventId}/check-in`
- Alias: `POST /api/events/{eventId}/check-in`

**Auth**

- Required
- Staff-only via roles: `admin`, `staff` (case-insensitive variants supported in code)

**Request body**

```json
{
  "jwt": "scanned_ticket_jwt_here"
}
```

**Behavior**

Backend will:

- verify JWT signature
- verify token is not expired
- read claims (`eventId`, `userId`, `ticketId`, `iat`, `exp`)
- compare route `{eventId}` to claim `eventId`
- lookup registration by `ticketId` and confirm it matches event + user
- enforce check-in window: **from 1 hour before event start until event end**
- first valid scan: marks checked in (`IsCheckedIn=true`, `CheckedInAt=UtcNow`, `CheckedInBy=staffId`)
- subsequent scans: returns `already_checked_in`

**Success response (first valid scan)**

```json
{
  "success": true,
  "status": "checked_in",
  "message": "Entry allowed",
  "ticketId": "TCK-9F2C3B1A0D4E5F67",
  "attendeeId": "9b2f1b3a-aaaa-bbbb-cccc-1234567890ab",
  "eventId": "42",
  "checkedInAt": "2026-04-30T09:55:12.0000000Z"
}
```

**Duplicate scan response**

```json
{
  "success": false,
  "status": "already_checked_in",
  "message": "This ticket was already used.",
  "ticketId": "TCK-9F2C3B1A0D4E5F67",
  "attendeeId": "9b2f1b3a-aaaa-bbbb-cccc-1234567890ab",
  "eventId": "42",
  "checkedInAt": "2026-04-30T09:55:12.0000000Z"
}
```

**Other possible statuses**

- `invalid_ticket`
- `expired_ticket`
- `ticket_not_found`
- `event_mismatch`
- `outside_checkin_window`

---

## 6) Frontend implementation notes

### 6.1 Persisting ticket cards without extra refetch

Use `GET /users/me/event-registrations` to render “My Tickets”:

- `event.imageUrl` is already included (snapshot stored at registration time)
- you don’t need to call `GET /events/{eventId}` just to render cards

### 6.2 QR generation

- QR content should be the returned `jwt` string.
- If the ticket screen is opened later, call `GET /events/{eventId}/ticket` to get the **same `jwt`** again.

### 6.3 Registration button behavior

If you call `POST /events/{eventId}/registrations` multiple times:

- you will get the **same** ticket back (no duplicates)
- if capacity is reached, you’ll get HTTP 409

---

## 7) Backend configuration required (for ticket JWT signing)

Ticket JWTs use a **backend-local symmetric signing key** (separate from your AuthService JWKS).

Configure:

- `Tickets:SigningKey` (required, **min 32 characters**)
- Optional: `Tickets:Issuer` (default `myada-tickets`)
- Optional: `Tickets:Audience` (default `myada-scanner`)

