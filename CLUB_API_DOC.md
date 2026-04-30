# Club Management API Documentation

Last updated: 2026-04-30

This document is based on the current source code and gateway configuration in this repository.

Deployment note:

- This document uses the deployment gateway base URL shared in this thread: `https://myada.site/club`.
- Endpoint paths below are derived from the current controllers under `Services/ClubManagementService/ClubManagementService.API/Controllers`, then expressed against that base URL.
- Club position categories are defined by `ClubPositionCategory` in `Services/ClubManagementService/ClubManagementService.Domain/Enums/ClubPositionCategory.cs`.

The Club Management service uses URL versioning. The main path format is:

`/api/v1/...`

Use these bases in frontend and integration code:

- Public gateway base: `https://myada.site/club`
- Versioned API base: `https://myada.site/club/api/v1`

---

## Base URLs

| Environment | Base URL | Notes |
|-------------|----------|-------|
| Deployment base shared in this thread | `https://myada.site/club` | Public gateway base for the Club service. |
| Deployment versioned API base | `https://myada.site/club/api/v1` | Recommended base for frontend and integration calls. |
| Repo-local gateway | `http://localhost:5000` | From `docker-compose.yml` (`apigateway` maps `5000:8080`). |
| Local service | `http://localhost:5003` | Local docker/service port. |
| Swagger UI | `https://myada.site/club/swagger/index.html` | Exposed only when the club service runs in `Development`. |
| OpenAPI JSON | `https://myada.site/club/swagger/v1/swagger.json` | Exposed only when the club service runs in `Development`. |

Examples:

- Categories: `https://myada.site/club/api/v1/categories`
- Club detail: `https://myada.site/club/api/v1/clubs/1`
- Swagger: `https://myada.site/club/swagger/index.html`

Important gateway rule:

- Correct: `https://myada.site/club/api/v1/categories`
- Wrong: `https://myada.site/api/v1/categories`

Append `/api/v1/...` directly to the provided deployment base.

---

## Quick route index

Use `https://myada.site/club/api/v1` as the base for the groups below.

- `/categories` - category CRUD
- `/clubs` - club listing, detail, join applications
- `/events` - event listing, registration, tickets, check-in
- `/event-registrations` - event registration CRUD
- `/club-memberships` - club membership CRUD
- `/subevents` - subevent CRUD
- `/vacancies` - vacancy listing and vacancy applications
- `/job-applications` - submitted job application records
- `/club-proposals` - new club proposal submission
- `/applications`, `/interviews`, `/slots` - interview scheduling flows
- `/users/{userId}` and `/users/me` - user-specific club data
- `/student-services` - student services moderation and approvals
- `/club-admin/{clubId}` - club admin workspace routes
- `/club-position-requirements` - club position requirement CRUD

Representative full URLs:

- `https://myada.site/club/api/v1/clubs`
- `https://myada.site/club/api/v1/events`
- `https://myada.site/club/api/v1/club-proposals`
- `https://myada.site/club/api/v1/student-services/dashboard`

---

## Authentication

- Most club endpoints require `Authorization: Bearer <JWT>`.
- JWT identity is read from claim `sub` or `user_id`.
- JWT validation is configured from Auth JWKS in `Program.cs`.
- Some routes are intentionally anonymous. See the per-section tables below.

Role and caller rules that matter:

- `POST /api/v1/events/{eventId}/check-in` requires role `admin`, `Admin`, `staff`, or `Staff`.
- `GET /api/v1/users/{userId}/...` routes require the path `userId` to match the JWT subject unless the caller is a MyAda administrator.
- Most `student-services` routes enforce MyAda-administrator-only access in controller code.
- `GET` and `PUT` on `/api/v1/student-services/club-proposal-requirements` only require authentication; they do not run the extra admin-only guard used by the rest of `StudentServicesController`.
- `club-admin` routes require authentication; club-level authorization is handled by the club-admin service layer.

Anonymous routes currently exposed:

- `GET/POST/PUT/DELETE /api/v1/categories`
- `GET /api/v1/event-registrations`
- `GET /api/v1/event-registrations/by-event/{eventId}`
- `GET /api/v1/event-registrations/{id}`
- `GET/POST/PUT/DELETE /api/v1/subevents`
- `GET /api/v1/job-applications`
- `GET /api/v1/job-applications/by-vacancy/{jobVacancyId}`
- `GET /api/v1/job-applications/{id}`
- `POST /api/v1/club-proposals`

---

## Response shape

Responses are wrapped by AutoWrapper (`UseApiResponseAndExceptionWrapper`).

Typical success shape:

```json
{
  "statusCode": 200,
  "message": "GET Request successful.",
  "result": {}
}
```

Do not assume the controller payload is returned raw.

---

## Current differences from older docs

- This document now uses the deployment gateway base URL `https://myada.site/club`.
- `POST /api/v1/club-proposals` is anonymous and supports both `application/json` and `multipart/form-data`; use `multipart/form-data` for direct file attachments and S3 upload flow.
- Student-services proposal routes are:
  - `/api/v1/student-services/clubs/proposals/...`
  - `/api/v1/student-services/events/proposals/...`
