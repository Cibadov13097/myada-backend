# Club Management API Documentation for Frontend

Last updated: 2026-04-19

This document describes the **Club Management** microservice REST API (`ClubManagementService.API`). All routes use URL-based versioning: **`api/v1/...`** (only **v1** is supported).

---

## Base URLs

| Environment | Base URL | Notes |
|-------------|----------|--------|
| **Local (Docker Compose)** | `http://localhost:5003` | Service port mapping from `docker-compose.yml` (`clubmanagementservice` → host `5003`). |
| **Deployed (via API Gateway)** | `http://51.20.193.29:5000/club` | Gateway strips the `/club` prefix and forwards to the club service. Full path pattern: **`http://51.20.193.29:5000/club/api/v1/...`**. |

**Examples**

- Local Swagger: `http://localhost:5003/swagger`
- Local categories: `http://localhost:5003/api/v1/categories`
- Deployed categories: `http://51.20.193.29:5000/club/api/v1/categories`

Direct service (diagnostics only):
- Production: `http://51.20.193.29:5003/api/v1/...`
- Local: `http://localhost:5003/api/v1/...`

---

## API Gateway (deployed)

From `ApiGateway/appsettings.json`, route **`club_api`** matches **`/club/{**catch-all}`** and applies **`PathRemovePrefix: /club`**. The club container still receives paths starting with **`/api/v1/...`**.

## Common 404 Mistake (Important)

If you call Club through gateway (`http://51.20.193.29:5000`), you must include `/club`.

- Correct: `http://51.20.193.29:5000/club/api/v1/categories`
- Wrong: `http://51.20.193.29:5000/api/v1/categories` (returns 404 on gateway)

---

## Authentication

- Most mutating and club-scoped endpoints require **`Authorization: Bearer <JWT>`**.
- JWT is validated using keys from the Auth service **JWKS** (`Auth:JwksUrl` in configuration). Issuer / audience default to **`myada-auth`** / **`myada-api`** unless overridden.
- User identity is read from JWT claims **`sub`** or **`user_id`** (GUID).
- **Categories**, **Subevents**, **Event registrations** (controller-level): no `[Authorize]` on the controller — treat as **anonymous** unless a future global policy is added.
- **Job applications**: `GET` endpoints are **`[AllowAnonymous]`**; `POST` / `PATCH` / `DELETE` require auth.
- In **Development**, if JWKS cannot be reached after retries, the API may fall back to a **development bearer bypass** (see `Program.cs`); do not rely on this in production.

---

## Response shape (AutoWrapper)

Successful JSON responses are passed through **AutoWrapper** (`UseApiResponseAndExceptionWrapper`). Clients should expect a **wrapper object** (for example a `result` payload plus status metadata), not always the raw controller return type. See Swagger “Schema” or sample responses for the exact shape.

Errors are normalized (e.g. `KeyNotFoundException` → **404** via `HttpExceptionFilter`).

---

## Swagger / OpenAPI

- **Local:** `http://localhost:5003/swagger` (when `ASPNETCORE_ENVIRONMENT=Development`).
- OpenAPI document group: **`v1`**.

---

## Common conventions

- **`Content-Type`**: `application/json` unless noted (`multipart/form-data` for file uploads).
- **Pagination**: many list endpoints accept **`page`** (default `1`) and **`limit`** (varies by endpoint).
- **`clubId` in club-admin routes**: must parse as a **positive integer** (club route key).

---

# Endpoints by area

Below, **`{origin}`** means your base URL without trailing slash:

- Local: `{origin} = http://localhost:5003`
- Deployed: `{origin} = http://51.20.193.29:5000/club`

All paths below are relative to `{origin}` (e.g. `{origin}/api/v1/categories`).

---

## Categories — `api/v1/categories`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/v1/categories` | No | List all categories. |
| `GET` | `/api/v1/categories/{id}` | No | Get category by id. |
| `POST` | `/api/v1/categories` | No | Create. Body: **`CreateCategoryDto`** `{ "name": string }`. |
| `PUT` | `/api/v1/categories/{id}` | No | Update. Body: **`UpdateCategoryDto`** `{ "name": string }`. |
| `DELETE` | `/api/v1/categories/{id}` | No | Delete. |

