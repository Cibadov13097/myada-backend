# Interview Logic for Frontend

Last updated: 2026-04-24

This guide documents the full interview flow implemented in `ClubManagementService` for frontend integration.

## Scope

- Applicant flow (view slots, book slot, read my booking)
- Club-admin flow (invite applicant, generate/edit/delete interview slots)
- Slot detail lookup
- Status transitions, permissions, and error handling

## Base URLs

- Local: `http://localhost:5003`
- Gateway: `http://51.20.193.29:5000/club`
- Relative API paths below are `api/v1/...`

## Core Concepts

- **Interview id** in interview endpoints is the **job application id**.
- **Slot id** is a stable GUID string (`InterviewSlot.SlotId`).
- Booking is stored on job application as `InterviewSlotId`.
- Slot capacity is enforced by counting all non-deleted applications already assigned to that slot.

## Status Model (Job Application)

- `Pending`
- `Reviewing`
- `InterviewInvited` -> applicant is allowed to book
- `InterviewScheduled` -> applicant already booked a slot
- `Approved`
- `Rejected`

Important transitions for interview flow:

- Admin invites: `Pending/Reviewing/...` -> `InterviewInvited`
- Applicant books slot: `InterviewInvited` -> `InterviewScheduled`

## Permissions and Access

- All interview endpoints require Bearer token.
- Caller user id is read from JWT claim `sub` or `user_id`.
- Non-admin applicant can only access their own application.
- Non-admin applicant can list/book slots only when status is `InterviewInvited`.
- Admin can bypass applicant/status checks for list/book/get-booking service calls.

## Applicant APIs

## 1) List available slots

- Alias route:
  - `GET /api/v1/applications/{applicationId}/interview-slots`
- Canonical route:
  - `GET /api/v1/interviews/{interviewId}/slots`

Query params:

- `from` (optional, `DateTimeOffset`)
- `to` (optional, `DateTimeOffset`)
- `date` (optional, `DateOnly`, UTC day filter)
- `includeFull` (optional, default `false`)

Behavior:

- Returns published future slots only.
- Slot pool is limited to:
  - same club as vacancy of this application
  - slot vacancy either null (club-wide) or matching application vacancy
- By default (`includeFull=false`) full slots are removed.

Response shapes:

- Alias route: `{ "items": InterviewSlotItem[] }`
- Canonical route: `{ "interviewId": "...", "slots": InterviewSlotItem[] }`

`InterviewSlotItem`:

- `id`
- `startAt`
- `endAt`
- `timezone` (`UTC`)
- `capacity`
- `bookedCount`
- `remainingCapacity`
- `status` (`ACTIVE` or `CANCELLED`)
- `bookable` (true when remaining capacity > 0)
- `locationType` (currently null)
- `meetingUrl` (currently null)
- `interviewerName` (currently null)

## 2) Book slot

- Alias route:
  - `POST /api/v1/applications/{applicationId}/interview-slot`
- Canonical route:
  - `POST /api/v1/interviews/{interviewId}/bookings`

Body:

```json
{
  "slotId": "b76d4e93-7d95-43ae-93de-5910b0d575f3"
}
```

Booking rules:

- `slotId` is required.
- Applicant must own the application (unless admin).
- Applicant status must be `InterviewInvited` (unless admin).
- Applicant can only have one active booking:
  - booking same slot again is idempotent success
  - booking a different slot returns conflict
- Slot must:
  - exist
  - be published
  - be in the future
  - belong to same club and valid vacancy scope
  - have free capacity

Successful side effects:

- `JobApplication.InterviewSlotId = slotId`
- `JobApplication.Status = InterviewScheduled`

Success responses:

- Canonical:
  - `201 Created` for first-time booking
  - `200 OK` for idempotent replay (same slot already booked)
- Alias:
  - `200 OK` with summary payload and `"status": "Interview Scheduled"`

Conflict/error expectations:

- `400`: invalid request (for example missing slotId, wrong interview-slot relationship)
- `401`: token missing/invalid identity claim
- `403`: ownership/permission failure
- `404`: application or slot not found
- `409`: status not open for booking, already booked, slot full/not bookable

## 3) Get my booking

- Canonical route:
  - `GET /api/v1/interviews/{interviewId}/my-booking`

Response:

- `200` with booking payload if application has `InterviewSlotId`
- `404` if no booking found
- `403` if application not owned by caller (non-admin)

## Slot Details API

- `GET /api/v1/slots/{slotId}`

Returns slot + computed capacity info:

- `id`, `startAt`, `endAt`, `timezone`
- `capacity`, `bookedCount`, `remainingCapacity`
- `status`
- `bookable` (published + future + remaining > 0)

Use this for detail/refresh if frontend already has a slot id.

## Club Admin APIs (Interview-related)

All are under:

- `api/v1/club-admin/{clubId}/...`

### 1) Invite application to interview

- `POST applications/{applicationId}/invite-to-interview`

Notes:

- `applicationId` here is typed id string from admin app list (for jobs, format like `job:{id}`).
- Sets status to `InterviewInvited`.
- If already `InterviewScheduled`, invite returns not found-style outcome via controller (`404`) because service rejects this transition.

### 2) List interview slots for club

- `GET interview-slots?page=1&limit=50`

### 3) Generate slots in bulk

- `POST interview-slots/generate`

Body:

```json
{
  "date": "2026-05-01",
  "endDate": "2026-05-03",
  "startTime": "09:00",
  "endTime": "12:00",
  "interviewLength": 30,
  "vacancy": "123"
}
```

Generation behavior:

- Creates contiguous slots of `interviewLength` minutes within each day window.
- `vacancy` optional:
  - empty -> slot is club-wide (`JobVacancyId = null`)
  - set -> must belong to same club
- Created slots are published by default with capacity `1`.

### 4) Patch slot

- `PATCH interview-slots/{slotId}`

Body fields:

- `startsAt?`
- `endsAt?`
- `vacancyId?` (empty string clears vacancy scope)
- `capacity?` (minimum enforced as 1)
- `isPublished?`

### 5) Delete slot

- `DELETE interview-slots/{slotId}`

Behavior:

- Soft delete (`IsDeleted = true`)

## Frontend Flow (Recommended)

## Applicant side

1. Show applications list/status from user applications endpoint.
2. If status is `InterviewInvited`, enable "Choose interview slot".
3. Call `GET /applications/{id}/interview-slots` with optional date filter.
4. Let user pick a `slotId`.
5. Call `POST /applications/{id}/interview-slot`.
6. On success, update local application state to `InterviewScheduled`.
7. Call `GET /interviews/{id}/my-booking` on details page to display final confirmed slot.

## Club-admin side

1. Generate slots (`interview-slots/generate`) before inviting.
2. Move suitable application to interview stage (`invite-to-interview`).
3. Optionally monitor slot inventory using `GET interview-slots`.
4. Edit or unpublish slots via patch; delete obsolete slots.

## UI State Rules

- Show "Book interview" only when application status is `InterviewInvited`.
- Show booked time card when status is `InterviewScheduled`.
- If booking API returns `409`, refresh slot list and prompt reselection.
- Use optimistic updates cautiously; capacity can change concurrently.

## Error Handling Cheat Sheet

- `400` -> input/validation issue, show inline form error.
- `401` -> auth expired or malformed token, force re-login.
- `403` -> user does not own this application or forbidden action.
- `404` -> application/slot missing, refresh source list.
- `409` -> race or invalid transition (already booked, full, not open).

## Known Current Limitations

- No cancel/reschedule endpoint for applicant booking yet.
- Slot metadata placeholders currently null:
  - `locationType`
  - `meetingUrl`
  - `interviewerName`
- Timezone is currently always UTC in responses.