- Event proposal create routes now support both `application/json` and `multipart/form-data`, including direct image upload with `imageFile`.
- `GET /api/v1/users/me/event-registrations` exists as a shortcut route.
- Legacy event aliases under `/api/events/...` are still live.
- Club-admin position payloads now use a string enum `category`, not `categoryId`.
- Club-admin file management routes now exist under `/api/v1/club-admin/{clubId}/files`.

---

## Categories - `/api/v1/categories`

Auth: none

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/categories` | List all categories. |
| `GET` | `/api/v1/categories/{id}` | Get category by id. |
| `POST` | `/api/v1/categories` | Body: `{ "name": string }`. |
| `PUT` | `/api/v1/categories/{id}` | Body: `{ "name": string }`. |
| `DELETE` | `/api/v1/categories/{id}` | Delete category. |

Representative item:

```json
{ "id": 1, "name": "Technology" }
```

---

## Clubs - `/api/v1/clubs`

Auth: Bearer required for all routes in this controller

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/clubs` | Query: `search`, `category`, `page`, `limit` (default `12`). Includes image fields. |
| `GET` | `/api/v1/clubs/{clubId}` | Public-style club detail projection with focus areas, social links, resources, officers, and image fields. |
| `GET` | `/api/v1/clubs/{clubId}/members` | Returns `{ items: [{ userId }] }`. |
| `POST` | `/api/v1/clubs/{clubId}/join-applications` | `multipart/form-data`. See form fields below. |
| `POST` | `/api/v1/clubs` | Body: `CreateClubDto`. |
| `PATCH` | `/api/v1/clubs/{clubId}` | Body: `UpdateClubDto`. |
| `DELETE` | `/api/v1/clubs/{clubId}` | Delete club. |

Image behavior on club `GET` routes:

- `profileImageUrl` is read from the club row in the database.
- `backgroundImageUrl` is also read from the club row in the database.
- If either stored value is an S3/bucket URL, the API returns that same absolute URL.
- If either stored value is a relative local upload path, the API returns that relative path.
- `backgroundImage` is the display-safe field: it returns `backgroundImageUrl` when present, otherwise `/clubs/default.png`.
- `image` currently mirrors `backgroundImage`, not `profileImageUrl`.
- Public club detail does not expose `proposedProfileImageUrl`; that pending image is only surfaced on student-services routes.

Representative list item:

```json
{
  "id": 1,
  "name": "Robotics Club",
  "category": "Technology",
  "profileImageUrl": "https://bucket.example/clubs/robotics/logo.png",
  "backgroundImageUrl": "https://bucket.example/clubs/robotics/cover.png",
  "backgroundImage": "https://bucket.example/clubs/robotics/cover.png",
  "image": "https://bucket.example/clubs/robotics/cover.png",
  "tags": [],
  "members": 0,
  "about": "Build and learn",
  "status": "Approved"
}
```

Representative detail fields:

- `profileImageUrl`
- `backgroundImageUrl`
- `backgroundImage`
- `image`
- `focusAreas`
- `socialLinks`
- `resources`
- `documents`
- `officers`

Join application form fields:

- `userId` optional; if omitted, backend falls back to JWT subject
- `letterOfPurpose` required
- `portfolioLinks` optional string
- `portfolioFiles` optional repeated files

Join application response shape:

```json
{
  "applicationId": "MEM-<guid>",
  "status": "Submitted",
  "submittedAt": "2026-04-25T10:00:00Z",
  "portfolioUrls": []
}
```

---

## Events - `/api/v1/events`