**DTOs:** `CategoryDto` — `{ id, name }`.

---

## Clubs — `api/v1/clubs`

Controller is **`[Authorize]`** for all actions.

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/clubs` | Query: **`search`**, **`category`**, **`page`**, **`limit`** (default `12`). Returns `{ items, total, page, limit }` with a shaped club card projection. |
| `GET` | `/api/v1/clubs/{clubId}` | Club profile-style projection. |
| `GET` | `/api/v1/clubs/{clubId}/members` | Stub: empty `items`. |
| `POST` | `/api/v1/clubs/{clubId}/join-applications` | **`multipart/form-data`**. Form: **`letterOfPurpose`**, optional **`portfolioLinks`**, optional **`portfolioFiles`**. Returns synthetic `applicationId`. |
| `POST` | `/api/v1/clubs` | Create club. Body: **`CreateClubDto`**. |
| `PATCH` | `/api/v1/clubs/{clubId}` | Body: **`UpdateClubDto`**. |
| `DELETE` | `/api/v1/clubs/{clubId}` | Delete club. |

---

## Events — `api/v1/events`

**`[Authorize]`** on controller.

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/events` | Query: **`clubId`**, **`myClubsOnly`** (requires user; uses club memberships), **`search`**, **`page`**, **`limit`** (default page size **12** for v1). Returns `{ items, total, page, limit }`. |
| `GET` | `/api/v1/events/{eventId}` | **`EventDto`**. |
| `POST` | `/api/v1/events/{eventId}/registrations` | Register current user for event. **409** if already registered or seat limit reached. Returns ticket-style payload. |
| `DELETE` | `/api/v1/events/{eventId}/registrations` | Cancel current user’s registration. **404** if none. |
| `GET` | `/api/v1/events/{eventId}/ticket` | Ticket payload for current user’s registration. |
| `POST` | `/api/v1/events` | Create. Body: **`CreateEventDto`**. |
| `PATCH` | `/api/v1/events/{eventId}` | Body: **`UpdateEventDto`**. |
| `DELETE` | `/api/v1/events/{eventId}` | Delete event. |

---

## Event registrations — `api/v1/event-registrations`

No controller-level **`[Authorize]`** (anonymous unless globally enforced).

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/event-registrations` | List all registrations. |
| `GET` | `/api/v1/event-registrations/by-event/{eventId}` | By event. |
| `GET` | `/api/v1/event-registrations/{id}` | By id. |
| `POST` | `/api/v1/event-registrations` | Body: **`CreateEventRegistrationDto`** (`EventId`, `UserId`, …). |
| `DELETE` | `/api/v1/event-registrations/{id}` | Delete. |

---

## Subevents — `api/v1/subevents`

No controller-level **`[Authorize]`**.

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/subevents` | List all. |
| `GET` | `/api/v1/subevents/by-event/{mainEventId}` | By parent event. |
| `GET` | `/api/v1/subevents/{id}` | By id. |
| `POST` | `/api/v1/subevents` | Body: **`CreateSubeventDto`**. |
| `PUT` | `/api/v1/subevents/{id}` | Body: **`UpdateSubeventDto`**. |
| `DELETE` | `/api/v1/subevents/{id}` | Delete. |

---

## Vacancies — `api/v1/vacancies`

