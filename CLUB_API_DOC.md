# Club Management API Documentation for Frontend

Last updated: 2026-04-13

## Base URLs

Recommended via API Gateway:
- Production: `http://13.60.196.144:5000`
- Local: `http://localhost:5000`

Club service gateway prefix:
- `/club`

Controller routes are defined as `/api/v1/...`, so final frontend URL format is:
- `http://13.60.196.144:5000/club/api/v1/...`

Direct service (diagnostics only):
- `http://localhost:5003/api/v1/...`

## Authentication

Most Club API endpoints require JWT bearer token.

Header:

```http
Authorization: Bearer <accessToken>
```

Public (currently no `[Authorize]` on controller):
- `/api/v1/categories/*`
- `/api/v1/event-registrations/*`
- `/api/v1/job-applications/*`
- `/api/v1/subevents/*`

Protected (`[Authorize]` required):
- `/api/v1/clubs/*`
- `/api/v1/events/*`
- `/api/v1/vacancies/*`
- `/api/v1/me/*`
- `/api/v1/club-admin/*`
- `/api/v1/club-proposals/*`
- `/api/v1/applications/*`
- `/api/v1/student-services/*`

## Quick Start (Frontend)

1. Login from Auth API (`POST /api/auth/login`) and keep `accessToken`.
2. Call club endpoints using gateway prefix `/club`.
3. On `401`, refresh token via `POST /api/auth/refresh` and retry request.

## Clubs API

Base path: `/club/api/v1/clubs`

### `GET /club/api/v1/clubs`
Query params:
- `search` (string, optional)
- `category` (string, optional)
- `page` (int, default `1`)
- `limit` (int, default `12`)

Response:

```json
{
  "items": [
    {
      "id": 1,
      "name": "Chess Club",
      "category": "Sports",
      "image": "/clubs/default.png",
      "tags": [],
      "members": 0,
      "about": "...",
      "status": "Approved"
    }
  ],
  "total": 1,
  "page": 1,
  "limit": 12
}
```

### `GET /club/api/v1/clubs/{clubId}`
Response includes club detail + placeholder fields (`focusAreas`, `officers`, etc.).

### `GET /club/api/v1/clubs/{clubId}/members`
Returns:

```json
{ "items": [] }
```

### `POST /club/api/v1/clubs/{clubId}/join-applications`
`multipart/form-data` body:
- `letterOfPurpose` (string, required)
- `portfolioLinks` (string, optional)
- `portfolioFiles` (file[], optional)

Response:

```json
{
  "applicationId": "MEM-1712900000",
  "status": "Submitted",
  "submittedAt": "2026-04-13T00:00:00Z"
}
```

### `POST /club/api/v1/clubs`
Body (`CreateClubDto`):

```json
{
  "name": "Robotics Club",
  "description": "...",
  "approvedBy": "00000000-0000-0000-0000-000000000000",
  "categoryId": 1
}
```

### `PATCH /club/api/v1/clubs/{clubId}`
Body (`UpdateClubDto`):

```json
{
  "name": "Robotics Club",
  "description": "...",
  "status": "Approved"
}
```

### `DELETE /club/api/v1/clubs/{clubId}`
Response: `204 No Content`

## Categories API (Public)

Base path: `/club/api/v1/categories`

- `GET /club/api/v1/categories`
- `GET /club/api/v1/categories/{id}`
- `POST /club/api/v1/categories`
- `PUT /club/api/v1/categories/{id}`
- `DELETE /club/api/v1/categories/{id}`

Create/update body:

```json
{ "name": "Sports" }
```

## Events API

Base path: `/club/api/v1/events`

### `GET /club/api/v1/events`
Query params:
- `clubId` (int?, currently not applied in filtering)
- `myClubsOnly` (bool, currently not applied)
- `search` (string?)
- `page` (int, default `1`)
- `limit` (int, default `12`)

### `GET /club/api/v1/events/{eventId}`
Returns `EventDto`.