Auth: Bearer required

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/events` | Query: `clubId`, `myClubsOnly`, `search`, `page`, `limit`. Default page size is `12` in v1. |
| `GET` | `/api/v1/events/{eventId}` | Event detail. |
| `POST` | `/api/v1/events/{eventId}/registrations` | Register current user for event. |
| `DELETE` | `/api/v1/events/{eventId}/registrations` | Cancel current user's registration. |
| `GET` | `/api/v1/events/{eventId}/ticket` | Get current user's ticket payload. |
| `POST` | `/api/v1/events/{eventId}/check-in` | Staff/admin only. Body: `{ "jwt": "..." }`. |
| `POST` | `/api/v1/events` | `multipart/form-data` only. Fields: `clubId`, `name`, `description`, `startTime`, `endTime`, `location`, `buildingId`, `roomId`, `seatLimit`, `requirements`, `prerequisites`, `notes`, optional `imageFile`. `approvedBy` is filled from JWT. |
| `PATCH` | `/api/v1/events/{eventId}` | `multipart/form-data` only. Fields: `name`, `description`, `startTime`, `endTime`, `location`, `buildingId`, `roomId`, `seatLimit`, `requirements`, `prerequisites`, `notes`, `status`, optional `imageFile`. |
| `DELETE` | `/api/v1/events/{eventId}` | Delete event. |

Compatibility aliases also exposed by the same controller:

- `GET /api/events`
- `GET /api/events/{eventId}`
- `POST /api/events/{eventId}/registrations`
- `DELETE /api/events/{eventId}/registrations`
- `GET /api/events/{eventId}/ticket`
- `POST /api/events/{eventId}/check-in`

Registration response shape:

```json
{
  "ticketId": "<guid>",
  "eventId": "12",
  "userId": "<guid>",
  "jwt": "<ticket-jwt>",
  "attendeeId": "<guid>",
  "event": {
    "id": "12",
    "name": "Sample Event",
    "date": "2026-04-25",
    "startTime": "14:00",
    "endTime": "16:00",
    "location": "Main Hall",
    "clubName": null,
    "imageUrl": null,
    "seatLimit": 100,
    "registeredCount": 7
  }
}
```

Create/update notes:

- Attach the event image with `imageFile`.
- `buildingId` and `roomId` are optional, but they must be sent together.
- If provided, both `buildingId` and `roomId` must be positive integers.
- `imageUrl` and `imgUrl` are rejected on `POST /api/v1/events` and `PATCH /api/v1/events/{eventId}`.
- Event responses still expose the persisted image as `imageUrl`, and now also include `buildingId` and `roomId` when present.

---

## Event registrations - `/api/v1/event-registrations`

Auth:

- `GET` routes: no auth attribute
- `POST`, `PUT`, `DELETE`: Bearer required

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/event-registrations` | List all registrations. |
| `GET` | `/api/v1/event-registrations/by-event/{eventId}` | List by event. |
| `GET` | `/api/v1/event-registrations/{id}` | Get by id. |
| `POST` | `/api/v1/event-registrations` | Body: `{ "eventId": int }`. Uses current JWT user. |
| `PUT` | `/api/v1/event-registrations/{id}` | Body: `{ "eventId": int }`. Owner or admin only. |
| `DELETE` | `/api/v1/event-registrations/{id}` | Owner or admin only. |

---

## Club memberships - `/api/v1/club-memberships`

Auth: Bearer required

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/club-memberships` | Query: `clubId`, `userId`. Non-admin callers only see their own memberships. |
| `GET` | `/api/v1/club-memberships/{id}` | Owner or MyAda admin. |
| `POST` | `/api/v1/club-memberships` | Body: `{ "clubId": int }`. Uses current JWT user. |
| `PUT` | `/api/v1/club-memberships/{id}` | Body: `{ "clubId": int }`. Owner or MyAda admin. |
| `DELETE` | `/api/v1/club-memberships/{id}` | Owner or MyAda admin. |

Returned item fields:

```json
{
  "id": 1,
  "clubId": 2,
  "userId": "<guid>",
  "createdAt": "2026-04-25T10:00:00Z",
  "modifiedAt": null
}
```

---

## Subevents - `/api/v1/subevents`

Auth: none

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/subevents` | List all subevents. |
| `GET` | `/api/v1/subevents/by-event/{mainEventId}` | List subevents for an event. |
| `GET` | `/api/v1/subevents/{id}` | Get subevent by id. |
| `POST` | `/api/v1/subevents` | Body: `CreateSubeventDto`. |
| `PUT` | `/api/v1/subevents/{id}` | Body: `UpdateSubeventDto`. |
| `DELETE` | `/api/v1/subevents/{id}` | Delete subevent. |

---

## Vacancies - `/api/v1/vacancies`

Auth: Bearer required

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/vacancies` | Query: `search`, `category`, `savedOnly`, `page`, `limit`. `category` and `savedOnly` are currently ignored by the controller. |
| `GET` | `/api/v1/vacancies/{vacancyId}` | `vacancyId` must be a positive integer string. Draft or inactive vacancies return `404`. |
| `POST` | `/api/v1/vacancies/{vacancyId}/applications` | `multipart/form-data`. See form fields below. |
| `GET` | `/api/v1/vacancies/by-club/{clubId}` | Active vacancies for a club. |
| `POST` | `/api/v1/vacancies` | Body: `CreateJobVacancyDto`; `postedBy` is set from JWT. |
| `PATCH` | `/api/v1/vacancies/{vacancyId}` | Body: `UpdateJobVacancyDto`. |
| `DELETE` | `/api/v1/vacancies/{vacancyId}` | Delete vacancy. |

Vacancy application form fields:

- `userId` optional; if omitted, backend falls back to JWT subject
- `purposeOfApplication` required
- `cvFile` required

Vacancy application response shape:

```json
{
  "applicationId": "job:15",
  "status": "Submitted",
  "cvUrl": "https://..."
}
```

---

## Job applications - `/api/v1/job-applications`

Auth:

- `GET` routes: anonymous
- `POST`, `PATCH`, `DELETE`: Bearer required

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/job-applications` | List all job applications. |
| `GET` | `/api/v1/job-applications/by-vacancy/{jobVacancyId}` | List applications for a vacancy. |
| `GET` | `/api/v1/job-applications/{id}` | Get one application. |
| `POST` | `/api/v1/job-applications` | `multipart/form-data`. See form fields below. |
| `PATCH` | `/api/v1/job-applications/{id}/status` | Body: `UpdateJobApplicationStatusDto`. |
| `DELETE` | `/api/v1/job-applications/{id}` | Delete application. |

Create job application form fields:

- `jobVacancyId` required integer
- `experienceLinks` string
- `cvFile` required file