**`[Authorize]`**. Public list/detail omit **draft** vacancies and inactive ones where enforced in service.

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/vacancies` | Query: **`search`**, **`category`** (ignored), **`savedOnly`** (ignored), **`page`**, **`limit`**. |
| `GET` | `/api/v1/vacancies/{vacancyId}` | **`vacancyId`** is integer string. **404** if draft or inactive. |
| `POST` | `/api/v1/vacancies/{vacancyId}/applications` | **`multipart/form-data`**: **`purposeOfApplication`** (required), optional **`cvFile`**. |
| `GET` | `/api/v1/vacancies/by-club/{clubId}` | Active, non-draft vacancies for club. |
| `POST` | `/api/v1/vacancies` | Body: **`CreateJobVacancyDto`**: `clubId`, `title`, `description`, `postedBy`, optional `clubPositionId`, optional `applicationDeadline`, **`publish`** (default `true`). Either **title** or **clubPositionId** must be meaningful per validation. |
| `PATCH` | `/api/v1/vacancies/{vacancyId}` | Body: **`UpdateJobVacancyDto`** (partial). |
| `DELETE` | `/api/v1/vacancies/{vacancyId}` | Delete. |

**`JobVacancyDto`** (representative): `id`, `clubId`, `clubName`, `title`, `description`, `postedBy`, `isActive`, `isDraft`, `clubPositionId`, `applicationDeadline`, `status` (`draft` \| `active` \| `inactive`), `createdAt`.

---

## Job applications — `api/v1/job-applications`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/v1/job-applications` | Anonymous | List all. |
| `GET` | `/api/v1/job-applications/by-vacancy/{jobVacancyId}` | Anonymous | By vacancy. |
| `GET` | `/api/v1/job-applications/{id}` | Anonymous | By id. |
| `POST` | `/api/v1/job-applications` | **Bearer** | Body: **`CreateJobApplicationDto`**. |
| `PATCH` | `/api/v1/job-applications/{id}/status` | **Bearer** | Body: **`UpdateJobApplicationStatusDto`**. |
| `DELETE` | `/api/v1/job-applications/{id}` | **Bearer** | Delete. |

---

## Users (scoped profile) — `api/v1/users/{userId}`

**`[Authorize]`**. **`userId`** is a **GUID** in the path. It **must match** the authenticated user’s JWT claim **`sub`** or **`user_id`**; otherwise the API returns **403 Forbidden** (or **401 Unauthorized** if the token has no usable user id).

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/users/{userId}/club-memberships` | `{ items: [ { clubId, role } ] }` from DB memberships. |
| `GET` | `/api/v1/users/{userId}/vacancy-applications` | Query: **`page`**, **`limit`**. That user’s vacancy applications. |
| `GET` | `/api/v1/users/{userId}/membership-applications` | Stub empty `items`. |
| `GET` | `/api/v1/users/{userId}/event-registrations` | That user’s event registrations. |
| `GET` | `/api/v1/users/{userId}/club-notifications` | Query: **`type`** (default `all`). Stub empty `items`. |
| `PATCH` | `/api/v1/users/{userId}/club-notifications/{notificationId}/read` | Stub read receipt. |

**Example (local):** `GET http://localhost:5003/api/v1/users/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/club-memberships` with a Bearer token whose `sub` is that same GUID.

---

## Applications (interview) — `api/v1/applications`

**`[Authorize]`**. Stub flows for interview scheduling.

| Method | Path | Body | Description |
|--------|------|------|-------------|
| `GET` | `/api/v1/applications/{applicationId}/interview-slots` | — | Empty `items`. |
| `POST` | `/api/v1/applications/{applicationId}/interview-slot` | `{ "slotId": string }` | Returns scheduled status. |

---

## Club proposals (student submit) — `api/v1/club-proposals`

**`[Authorize]`**. **`multipart/form-data`**.

| Method | Path | Form fields | Description |
|--------|------|-------------|-------------|
| `POST` | `/api/v1/club-proposals` | Optional `name`, `description`, `logoFile`, `constitutionFile` | Returns synthetic `proposalId` and `status`. |

---

## Student Services — `api/v1/student-services`

**`[Authorize]`**. Staff-style operations and stubs mixed with real club/event updates.