### `POST /club/api/v1/events/{eventId}/registrations`
No body.
Returns generated ticket-like payload.

### `DELETE /club/api/v1/events/{eventId}/registrations`
Returns `204 No Content`.

### `GET /club/api/v1/events/{eventId}/ticket`
Returns temporary ticket payload.

### `POST /club/api/v1/events`
Body (`CreateEventDto`):

```json
{
  "name": "Hackathon",
  "description": "...",
  "startTime": "2026-05-01T10:00:00Z",
  "endTime": "2026-05-01T18:00:00Z",
  "location": "Main Hall",
  "seatLimit": 100,
  "requirements": "Laptop",
  "prerequisites": "Basic coding",
  "notes": "Bring ID",
  "approvedBy": "00000000-0000-0000-0000-000000000000"
}
```

### `PATCH /club/api/v1/events/{eventId}`
Body (`UpdateEventDto`):

```json
{
  "name": "Hackathon 2026",
  "description": "...",
  "startTime": "2026-05-01T10:00:00Z",
  "endTime": "2026-05-01T18:00:00Z",
  "location": "Main Hall",
  "seatLimit": 120,
  "requirements": "Laptop",
  "prerequisites": "Basic coding",
  "notes": "Bring ID",
  "status": "Approved"
}
```

### `DELETE /club/api/v1/events/{eventId}`
Response: `204 No Content`

## Event Registrations API (Public)

Base path: `/club/api/v1/event-registrations`

- `GET /club/api/v1/event-registrations`
- `GET /club/api/v1/event-registrations/by-event/{eventId}` (`eventId` is `guid`)
- `GET /club/api/v1/event-registrations/{id}`
- `POST /club/api/v1/event-registrations`
- `DELETE /club/api/v1/event-registrations/{id}`

Create body:

```json
{
  "eventId": "00000000-0000-0000-0000-000000000000",
  "userId": "00000000-0000-0000-0000-000000000000"
}
```

## Vacancies API

Base path: `/club/api/v1/vacancies`

### `GET /club/api/v1/vacancies`
Query params:
- `search` (string?)
- `category` (string?, currently not applied)
- `savedOnly` (bool, currently not applied)
- `page` (int, default `1`)
- `limit` (int, default `12`)

### `GET /club/api/v1/vacancies/{vacancyId}`
Returns `JobVacancyDto`.

### `POST /club/api/v1/vacancies/{vacancyId}/applications`
`multipart/form-data` body:
- `purposeOfApplication` (string, required)
- `cvFile` (file, optional)

Response:

```json
{
  "applicationId": "APP-123",
  "status": "Submitted"
}
```

### `GET /club/api/v1/vacancies/by-club/{clubId}`
Returns vacancy list by club.

### `POST /club/api/v1/vacancies`
Body (`CreateJobVacancyDto`):

```json
{
  "clubId": 1,
  "title": "UI/UX Lead",
  "description": "...",
  "postedBy": "00000000-0000-0000-0000-000000000000"
}
```

### `PATCH /club/api/v1/vacancies/{vacancyId}`
Body (`UpdateJobVacancyDto`):

```json
{
  "title": "UI/UX Lead",
  "description": "Updated"
}
```

### `DELETE /club/api/v1/vacancies/{vacancyId}`
Response: `204 No Content`

## Job Applications API (Public)

Base path: `/club/api/v1/job-applications`

- `GET /club/api/v1/job-applications`
- `GET /club/api/v1/job-applications/by-vacancy/{jobVacancyId}`
- `GET /club/api/v1/job-applications/{id}`
- `POST /club/api/v1/job-applications`
- `PATCH /club/api/v1/job-applications/{id}/status`
- `DELETE /club/api/v1/job-applications/{id}`

Create body:

```json
{
  "jobVacancyId": 1,
  "applicantId": "00000000-0000-0000-0000-000000000000",
  "experienceLinks": "https://github.com/user",
  "fileUrl": "cv.pdf"
}
```