---

## Club proposals - `/api/v1/club-proposals`

Auth: anonymous

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/api/v1/club-proposals` | Accepts `multipart/form-data` for direct file upload to S3, or `application/json` when file fields are already URLs/references. |

Swagger / OpenAPI note:

- In Swagger UI, choose `multipart/form-data` in the request body content-type dropdown if you want file attach controls for `logoFile` and `constitutionFile`.
- If Swagger UI is set to `application/json`, those fields are plain strings because JSON mode expects already-uploaded URLs, not binary files.

Multipart form fields:

| Field | Required | Type | Notes |
|------|----------|------|-------|
| `organizationId` | Yes | string | Submitter organization id, exactly 9 digits |
| `name` | Yes | string | Club name |
| `description` | Yes in legacy mode | string | Full proposal text |
| `logoFile` | Yes | file | Uploaded to S3 by backend |
| `constitutionFile` | Yes | file | Uploaded to S3 by backend |
| `presidentStudentId` | No | string | Structured mode field, exactly 9 digits |
| `vicePresidentStudentId` | No | string | Structured mode field, exactly 9 digits |
| `otherMembersJson` | No | repeated string field | Send one 9-digit user organization id per field |
| `commitment` | No | string | Usually `"yes"` in structured mode |
| `shortDesc` | No | string | Structured mode field |
| `uniqueDesc` | No | string | Structured mode field |
| `goals` | No | string | Structured mode field |
| `activities` | No | string | Structured mode field |
| `alignment` | No | string | Structured mode field |
| `vision` | No | string | Structured mode field |

Multipart example:

```bash
curl -X POST "{CLUB_API_BASE}/api/v1/club-proposals" \
  -F "organizationId=123456789" \
  -F "name=Robotics Club" \
  -F "description=Club proposal description" \
  -F "logoFile=@logo.png" \
  -F "constitutionFile=@constitution.pdf" \
  -F "presidentStudentId=123456780" \
  -F "vicePresidentStudentId=876543210" \
  -F "otherMembersJson=123456781" \
  -F "otherMembersJson=123456782" \
  -F "commitment=yes"
```

- When using `curl -F`, `FormData`, Axios, or `fetch`, do not set the multipart `Content-Type` header manually. Let the client generate the boundary.

JSON request body fields:

- `organizationId`
- `name`
- `description`
- `logoFile`
- `constitutionFile`
- `presidentStudentId`
- `vicePresidentStudentId`
- `otherMembersJson`
- `commitment`
- `shortDesc`
- `uniqueDesc`
- `goals`
- `activities`
- `alignment`
- `vision`

JSON example:

```json
{
  "organizationId": "123456789",
  "name": "Robotics Club",
  "description": "Club proposal description",
  "logoFile": "https://bucket.example/club-proposals/logo.png",
  "constitutionFile": "https://bucket.example/club-proposals/constitution.pdf",
  "presidentStudentId": "123456780",
  "vicePresidentStudentId": "876543210",
  "otherMembersJson": [
    "123456781",
    "123456782"
  ],
  "commitment": "yes",
  "shortDesc": "short text",
  "uniqueDesc": "unique text",
  "goals": "goals text",
  "activities": "activities text",
  "alignment": "alignment text",
  "vision": "vision text"
}
```

Important:

- In multipart mode, `logoFile` and `constitutionFile` are uploaded files and the backend stores them in S3.
- In JSON mode, `logoFile` and `constitutionFile` are string fields and should contain an already-uploaded URL/reference.
- `organizationId`, `presidentStudentId`, `vicePresidentStudentId`, and each `otherMembersJson` item must be exactly 9 digits.
- `otherMembersJson` is now a list of user organization ids, not a stringified JSON blob.
- When a proposal is approved, matching `otherMembersJson` users are resolved from Auth and added to club memberships.

---

## Users - `/api/v1/users/{userId}`

Auth: Bearer required

Unless the caller is a MyAda administrator, `{userId}` must exactly match the JWT `sub` or `user_id`.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/users/{userId}/club-memberships` | Returns `{ items: [{ clubId, role }] }`. |
| `GET` | `/api/v1/users/{userId}/vacancy-applications` | Query: `page`, `limit`. |
| `GET` | `/api/v1/users/{userId}/membership-applications` | Stub empty `items`. |
| `GET` | `/api/v1/users/{userId}/event-registrations` | Current user's event registrations. |
| `GET` | `/api/v1/users/{userId}/club-notifications` | Query: `type`, default `all`. |
| `PATCH` | `/api/v1/users/{userId}/club-notifications/{notificationId}/read` | Returns `{ notificationId, read: true }`. |

---

## Users me shortcut - `/api/v1/users/me`

Auth: Bearer required

These routes exist in addition to the `users/{userId}` routes:

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/users/me/event-registrations` | Returns the current user's ticket list. |
| `GET` | `/api/users/me/event-registrations` | Unversioned alias of the same action. |

The `users/me` shortcut currently exists only for event registrations.

---

## Applications, interviews, and slots

These three groups work together for interview scheduling.

### Applications - `/api/v1/applications`

Auth: Bearer required

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/applications/{applicationId}/interview-slots` | Query: `from`, `to`, `date`, `includeFull`. Returns `{ items }`. |
| `POST` | `/api/v1/applications/{applicationId}/interview-slot` | Body: `{ "slotId": string }`. Books a slot. |

