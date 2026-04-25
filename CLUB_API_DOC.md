# Club Management API Documentation

Last updated: 2026-04-25

This document was verified against:

- Live gateway base: `http://13.60.31.141:5000/club`
- Live Swagger UI: `http://13.60.31.141:5000/club/swagger/index.html`
- Live OpenAPI JSON: `http://13.60.31.141:5000/club/swagger/v1/swagger.json`
- Live sample endpoint: `GET http://13.60.31.141:5000/club/api/v1/categories`
- Source controllers under `Services/ClubManagementService/ClubManagementService.API/Controllers`

The Club Management service uses URL versioning. The main path format is:

`/api/v1/...`

Use this gateway base in frontend and integration code:

`{base} = http://13.60.31.141:5000/club`

---

## Base URLs

| Environment | Base URL | Notes |
|-------------|----------|-------|
| Gateway (verified) | `http://13.60.31.141:5000/club` | Recommended production entrypoint. |
| Local service | `http://localhost:5003` | Local docker/service port. |
| Swagger UI | `http://13.60.31.141:5000/club/swagger/index.html` | Available on the live gateway. |
| OpenAPI JSON | `http://13.60.31.141:5000/club/swagger/v1/swagger.json` | Best machine-readable source of truth. |

Examples:

- Categories: `http://13.60.31.141:5000/club/api/v1/categories`
- Club detail: `http://13.60.31.141:5000/club/api/v1/clubs/1`
- Swagger: `http://13.60.31.141:5000/club/swagger/index.html`

Important gateway rule:

- Correct: `http://13.60.31.141:5000/club/api/v1/categories`
- Wrong: `http://13.60.31.141:5000/api/v1/categories`

The gateway strips `/club` before forwarding to the club service.

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

- The verified production gateway host is now `13.60.31.141:5000`, not `51.20.193.29:5000`.
- `POST /api/v1/club-proposals` is now JSON-based and anonymous, not authenticated multipart upload.
- Student-services proposal routes are:
  - `/api/v1/student-services/clubs/proposals/...`
  - `/api/v1/student-services/events/proposals/...`
- `GET /api/v1/users/me/event-registrations` exists as a shortcut route.
- Legacy event aliases under `/api/events/...` are still live.

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
- If that stored value is an S3/bucket URL, the API returns that same absolute URL.
- If that stored value is a relative local upload path, the API returns that relative path.
- `image` is the display-safe field: it returns `profileImageUrl` when present, otherwise `/clubs/default.png`.
- Public club detail does not expose `proposedProfileImageUrl`; that pending image is only surfaced on student-services routes.

Representative list item:

```json
{
  "id": 1,
  "name": "Robotics Club",
  "category": "Technology",
  "profileImageUrl": "https://bucket.example/clubs/robotics/logo.png",
  "image": "https://bucket.example/clubs/robotics/logo.png",
  "tags": [],
  "members": 0,
  "about": "Build and learn",
  "status": "Approved"
}
```

Representative detail fields:

- `profileImageUrl`
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
| `POST` | `/api/v1/events` | Body: `CreateEventDto`; `approvedBy` is filled from JWT. |
| `PATCH` | `/api/v1/events/{eventId}` | Body: `UpdateEventDto`. |
| `DELETE` | `/api/v1/events/{eventId}` | Delete event. |

Live legacy aliases also exposed by the same controller:

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
| `POST` | `/api/v1/club-proposals` | `application/json`. Body uses `ClubProposalSubmissionDto`. |

Current request body fields:

- `id`
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

Important:

- This route is no longer multipart in the current controller.
- `logoFile` and `constitutionFile` are JSON string fields in the DTO, not uploaded files.

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
| `GET` | `/api/v1/student-services/clubs` | Admin-only. Query: `search`, `page`, `limit`. Includes `profileImageUrl`, `image`, and `proposedProfileImageUrl`. |
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
| `POST` | `/api/v1/student-services/events/proposals` | Admin-only. Body: `EventProposalSubmitDto`. |
| `GET` | `/api/v1/student-services/events/proposals` | Admin-only. Query: `search`, `status`, `page`, `limit`. |
| `GET` | `/api/v1/student-services/events/proposals/{proposalId}` | Admin-only. |
| `POST` | `/api/v1/student-services/events/proposals/{proposalId}/approve` | Admin-only. Body: optional `EventProposalApproveDto`. |
| `POST` | `/api/v1/student-services/events/proposals/{proposalId}/reject` | Admin-only. Body: `{ "reason": string }`. |
| `POST` | `/api/v1/student-services/events/proposals/{proposalId}/request-revision` | Admin-only. Body: `{ "changes": string }`. |

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
  - `image`: same value as `profileImageUrl` when present, otherwise `/clubs/default.png`
  - `proposedProfileImageUrl`: pending student-services upload when one exists
- `GET /api/v1/student-services/clubs/{clubId}` returns the mapped `ClubDto`, so clients should expect `ProfileImageUrl` and `ProposedProfileImageUrl`.
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
| `POST` | `/api/v1/club-admin/{clubId}/positions` | Body: `{ "title", "name"?, "categoryId", "requirements": string[] }`. |
| `PATCH` | `/api/v1/club-admin/{clubId}/positions/{positionId}` | Same body shape as create. |
| `DELETE` | `/api/v1/club-admin/{clubId}/positions/{positionId}` | Delete position. |

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
| `POST` | `/api/v1/club-admin/{clubId}/events/proposals` | Body: `EventProposalSubmitDto`. |
| `GET` | `/api/v1/club-admin/{clubId}/events/{eventId}/attendees` | Query: `page`, `limit`. |
| `PATCH` | `/api/v1/club-admin/{clubId}/events/{eventId}/attendees/{attendeeId}` | Body: `{ "entered": bool }`. |
| `DELETE` | `/api/v1/club-admin/{clubId}/events/{eventId}/attendees/{attendeeId}` | Delete attendee. |

### Profile and announcements

| Method | Path | Notes |
|--------|------|-------|
| `PATCH` | `/api/v1/club-admin/{clubId}/profile` | `multipart/form-data`. Fields: `socialLinks`, `focusAreasJson`, `email`, `resourcesJson`, `logoFile`, `backgroundFile`. Uploaded `logoFile` is persisted into the club's `ProfileImageUrl`, so later club `GET` endpoints return it. |
| `POST` | `/api/v1/club-admin/{clubId}/announcements` | Body: `{ "title": string, "message": string }`. |

Current profile patch response highlights:

- `clubId`
- `updated`
- `pendingReview`
- `profileImageUrl`
- `focusAreas`
- `email`
- `socialLinks`
- `resources`
- `logoUrl`
- `backgroundUrl`

Important image note:

- `logoUrl` is the direct uploaded file URL returned from storage for that request.
- `profileImageUrl` is the value persisted onto the club row and is what public club `GET` endpoints now read.
- `backgroundUrl` is returned when uploaded, but current public club `GET` endpoints do not expose a background image field.

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

The live swagger currently exposes these compatibility routes in addition to the main `/api/v1/...` surface:

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

`http://13.60.31.141:5000/club`

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

- Swagger UI: `http://13.60.31.141:5000/club/swagger/index.html`
- OpenAPI JSON: `http://13.60.31.141:5000/club/swagger/v1/swagger.json`

This markdown file is the human-readable summary. The OpenAPI document is the canonical live contract.