| Method | Path | Notes |
|--------|------|--------|
| `GET` | `/api/v1/student-services/dashboard` | Stub `cards`. |
| `GET` | `/api/v1/student-services/clubs` | Query `search`, `page`, `limit`. |
| `GET` | `/api/v1/student-services/clubs/{clubId}` | **`ClubDto`** or 404. |
| `GET` | `/api/v1/student-services/club-proposals` | Stub. |
| `GET` | `/api/v1/student-services/club-proposals/{proposalId}` | Stub. |
| `POST` | `/api/v1/student-services/club-proposals/{proposalId}/approve` | Stub. |
| `POST` | `/api/v1/student-services/club-proposals/{proposalId}/reject` | JSON `{ "reason" }`. |
| `POST` | `/api/v1/student-services/club-proposals/{proposalId}/request-revision` | JSON `{ "changes" }`. |
| `GET` | `/api/v1/student-services/club-proposal-requirements` | Stub. |
| `PUT` | `/api/v1/student-services/club-proposal-requirements` | JSON `{ requirements[], deadline? }`. |
| `GET` | `/api/v1/student-services/event-proposals` | Stub. |
| `GET` | `/api/v1/student-services/event-proposals/{proposalId}` | Stub. |
| `POST` | … `/approve` | JSON with optional `assignments` (sub-event / room model). |
| `POST` | … `/reject` | `{ "reason" }`. |
| `POST` | … `/request-revision` | `{ "changes" }`. |
| `GET` | `/api/v1/student-services/events` | All events. |
| `POST` | `/api/v1/student-services/events` | **`CreateEventRequest`** → persists via **`CreateEventDto`**. |
| `PATCH` | `/api/v1/student-services/events/{eventId}` | **`UpdateEventDto`**. |
| `GET` | `/api/v1/student-services/events/{eventId}` | Event or 404. |
| `DELETE` | `/api/v1/student-services/events/{eventId}` | Delete. |
| `PATCH` | `/api/v1/student-services/clubs/{clubId}` | **`UpdateClubRequest`**: `name`, `description`, `status` (club status enum name). |
| `POST` | `/api/v1/student-services/clubs/{clubId}/profile-image/approve` | Stub. |
| `GET` / `PATCH` / `DELETE` | `/api/v1/student-services/clubs/{clubId}/members` … | Stubs / minimal payloads. |
| `GET` / `POST` / `PATCH` / `DELETE` | `/api/v1/student-services/clubs/{clubId}/employees` … | Stubs / minimal payloads. |

---

## Club Admin — `api/v1/club-admin/{clubId}`

**`[Authorize]`**. **`clubId`** is the club route key (positive integer as string). All paths are prefixed with `/api/v1/club-admin/{clubId}/`.

### Dashboard & applications

| Method | Relative path | Query / body | Description |
|--------|---------------|--------------|-------------|
| `GET` | `dashboard` | — | Dashboard DTO. **404** if club key invalid. |
| `GET` | `applications` | `type`, `status`, `page`, `limit` | Job + membership application list. |
| `GET` | `applications/{applicationId}` | — | Detail. IDs encode type (e.g. job vs membership). |
| `POST` | `applications/{applicationId}/approve` | — | |
| `POST` | `applications/{applicationId}/reject` | `{ "reason" }` | |
| `POST` | `applications/{applicationId}/request-changes` | `{ "changes" }` | |
| `POST` | `applications/{applicationId}/notes` | `{ "note" }` | |
| `POST` | `applications/bulk-decision` | `{ "applicationIds": string[], "decision", "reason" }` | |

### Interview slots (in-memory workspace)

| Method | Relative path | Body | Description |
|--------|---------------|------|-------------|
| `GET` | `interview-slots` | `page`, `limit` | |
| `POST` | `interview-slots/generate` | `{ "date", "startTime", "endTime", "interviewLength", "vacancy" }` | |
| `PATCH` | `interview-slots/{slotId}` | `{ startsAt?, endsAt?, vacancyId?, capacity? }` | |
| `DELETE` | `interview-slots/{slotId}` | — | |

### Vacancies (admin)

| Method | Relative path | Query / body | Description |
|--------|---------------|--------------|-------------|
| `GET` | `vacancies` | `page`, `limit`, **`status`** optional: `draft`, `active` / `published`, `inactive` | |
| `POST` | `vacancies` | **`CreateVacancyRequest`**: optional `title`, required `description`, optional `positionId` (links **`ClubPosition`**), optional `applicationDeadline`, **`publish`** (default `true`) | `postedBy` from JWT. |
| `PATCH` | `vacancies/{vacancyId}` | **`UpdateVacancyRequest`**: optional `title`, `description`, `isActive`, `positionId`, `applicationDeadline`, `isDraft` | |
| `PATCH` | `vacancies/{vacancyId}/status` | `{ "status" }` — **`draft`**, **`published`** / **`active`**, or **`inactive`** | Maps to `isDraft` + `isActive`. |

### Positions (reusable role templates)

Numeric **`positionId`** in path (database id).