### Interviews - `/api/v1/interviews`

Auth: Bearer required

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/interviews/{interviewId}/slots` | Query: `from`, `to`, `date`, `includeFull`. |
| `POST` | `/api/v1/interviews/{interviewId}/bookings` | Body: `{ "slotId": string }`. Returns `201` on new booking, `200` on idempotent replay. |
| `GET` | `/api/v1/interviews/{interviewId}/my-booking` | Get current user's booking. |

### Slots - `/api/v1/slots`

Auth: Bearer required

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/slots/{slotId}` | Returns capacity, remaining capacity, status, and `bookable`. |

Notes:

- In these routes, `applicationId` / `interviewId` map to a job-application id.
- `slotId` is a string identifier, not an integer database key.

---

## Student services - `/api/v1/student-services`

Auth: Bearer required

Most routes in this controller also require the caller to be a MyAda administrator. The only exception in controller code is the club-proposal-requirements endpoints, which are auth-only.

### Dashboard and club/event lists

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/student-services/dashboard` | Admin-only. |
| `GET` | `/api/v1/student-services/clubs` | Admin-only. Query: `search`, `page`, `limit`. Includes `profileImageUrl`, `backgroundImageUrl`, `backgroundImage`, `image`, and `proposedProfileImageUrl`. |
| `GET` | `/api/v1/student-services/clubs/{clubId}` | Admin-only. Returns `ClubDto`, including image fields from the club row. |
| `GET` | `/api/v1/student-services/events` | Admin-only. |
| `POST` | `/api/v1/student-services/events` | Admin-only. Body: `CreateEventRequest`. |
| `PATCH` | `/api/v1/student-services/events/{eventId}` | Admin-only. Body: `UpdateEventDto`. |
| `GET` | `/api/v1/student-services/events/{eventId}` | Admin-only. |
| `DELETE` | `/api/v1/student-services/events/{eventId}` | Admin-only. |
| `PATCH` | `/api/v1/student-services/clubs/{clubId}` | Admin-only. Body: `{ name?, description?, status? }`. |

### Club proposals

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/student-services/clubs/proposals` | Admin-only. Query: `status`, `search`, `page`, `limit`. |
| `GET` | `/api/v1/student-services/clubs/proposals/{proposalId}` | Admin-only. |
| `POST` | `/api/v1/student-services/clubs/proposals/{proposalId}/approve` | Admin-only. |
| `POST` | `/api/v1/student-services/clubs/proposals/{proposalId}/reject` | Admin-only. Body: `{ "reason": string }`. |
| `POST` | `/api/v1/student-services/clubs/proposals/{proposalId}/request-revision` | Admin-only. Body: `{ "changes": string }`. |

### Event proposals

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/api/v1/student-services/events/proposals` | Admin-only. Accepts `application/json` or `multipart/form-data`. Use `imageFile` for direct uploads, or `imageUrl` when the image is already hosted. |
| `GET` | `/api/v1/student-services/events/proposals` | Admin-only. Query: `search`, `status`, `page`, `limit`. |
| `GET` | `/api/v1/student-services/events/proposals/{proposalId}` | Admin-only. |
| `POST` | `/api/v1/student-services/events/proposals/{proposalId}/approve` | Admin-only. Body: optional `EventProposalApproveDto`. |
| `POST` | `/api/v1/student-services/events/proposals/{proposalId}/reject` | Admin-only. Body: `{ "reason": string }`. |
| `POST` | `/api/v1/student-services/events/proposals/{proposalId}/request-revision` | Admin-only. Body: `{ "changes": string }`. |

Event proposal create contract:

- `application/json` body uses `EventProposalSubmitDto`.
- `multipart/form-data` supports direct image upload from Swagger/Postman:
  - scalar fields: `name`, `dateTime`, `duration`, `attendance`, `venue`, `buildingId`, `roomId`, `description`, `objectives`, `submittedByOrganization`
  - optional image fields: `imageUrl`, `imageFile`
  - nested JSON string fields: `subEventsJson`, `logisticsJson`
- If both `imageUrl` and `imageFile` are sent, the uploaded `imageFile` wins.
- `buildingId` and `roomId` are optional, but they must be provided together.
- If provided, both `buildingId` and `roomId` must be positive integers.
- Proposal list/detail responses now expose the stored proposal image as `imageUrl` and legacy alias `posterUrl`.
- Proposal list/detail responses also expose `requestedBuildingId`, `requestedRoomId`, `buildingId`, and `roomId`.
- Approval copies that proposal image into the created event's `ImageUrl`.

### Club proposal requirements

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/student-services/club-proposal-requirements` | Auth required. |
| `PUT` | `/api/v1/student-services/club-proposal-requirements` | Auth required. Body: `{ "requirements": string[], "deadline": datetime|null }`. |