Status patch body:

```json
{ "status": "Approved" }
```

Allowed status values:
- `Pending`
- `Approved`
- `Rejected`
- `Cancelled`

## Subevents API (Public)

Base path: `/club/api/v1/subevents`

- `GET /club/api/v1/subevents`
- `GET /club/api/v1/subevents/by-event/{mainEventId}`
- `GET /club/api/v1/subevents/{id}`
- `POST /club/api/v1/subevents`
- `PUT /club/api/v1/subevents/{id}`
- `DELETE /club/api/v1/subevents/{id}`

Create body:

```json
{
  "mainEventId": 1,
  "name": "Workshop A",
  "startTime": "2026-05-01T10:00:00Z",
  "endTime": "2026-05-01T12:00:00Z"
}
```

## Me API

Base path: `/club/api/v1/me` (protected)

- `GET /club/api/v1/me/club-memberships`
- `GET /club/api/v1/me/vacancy-applications`
- `GET /club/api/v1/me/membership-applications`
- `GET /club/api/v1/me/event-registrations`
- `GET /club/api/v1/me/club-notifications?type=all`
- `PATCH /club/api/v1/me/club-notifications/{notificationId}/read`

## Club Proposals API

Base path: `/club/api/v1/club-proposals` (protected)

### `POST /club/api/v1/club-proposals`
`multipart/form-data` body:
- `name` (string)
- `description` (string)
- `logoFile` (file)
- `constitutionFile` (file)

Response:

```json
{
  "proposalId": "PROP-2026-1712900000",
  "status": "pending_review"
}
```

## Applications API

Base path: `/club/api/v1/applications` (protected)

- `GET /club/api/v1/applications/{applicationId}/interview-slots`
- `POST /club/api/v1/applications/{applicationId}/interview-slot`

Select slot body:

```json
{ "slotId": "slot-1" }
```

## Club Admin API

Base path: `/club/api/v1/club-admin/{clubId}` (protected)

Dashboard and application management:
- `GET /dashboard`
- `GET /applications`
- `GET /applications/{applicationId}`
- `POST /applications/{applicationId}/approve`
- `POST /applications/{applicationId}/reject` body: `{ "reason": "..." }`
- `POST /applications/{applicationId}/request-changes` body: `{ "changes": "..." }`
- `POST /applications/{applicationId}/notes` body: `{ "note": "..." }`
- `POST /applications/bulk-decision` body:
  ```json
  {
    "applicationIds": [1, 2],
    "decision": "approve",
    "reason": "optional"
  }
  ```

Interview slots:
- `GET /interview-slots`
- `POST /interview-slots/generate` body:
  ```json
  {
    "date": "2026-05-10",
    "startTime": "10:00",
    "endTime": "18:00",
    "interviewLength": 30,
    "vacancy": "Designer"
  }
  ```
- `PATCH /interview-slots/{slotId}` body: `object`
- `DELETE /interview-slots/{slotId}`
- `POST /interview-slots/bulk-assign-vacancy` body: `object`

Vacancies (club-scoped):
- `GET /vacancies`
- `POST /vacancies` body:
  ```json
  {
    "title": "Designer",
    "description": "..."
  }
  ```
- `PATCH /vacancies/{vacancyId}` body:
  ```json
  {
    "title": "Designer",
    "description": "Updated"
  }
  ```
- `PATCH /vacancies/{vacancyId}/status` body: `{ "status": "active" }`

Positions / members / employees:
- `GET /positions`
- `POST /positions` body: `object`
- `PATCH /positions/{positionId}` body: `object`
- `DELETE /positions/{positionId}`
- `GET /members`
- `DELETE /members/{memberId}`
- `GET /employees`
- `POST /employees` body:
  ```json
  {
    "studentId": "S12345",
    "position": "Coordinator"
  }
  ```
- `PATCH /employees/positions` body:
  ```json
  {
    "changes": [
      { "id": 1, "position": "Lead" }
    ]
  }
  ```