| Method | Relative path | Body | Description |
|--------|---------------|------|-------------|
| `GET` | `positions` | `page`, `limit` | List **`ClubAdminPositionDto`**: `id`, `clubId`, `name`, `title`, `categoryId`, `categoryName`, `requirements[]`. |
| `GET` | `positions/{positionId}` | — | Single position. |
| `POST` | `positions` | **`CreatePositionRequest`**: `title` (or legacy `name`), **`categoryId`**, **`requirements`** string list | |
| `PATCH` | `positions/{positionId}` | **`PatchPositionRequest`**: same shape | Replaces requirement list. |
| `DELETE` | `positions/{positionId}` | — | Soft-deletes position; clears `ClubPositionId` on linked vacancies. |

### Members & employees (workspace / validation)

| Method | Relative path | Description |
|--------|---------------|-------------|
| `GET` | `members` | Stub store. |
| `DELETE` | `members/{memberId}` | |
| `GET` | `employees` | |
| `POST` | `employees` | `{ "studentId", "positionId" }` — if `positionId` is numeric, validated against **`ClubPosition`** for the club. |
| `PATCH` | `employees/positions` | `{ "changes": [ { "employeeId", "positionId" } ] }` | |
| `DELETE` | `employees/{employeeId}` | |

### Events & attendees (admin)

| Method | Relative path | Description |
|--------|---------------|-------------|
| `GET` | `events` | `status`, `search`, `page`, `limit`. |
| `PATCH` | `events/{eventId}` | **`UpdateEventDto`**. |
| `POST` | `events/proposals` | Stub proposal id. |
| `GET` | `events/{eventId}/attendees` | `page`, `limit`. |
| `PATCH` | `events/{eventId}/attendees/{attendeeId}` | `{ "entered": bool }`. |
| `DELETE` | `events/{eventId}/attendees/{attendeeId}` | |

### Profile & announcements

| Method | Relative path | Description |
|--------|---------------|-------------|
| `PATCH` | `profile` | **`multipart/form-data`**: `socialLinks`, `focusAreasJson`, optional `logoFile`, `backgroundFile`. |
| `POST` | `announcements` | JSON `{ "title", "message" }`. |

---

## Quick reference: where “positions” live

There is **no** `GET /api/v1/positions` at the root. Reusable positions are only under **club admin**:

```http
GET http://localhost:5003/api/v1/club-admin/1/positions
```

Deployed:

```http
GET http://51.20.193.29:5000/club/api/v1/club-admin/1/positions
```

---

## CORS

`Program.cs` registers **`SpaCors`** (e.g. `http://localhost:3000` in Development when not overridden). Browser clients must send credentials/origins consistent with that policy.

---

## Further detail

For full property-level schemas, generate or inspect **OpenAPI** from Swagger (`/swagger/v1/swagger.json` locally). Domain DTOs live under `Services/ClubManagementService/ClubManagementService.Application/DTOs/`.

---

## Documentation update - user-scoped endpoints (replacing `/me`)

**Change:** Endpoints that previously lived under **`/api/v1/me/...`** now use **`/api/v1/users/{userId}/...`**, where **`{userId}`** is a **GUID** (standard string format).

**Security:** The **`{userId}`** in the URL **must equal** the JWT subject (`sub` or `user_id`). If a client calls another user's id, the response is **403 Forbidden**. If the token has no parseable user id, **401 Unauthorized**.

**Mapping (old -> new):**

| Old path | New path |
|----------|----------|
| `GET /api/v1/me/club-memberships` | `GET /api/v1/users/{userId}/club-memberships` |
| `GET /api/v1/me/vacancy-applications` | `GET /api/v1/users/{userId}/vacancy-applications` |
| `GET /api/v1/me/membership-applications` | `GET /api/v1/users/{userId}/membership-applications` |
| `GET /api/v1/me/event-registrations` | `GET /api/v1/users/{userId}/event-registrations` |
| `GET /api/v1/me/club-notifications` | `GET /api/v1/users/{userId}/club-notifications` |
| `PATCH /api/v1/me/club-notifications/{notificationId}/read` | `PATCH /api/v1/users/{userId}/club-notifications/{notificationId}/read` |

**Gateway (deployed):** prefix with `/club`, e.g.  
`http://51.20.193.29:5000/club/api/v1/users/{userId}/club-memberships`.

**Swagger:** Operations are grouped under the **Users** tag (`UsersController`).