### Club profile image, members, and employees

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/api/v1/student-services/clubs/{clubId}/profile-image` | Admin-only. `multipart/form-data` with `logoFile`. |
| `POST` | `/api/v1/student-services/clubs/{clubId}/profile-image/approve` | Admin-only. |
| `GET` | `/api/v1/student-services/clubs/{clubId}/members` | Admin-only. Stub list. |
| `PATCH` | `/api/v1/student-services/clubs/{clubId}/members/{memberId}` | Admin-only. |
| `DELETE` | `/api/v1/student-services/clubs/{clubId}/members/{memberId}` | Admin-only. |
| `GET` | `/api/v1/student-services/clubs/{clubId}/employees` | Admin-only. Stub list. |
| `POST` | `/api/v1/student-services/clubs/{clubId}/employees` | Admin-only. Body: `{ "userId": guid, "positionId": string|null }`. |
| `PATCH` | `/api/v1/student-services/clubs/{clubId}/employees/{employeeId}` | Admin-only. Body: `{ "positionId": string|null }`. |
| `DELETE` | `/api/v1/student-services/clubs/{clubId}/employees/{employeeId}` | Admin-only. |

Image behavior on student-services club routes:

- `GET /api/v1/student-services/clubs` returns:
  - `profileImageUrl`: approved/public image currently stored on the club row
  - `backgroundImageUrl`: approved/public background image currently stored on the club row
  - `backgroundImage`: same value as `backgroundImageUrl` when present, otherwise `/clubs/default.png`
  - `image`: currently the same value as `backgroundImage`
  - `proposedProfileImageUrl`: pending student-services upload when one exists
- `GET /api/v1/student-services/clubs/{clubId}` returns the mapped `ClubDto`, so clients should expect `ProfileImageUrl`, `BackgroundImageUrl`, and `ProposedProfileImageUrl`.
- `POST /api/v1/student-services/clubs/{clubId}/profile-image` stores a pending image path in `ProposedProfileImageUrl`.
- `POST /api/v1/student-services/clubs/{clubId}/profile-image/approve` moves that pending value into `ProfileImageUrl`.

---

## Club admin - `/api/v1/club-admin/{clubId}`

Auth: Bearer required

`clubId` is passed as a string route segment but is treated as the club route key.

### Dashboard and applications

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/club-admin/{clubId}/dashboard` | Dashboard payload. |
| `GET` | `/api/v1/club-admin/{clubId}/applications` | Query: `type`, `status`, `page`, `limit`. |
| `GET` | `/api/v1/club-admin/{clubId}/applications/{applicationId}` | Application detail. |
| `POST` | `/api/v1/club-admin/{clubId}/applications/{applicationId}/approve` | Approve application. |
| `POST` | `/api/v1/club-admin/{clubId}/applications/{applicationId}/reject` | Body: `{ "reason": string }`. |
| `POST` | `/api/v1/club-admin/{clubId}/applications/{applicationId}/request-changes` | Body: `{ "changes": string }`. |
| `POST` | `/api/v1/club-admin/{clubId}/applications/{applicationId}/invite-to-interview` | Invite applicant to interview stage. |
| `POST` | `/api/v1/club-admin/{clubId}/applications/{applicationId}/notes` | Body: `{ "note": string }`. |
| `POST` | `/api/v1/club-admin/{clubId}/applications/bulk-decision` | Body: `{ "applicationIds": string[], "decision": string, "reason": string }`. |

### Interview slots

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/club-admin/{clubId}/interview-slots` | Query: `page`, `limit`. |
| `POST` | `/api/v1/club-admin/{clubId}/interview-slots/generate` | Body: `{ "date", "endDate"?, "startTime", "endTime", "interviewLength", "vacancy" }`. |
| `PATCH` | `/api/v1/club-admin/{clubId}/interview-slots/{slotId}` | Body: `{ "startsAt"?, "endsAt"?, "vacancyId"?, "capacity"?, "isPublished"? }`. |
| `DELETE` | `/api/v1/club-admin/{clubId}/interview-slots/{slotId}` | Delete slot. |

### Vacancies

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/club-admin/{clubId}/vacancies` | Query: `page`, `limit`, `status`. |
| `POST` | `/api/v1/club-admin/{clubId}/vacancies` | Body: `{ "title"?, "description", "positionId"?, "applicationDeadline"?, "publish": bool }`. |
| `PATCH` | `/api/v1/club-admin/{clubId}/vacancies/{vacancyId}` | Body: `{ "title"?, "description"?, "isActive"?, "positionId"?, "applicationDeadline"?, "isDraft"? }`. |
| `PATCH` | `/api/v1/club-admin/{clubId}/vacancies/{vacancyId}/status` | Body: `{ "status": "draft"|"published"|"active"|"inactive" }`. |