- `DELETE /employees/{employeeId}`

Events / profile / announcements:
- `GET /events`
- `PATCH /events/{eventId}` body: `object`
- `POST /events/proposals` body: `object`
- `GET /events/{eventId}/attendees`
- `PATCH /events/{eventId}/attendees/{attendeeId}` body:
  ```json
  { "entered": true }
  ```
- `DELETE /events/{eventId}/attendees/{attendeeId}`
- `PATCH /profile` (`multipart/form-data`):
  - `socialLinks` (string?)
  - `focusAreasJson` (string?)
  - `logoFile` (file?)
  - `backgroundFile` (file?)
- `POST /announcements` body:
  ```json
  {
    "title": "Meeting",
    "message": "Tomorrow 10:00"
  }
  ```

## Student Services API

Base path: `/club/api/v1/student-services` (protected)

- `GET /dashboard`
- `GET /clubs?search=&page=1&limit=12`
- `GET /clubs/{clubId}`
- `GET /club-proposals`
- `GET /club-proposals/{proposalId}`
- `POST /club-proposals/{proposalId}/approve`
- `POST /club-proposals/{proposalId}/reject` body: `{ "reason": "..." }`
- `POST /club-proposals/{proposalId}/request-revision` body: `{ "changes": "..." }`
- `GET /club-proposal-requirements`
- `PUT /club-proposal-requirements` body:
  ```json
  {
    "requirements": ["..."],
    "deadline": "2026-05-30T00:00:00Z"
  }
  ```
- `GET /event-proposals`
- `GET /event-proposals/{proposalId}`
- `POST /event-proposals/{proposalId}/approve` body:
  ```json
  {
    "assignments": [
      { "subEventIndex": 0, "buildingId": "B1", "roomId": "R101" }
    ]
  }
  ```
- `POST /event-proposals/{proposalId}/reject` body: `{ "reason": "..." }`
- `POST /event-proposals/{proposalId}/request-revision` body: `{ "changes": "..." }`
- `GET /events`
- `POST /events` body:
  ```json
  {
    "name": "Workshop",
    "description": "...",
    "startTime": "2026-05-01T10:00:00Z",
    "endTime": "2026-05-01T12:00:00Z",
    "location": "B1",
    "seatLimit": 30,
    "requirements": "Laptop",
    "prerequisites": "None",
    "notes": "Optional"
  }
  ```
- `PATCH /events/{eventId}` body: `object`
- `GET /events/{eventId}`
- `DELETE /events/{eventId}`
- `PATCH /clubs/{clubId}` body:
  ```json
  {
    "name": "New Club Name",
    "description": "Updated",
    "status": "Approved"
  }
  ```
- `POST /clubs/{clubId}/profile-image/approve`
- `GET /clubs/{clubId}/members`
- `PATCH /clubs/{clubId}/members/{memberId}` body: `object`
- `DELETE /clubs/{clubId}/members/{memberId}`
- `GET /clubs/{clubId}/employees`
- `POST /clubs/{clubId}/employees` body: `object`
- `PATCH /clubs/{clubId}/employees/{employeeId}` body: `object`
- `DELETE /clubs/{clubId}/employees/{employeeId}`

## Enum Values

`ClubStatus`:
- `Pending`
- `Approved`
- `Rejected`
- `Archived`

`JobApplicationStatus`:
- `Pending`
- `Approved`
- `Rejected`
- `Cancelled`

## Known Implementation Notes (Important for Frontend)

- Many admin/student-service endpoints currently return placeholder payloads (`items: []`, static status objects). Treat these as evolving contracts.
- `EventsController` registration endpoint currently creates a registration with generated `EventId` (`Guid.NewGuid()`) instead of route `eventId`. Ticket payload is also generated sample data.
- Some filters are present in query params but not yet implemented in backend logic (`category`, `savedOnly`, `myClubsOnly`, etc.).
- Swagger UI for Club service is enabled only in Development environment in service code.