### Positions

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/club-admin/{clubId}/positions` | Query: `page`, `limit`. |
| `GET` | `/api/v1/club-admin/{clubId}/positions/{positionId}` | Position detail. |
| `POST` | `/api/v1/club-admin/{clubId}/positions` | Body: `{ "title", "name"?, "category", "requirements": string[] }`. `name` is a legacy alias for `title`. |
| `PATCH` | `/api/v1/club-admin/{clubId}/positions/{positionId}` | Same body shape as create. |
| `DELETE` | `/api/v1/club-admin/{clubId}/positions/{positionId}` | Delete position. |

Position category enum values:

- `Marketing`
- `Technology`
- `Events`
- `Finance`
- `Outreach`
- `Content`
- `Other`

Important contract notes:

- `categoryId` is no longer part of the request DTOs or service layer.
- `category` is serialized as a JSON string enum (`ClubPositionCategory`).
- Invalid enum values are rejected with `400` and message `Invalid position category.`
- Position responses include both `category` and `categoryName` (currently the same enum label).
- Position responses also include both `title` and `name`; `name` is kept for older clients and mirrors `title`.

Create / patch example:

```json
{
  "title": "Head of Outreach",
  "category": "Outreach",
  "requirements": [
    "Strong communication skills",
    "Can coordinate cross-club partnerships"
  ]
}
```

Representative position response:

```json
{
  "id": "12",
  "clubId": "5",
  "name": "Head of Outreach",
  "title": "Head of Outreach",
  "category": "Outreach",
  "categoryName": "Outreach",
  "requirements": [
    "Strong communication skills",
    "Can coordinate cross-club partnerships"
  ]
}
```

### Members and employees

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/club-admin/{clubId}/members` | Query: `page`, `limit`. |
| `DELETE` | `/api/v1/club-admin/{clubId}/members/{memberId}` | Delete member. |
| `GET` | `/api/v1/club-admin/{clubId}/employees` | Query: `page`, `limit`. |
| `POST` | `/api/v1/club-admin/{clubId}/employees` | Body: `{ "studentId": string, "positionId": string }`. |
| `PATCH` | `/api/v1/club-admin/{clubId}/employees/positions` | Body: `{ "changes": [{ "employeeId": string, "positionId": string }] }`. |
| `DELETE` | `/api/v1/club-admin/{clubId}/employees/{employeeId}` | Delete employee. |

### Events and attendees

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/club-admin/{clubId}/events` | Query: `status`, `search`, `page`, `limit`. |
| `PATCH` | `/api/v1/club-admin/{clubId}/events/{eventId}` | Body: `UpdateEventDto`. |
| `POST` | `/api/v1/club-admin/{clubId}/events/proposals` | Accepts `application/json` or `multipart/form-data`. Use `imageFile` for direct uploads, or `imageUrl` when the image is already hosted. Request body also supports optional `buildingId` and `roomId`. |
| `GET` | `/api/v1/club-admin/{clubId}/events/{eventId}/attendees` | Query: `page`, `limit`. |
| `PATCH` | `/api/v1/club-admin/{clubId}/events/{eventId}/attendees/{attendeeId}` | Body: `{ "entered": bool }`. |
| `DELETE` | `/api/v1/club-admin/{clubId}/events/{eventId}/attendees/{attendeeId}` | Delete attendee. |

`POST /api/v1/club-admin/{clubId}/events/proposals` multipart example:

```bash
curl -X POST "https://myada.site/club/api/v1/club-admin/1/events/proposals" \
  -H "Authorization: Bearer <JWT>" \
  -F "name=Spring Robotics Showcase" \
  -F "dateTime=2026-05-10T14:00:00Z" \
  -F "duration=3 hours" \
  -F "attendance=120" \
  -F "venue=Main Auditorium" \
  -F "description=Club-wide demo event" \
  -F "objectives=Showcase projects and recruit members" \
  -F "buildingId=2" \
  -F "roomId=14" \
  -F "subEventsJson=[{\"title\":\"Demo Booths\",\"capacity\":\"80\",\"start\":\"14:00\",\"end\":\"16:00\",\"date\":\"2026-05-10\"}]" \
  -F "logisticsJson={\"avSetup\":true,\"security\":false,\"catering\":false,\"cleaning\":true,\"otherNeeds\":\"Two projectors\"}" \
  -F "imageFile=@C:/path/to/poster.png;type=image/png"
```

Notes:

- Attach the poster on `imageFile`.
- Use `imageUrl` only when you already have a hosted image URL and do not want to upload a file.
- The backend still accepts older aliases such as `posterFile`, but `imageFile` is the supported field to use moving forward.

### Profile, files, and announcements

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/club-admin/{clubId}/files` | List club resource files. Response body contains `items`. |
| `GET` | `/api/v1/club-admin/{clubId}/files/{fileId}` | Get one club file by item id. |
| `POST` | `/api/v1/club-admin/{clubId}/files` | `multipart/form-data`. Fields: `file` required, `title` optional. Uploads the file, stores the file URL in `ResourcesJson`, and returns the created item. |
| `PATCH` | `/api/v1/club-admin/{clubId}/files/{fileId}` | `multipart/form-data`. Fields: `title` optional, `file` optional. At least one must be provided. Replacing the file uploads a new object and removes the previous stored object when cleanup succeeds. |
| `DELETE` | `/api/v1/club-admin/{clubId}/files/{fileId}` | Removes the file item from `ResourcesJson` and attempts to delete the stored object. |
| `PATCH` | `/api/v1/club-admin/{clubId}/profile` | `multipart/form-data`. Fields: `socialLinks`, `focusAreasJson`, `email`, `resourcesJson`, `logoFile`, `backgroundFile`. Uploaded `logoFile` is persisted into the club's `ProfileImageUrl`, so later club `GET` endpoints return it. |
| `POST` | `/api/v1/club-admin/{clubId}/announcements` | Body: `{ "title": string, "message": string }`. |

Current file-management notes:

- Allowed upload extensions come from the storage service and currently include `.png`, `.jpg`, `.jpeg`, `.webp`, `.pdf`, `.doc`, and `.docx`.
- File items are stored in the club's `ResourcesJson` and now always carry a stable item `id`.
- Older `resourcesJson` payloads without item ids are normalized on read and on write so item-level CRUD can work reliably.
- Public club `GET` responses continue exposing these items through `resources` and the legacy alias `documents`.

Profile patch field semantics:

- `socialLinks` is a JSON object string and behaves as a merge patch. Supported keys are `website`, `instagram`, `x`, `twitter`, and `tiktok`. Keys you omit stay unchanged; send `null` or an empty string to clear one field.
- `focusAreasJson` is a replace-only JSON array string. Each item must be an object with `icon`, `title`, and `description`. The API allows at most 6 items.
- `email` is optional; send an empty string to clear the stored contact email.
- `resourcesJson` is a replace-only JSON array string. The API allows at most 50 items and normalizes each URL.
- `logoFile` and `backgroundFile` are optional file parts. Accepted extensions currently include `.png`, `.jpg`, `.jpeg`, `.webp`, `.pdf`, `.doc`, and `.docx`, although profile uploads are normally image files.

Representative curl example:

```bash
curl -X PATCH "https://myada.site/club/api/v1/club-admin/7/profile" \
  -H "Authorization: Bearer <JWT>" \
  -H "Accept: application/json" \
  -F 'email=robotics@ada.edu.az' \
  -F 'socialLinks={"website":"https://robotics.ada.edu.az","instagram":"https://instagram.com/ada_robotics","x":"https://x.com/ada_robotics"}' \
  -F 'focusAreasJson=[{"icon":"cpu","title":"AI Projects","description":"Building practical AI and robotics projects."},{"icon":"users","title":"Community","description":"Weekly meetups, workshops, and team challenges."}]' \
  -F 'resourcesJson=[{"title":"Club Handbook","url":"https://example.com/club-handbook.pdf"},{"title":"Registration Form","url":"https://example.com/register"}]' \
  -F 'logoFile=@logo.png' \
  -F 'backgroundFile=@background.jpg'
```

Current profile patch response highlights:

- `clubId`
- `updated`
- `pendingReview`
- `profileImageUrl`
- `backgroundImageUrl`
- `focusAreas`
- `email`
- `socialLinks`
- `resources`

Important image note:

- Uploading `logoFile` updates the persisted `profileImageUrl` when the patch succeeds.
- Uploading `backgroundFile` updates the persisted `backgroundImageUrl` when the patch succeeds.
- Public club and student-services `GET` endpoints now expose `backgroundImageUrl`, and their display-safe `image` field is derived from the background image rather than the logo/profile image.

---

## Club position requirements - `/api/v1/club-position-requirements`

Auth: Bearer required

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/api/v1/club-position-requirements` | Query: `clubPositionId`. |
| `GET` | `/api/v1/club-position-requirements/{id}` | Get requirement by id. |
| `POST` | `/api/v1/club-position-requirements` | Body: `{ "clubPositionId": int, "sortOrder": int, "text": string }`. |
| `PUT` | `/api/v1/club-position-requirements/{id}` | Same body shape as create. |
| `DELETE` | `/api/v1/club-position-requirements/{id}` | Delete requirement. |

---

## Legacy and compatibility routes

The current controller surface also exposes these compatibility routes in addition to the main `/api/v1/...` routes:

- `/api/events`
- `/api/events/{eventId}`
- `/api/events/{eventId}/registrations`
- `/api/events/{eventId}/ticket`
- `/api/events/{eventId}/check-in`
- `/api/users/me/event-registrations`

If you are building new frontend code, prefer the versioned `/api/v1/...` routes unless you specifically need backward compatibility.

---

## Recommended frontend base

Use:

`https://myada.site/club`

Then append one of:

- `/api/v1/categories`
- `/api/v1/clubs`
- `/api/v1/events`
- `/api/v1/vacancies`
- `/api/v1/users/{userId}/event-registrations`
- `/api/v1/club-admin/{clubId}/...`

---

## Best source of truth

For exact live schemas and tags, use:

- Swagger UI: `https://myada.site/club/swagger/index.html`
- OpenAPI JSON: `https://myada.site/club/swagger/v1/swagger.json`

Those Swagger routes are available only when the club service is running in `Development`. If they are unavailable in a deployment, use this markdown file plus the controller source as the source-backed reference.

If your deployment exposes the gateway on a different host or port, keep the same `/api/v1/...` paths and swap in the deployed base address.

This markdown file is the human-readable summary. The OpenAPI document is the canonical deployed contract when the target gateway is reachable.
