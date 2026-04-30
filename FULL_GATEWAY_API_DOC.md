# MyAda Full Gateway API Documentation

Last updated: 2026-04-26

## Base URL

- Gateway: http://13.60.31.141:500
- All frontend calls should start from this base URL.

## Service Prefix Map

| Service | Gateway Prefix | Swagger |
|---|---|---|
| Auth Service | /api/auth (primary), /auth (alias) | http://13.60.31.141:500/auth/swagger/index.html |
| Attendance Service | /attendance | http://13.60.31.141:500/attendance/swagger/index.html |
| Lost&Found Service | /lostfound | http://13.60.31.141:500/lostfound/swagger/index.html |
| Club Management Service | /club | http://13.60.31.141:500/club/swagger/index.html |
| Location Service | /location | http://13.60.31.141:500/location/swagger/index.html |
| Support Service | /support | http://13.60.31.141:500/support/swagger/index.html |
| Notification Service | /notification | http://13.60.31.141:500/notification/swagger/index.html |
| Call Service | /call | http://13.60.31.141:500/call/swagger/index.html |

## Important Notes

- Through gateway, always include service prefix (/attendance, /lostfound, /club, /location, /support, /notification, /call).
- Attendance student QR scan supports both routes:
  - POST /attendance/api/students/{studentId}/attendance/scan
  - POST /attendance/api/students/{studentId}/attendance/qr/scan
- Lost&Found accepts nullable location. If frontend uses locationType=other, send location as string.

## Endpoint Inventory (Gateway URLs)

### Auth Service

Primary auth routes use /api/auth. Gateway alias /auth/* is also available.

| Method | URL |
|---|---|
| GET | http://13.60.31.141:500/.well-known/jwks.json |
| PUT | http://13.60.31.141:500/api/auth/change-password |
| GET | http://13.60.31.141:500/api/auth/create-admin |
| POST | http://13.60.31.141:500/api/auth/create-user-with-role |
| POST | http://13.60.31.141:500/api/auth/forgot-password |
| POST | http://13.60.31.141:500/api/auth/login |
| POST | http://13.60.31.141:500/api/auth/logout |
| POST | http://13.60.31.141:500/api/auth/refresh |
| POST | http://13.60.31.141:500/api/auth/reset-password |
| GET | http://13.60.31.141:500/api/auth/users/{id} |
| POST | http://13.60.31.141:500/api/auth/users/{id}/profile-photo |
| GET | http://13.60.31.141:500/api/auth/users-by-role/{role} |
| GET | http://13.60.31.141:500/auth/.well-known/jwks.json |
| PUT | http://13.60.31.141:500/auth/api/auth/change-password |
| GET | http://13.60.31.141:500/auth/api/auth/create-admin |
| POST | http://13.60.31.141:500/auth/api/auth/create-user-with-role |
| POST | http://13.60.31.141:500/auth/api/auth/forgot-password |
| POST | http://13.60.31.141:500/auth/api/auth/login |
| POST | http://13.60.31.141:500/auth/api/auth/logout |
| POST | http://13.60.31.141:500/auth/api/auth/refresh |
| POST | http://13.60.31.141:500/auth/api/auth/reset-password |
| GET | http://13.60.31.141:500/auth/api/auth/users/{id} |
| POST | http://13.60.31.141:500/auth/api/auth/users/{id}/profile-photo |
| GET | http://13.60.31.141:500/auth/api/auth/users-by-role/{role} |

### Call Service

SignalR hub + WebRTC ICE configuration endpoint.

| Method | URL |
|---|---|
| WS | http://13.60.31.141:500/call/hub |
| POST | http://13.60.31.141:500/call/hub/negotiate?negotiateVersion=1 |
| GET | http://13.60.31.141:500/call/webrtc/ice-servers |

### Club Management Service

Most routes are under /api/v1/*.

| Method | URL |
|---|---|
| GET | http://13.60.31.141:500/club/api/events |
| POST | http://13.60.31.141:500/club/api/events |
| DELETE | http://13.60.31.141:500/club/api/events/{eventId:int} |
| GET | http://13.60.31.141:500/club/api/events/{eventId:int} |
| PATCH | http://13.60.31.141:500/club/api/events/{eventId:int} |
| POST | http://13.60.31.141:500/club/api/events/{eventId:int}/check-in |
| DELETE | http://13.60.31.141:500/club/api/events/{eventId:int}/registrations |
| POST | http://13.60.31.141:500/club/api/events/{eventId:int}/registrations |
| GET | http://13.60.31.141:500/club/api/events/{eventId:int}/ticket |
| GET | http://13.60.31.141:500/club/api/users/me/event-registrations |
| POST | http://13.60.31.141:500/club/api/v1/applications/{applicationId}/interview-slot |
| GET | http://13.60.31.141:500/club/api/v1/applications/{applicationId}/interview-slots |
| GET | http://13.60.31.141:500/club/api/v1/categories |
| POST | http://13.60.31.141:500/club/api/v1/categories |
| DELETE | http://13.60.31.141:500/club/api/v1/categories/{id:int} |
| GET | http://13.60.31.141:500/club/api/v1/categories/{id:int} |
| PUT | http://13.60.31.141:500/club/api/v1/categories/{id:int} |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/announcements |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/applications |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/applications/{applicationId} |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/applications/{applicationId}/approve |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/applications/{applicationId}/notes |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/applications/{applicationId}/reject |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/applications/{applicationId}/request-changes |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/applications/bulk-decision |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/dashboard |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/files |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/files |
| DELETE | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/files/{fileId} |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/files/{fileId} |
| PATCH | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/files/{fileId} |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/employees |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/employees |
| DELETE | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/employees/{employeeId} |
| PATCH | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/employees/positions |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/events |
| PATCH | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/events/{eventId} |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/events/{eventId}/attendees |
| DELETE | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/events/{eventId}/attendees/{attendeeId} |
| PATCH | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/events/{eventId}/attendees/{attendeeId} |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/events/proposals |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/interview-slots |
| DELETE | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/interview-slots/{slotId} |
| PATCH | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/interview-slots/{slotId} |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/interview-slots/generate |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/members |
| DELETE | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/members/{memberId} |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/positions |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/positions |
| DELETE | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/positions/{positionId} |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/positions/{positionId} |
| PATCH | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/positions/{positionId} |
| PATCH | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/profile |
| GET | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/vacancies |
| POST | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/vacancies |
| PATCH | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/vacancies/{vacancyId} |
| PATCH | http://13.60.31.141:500/club/api/v1/club-admin/{clubId}/vacancies/{vacancyId}/status |
| GET | http://13.60.31.141:500/club/api/v1/club-memberships |
| POST | http://13.60.31.141:500/club/api/v1/club-memberships |
| DELETE | http://13.60.31.141:500/club/api/v1/club-memberships/{id:int} |
| GET | http://13.60.31.141:500/club/api/v1/club-memberships/{id:int} |
| PUT | http://13.60.31.141:500/club/api/v1/club-memberships/{id:int} |
| GET | http://13.60.31.141:500/club/api/v1/club-position-requirements |
| POST | http://13.60.31.141:500/club/api/v1/club-position-requirements |
| DELETE | http://13.60.31.141:500/club/api/v1/club-position-requirements/{id:int} |
| GET | http://13.60.31.141:500/club/api/v1/club-position-requirements/{id:int} |
| PUT | http://13.60.31.141:500/club/api/v1/club-position-requirements/{id:int} |
| POST | http://13.60.31.141:500/club/api/v1/club-proposals |
| GET | http://13.60.31.141:500/club/api/v1/clubs |
| POST | http://13.60.31.141:500/club/api/v1/clubs |
| DELETE | http://13.60.31.141:500/club/api/v1/clubs/{clubId:int} |
| GET | http://13.60.31.141:500/club/api/v1/clubs/{clubId:int} |
| PATCH | http://13.60.31.141:500/club/api/v1/clubs/{clubId:int} |
| POST | http://13.60.31.141:500/club/api/v1/clubs/{clubId:int}/join-applications |
| GET | http://13.60.31.141:500/club/api/v1/clubs/{clubId:int}/members |
| GET | http://13.60.31.141:500/club/api/v1/event-registrations |
| POST | http://13.60.31.141:500/club/api/v1/event-registrations |
| DELETE | http://13.60.31.141:500/club/api/v1/event-registrations/{id:int} |
| GET | http://13.60.31.141:500/club/api/v1/event-registrations/{id:int} |
| PUT | http://13.60.31.141:500/club/api/v1/event-registrations/{id:int} |
| GET | http://13.60.31.141:500/club/api/v1/event-registrations/by-event/{eventId:int} |
| GET | http://13.60.31.141:500/club/api/v1/job-applications |
| POST | http://13.60.31.141:500/club/api/v1/job-applications |
| DELETE | http://13.60.31.141:500/club/api/v1/job-applications/{id:int} |
| GET | http://13.60.31.141:500/club/api/v1/job-applications/{id:int} |
| PATCH | http://13.60.31.141:500/club/api/v1/job-applications/{id:int}/status |
| GET | http://13.60.31.141:500/club/api/v1/job-applications/by-vacancy/{jobVacancyId:int} |
| GET | http://13.60.31.141:500/club/api/v1/student-services/club-proposal-requirements |
| PUT | http://13.60.31.141:500/club/api/v1/student-services/club-proposal-requirements |
| GET | http://13.60.31.141:500/club/api/v1/student-services/clubs |
| GET | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int} |
| PATCH | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int} |
| GET | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int}/employees |
| POST | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int}/employees |
| DELETE | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int}/employees/{employeeId} |
| PATCH | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int}/employees/{employeeId} |
| GET | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int}/members |
| DELETE | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int}/members/{memberId} |
| PATCH | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int}/members/{memberId} |
| POST | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int}/profile-image |
| POST | http://13.60.31.141:500/club/api/v1/student-services/clubs/{clubId:int}/profile-image/approve |
| GET | http://13.60.31.141:500/club/api/v1/student-services/clubs/proposals |
| GET | http://13.60.31.141:500/club/api/v1/student-services/clubs/proposals/{proposalId} |
| POST | http://13.60.31.141:500/club/api/v1/student-services/clubs/proposals/{proposalId}/approve |
| POST | http://13.60.31.141:500/club/api/v1/student-services/clubs/proposals/{proposalId}/reject |
| POST | http://13.60.31.141:500/club/api/v1/student-services/clubs/proposals/{proposalId}/request-revision |
| GET | http://13.60.31.141:500/club/api/v1/student-services/dashboard |
| GET | http://13.60.31.141:500/club/api/v1/student-services/events |
| POST | http://13.60.31.141:500/club/api/v1/student-services/events |
| DELETE | http://13.60.31.141:500/club/api/v1/student-services/events/{eventId:int} |
| GET | http://13.60.31.141:500/club/api/v1/student-services/events/{eventId:int} |
| PATCH | http://13.60.31.141:500/club/api/v1/student-services/events/{eventId:int} |
| GET | http://13.60.31.141:500/club/api/v1/student-services/events/proposals |
| POST | http://13.60.31.141:500/club/api/v1/student-services/events/proposals |
| GET | http://13.60.31.141:500/club/api/v1/student-services/events/proposals/{proposalId} |
| POST | http://13.60.31.141:500/club/api/v1/student-services/events/proposals/{proposalId}/approve |
| POST | http://13.60.31.141:500/club/api/v1/student-services/events/proposals/{proposalId}/reject |
| POST | http://13.60.31.141:500/club/api/v1/student-services/events/proposals/{proposalId}/request-revision |
| GET | http://13.60.31.141:500/club/api/v1/subevents |
| POST | http://13.60.31.141:500/club/api/v1/subevents |
| DELETE | http://13.60.31.141:500/club/api/v1/subevents/{id:int} |
| GET | http://13.60.31.141:500/club/api/v1/subevents/{id:int} |
| PUT | http://13.60.31.141:500/club/api/v1/subevents/{id:int} |
| GET | http://13.60.31.141:500/club/api/v1/subevents/by-event/{mainEventId:int} |
| GET | http://13.60.31.141:500/club/api/v1/users/{userId:guid}/club-memberships |
| GET | http://13.60.31.141:500/club/api/v1/users/{userId:guid}/club-notifications |
| PATCH | http://13.60.31.141:500/club/api/v1/users/{userId:guid}/club-notifications/{notificationId}/read |
| GET | http://13.60.31.141:500/club/api/v1/users/{userId:guid}/event-registrations |
| GET | http://13.60.31.141:500/club/api/v1/users/{userId:guid}/membership-applications |
| GET | http://13.60.31.141:500/club/api/v1/users/{userId:guid}/vacancy-applications |
| GET | http://13.60.31.141:500/club/api/v1/vacancies |
| POST | http://13.60.31.141:500/club/api/v1/vacancies |
| DELETE | http://13.60.31.141:500/club/api/v1/vacancies/{vacancyId} |
| GET | http://13.60.31.141:500/club/api/v1/vacancies/{vacancyId} |
| PATCH | http://13.60.31.141:500/club/api/v1/vacancies/{vacancyId} |
| POST | http://13.60.31.141:500/club/api/v1/vacancies/{vacancyId}/applications |
| GET | http://13.60.31.141:500/club/api/v1/vacancies/by-club/{clubId:int} |

### Location Service

Read endpoints are public; write endpoints require admin role.

| Method | URL |
|---|---|
| GET | http://13.60.31.141:500/location/api/v1/buildings |
| POST | http://13.60.31.141:500/location/api/v1/buildings |
| DELETE | http://13.60.31.141:500/location/api/v1/buildings/{id} |
| GET | http://13.60.31.141:500/location/api/v1/buildings/{id} |
| PUT | http://13.60.31.141:500/location/api/v1/buildings/{id} |
| GET | http://13.60.31.141:500/location/api/v1/locations/details |
| POST | http://13.60.31.141:500/location/api/v1/locations/validate-room-location |
| GET | http://13.60.31.141:500/location/api/v1/rooms |
| POST | http://13.60.31.141:500/location/api/v1/rooms |
| DELETE | http://13.60.31.141:500/location/api/v1/rooms/{id:int} |
| GET | http://13.60.31.141:500/location/api/v1/rooms/{id:int} |
| PUT | http://13.60.31.141:500/location/api/v1/rooms/{id:int} |
| GET | http://13.60.31.141:500/location/api/v1/rooms/by-building/{buildingId:int} |

### Lost&Found Service

Includes modern /api/lost-and-found and legacy /api/v1/* routes.

| Method | URL |
|---|---|
| GET | http://13.60.31.141:500/lostfound/api/lost-and-found/items |
| POST | http://13.60.31.141:500/lostfound/api/lost-and-found/items |
| GET | http://13.60.31.141:500/lostfound/api/lost-and-found/items/{id:int} |
| PATCH | http://13.60.31.141:500/lostfound/api/lost-and-found/items/{id:int} |
| GET | http://13.60.31.141:500/lostfound/api/lost-and-found/items/{id:int}/claims |
| POST | http://13.60.31.141:500/lostfound/api/lost-and-found/items/{id:int}/claims |
| POST | http://13.60.31.141:500/lostfound/api/lost-and-found/items/{id:int}/complete-delivery |
| POST | http://13.60.31.141:500/lostfound/api/lost-and-found/items/{id:int}/confirm-receipt |
| POST | http://13.60.31.141:500/lostfound/api/lost-and-found/items/{id:int}/notify |
| GET | http://13.60.31.141:500/lostfound/api/lost-and-found/items/{id:int}/timeline |
| POST | http://13.60.31.141:500/lostfound/api/lost-and-found/reports/found |
| POST | http://13.60.31.141:500/lostfound/api/lost-and-found/reports/lost |
| GET | http://13.60.31.141:500/lostfound/api/lost-and-found/stats |
| POST | http://13.60.31.141:500/lostfound/api/lost-and-found/upload |
| POST | http://13.60.31.141:500/lostfound/api/v1/admin/found-items |
| DELETE | http://13.60.31.141:500/lostfound/api/v1/admin/found-items/{id} |
| GET | http://13.60.31.141:500/lostfound/api/v1/admin/found-items/{id} |
| PUT | http://13.60.31.141:500/lostfound/api/v1/admin/found-items/{id} |
| DELETE | http://13.60.31.141:500/lostfound/api/v1/admin/found-items/{id}/force |
| GET | http://13.60.31.141:500/lostfound/api/v1/admin/lost-items |
| POST | http://13.60.31.141:500/lostfound/api/v1/admin/lost-items |
| DELETE | http://13.60.31.141:500/lostfound/api/v1/admin/lost-items/{id} |
| GET | http://13.60.31.141:500/lostfound/api/v1/admin/lost-items/{id} |
| PUT | http://13.60.31.141:500/lostfound/api/v1/admin/lost-items/{id} |
| DELETE | http://13.60.31.141:500/lostfound/api/v1/admin/lost-items/{id}/force |
| GET | http://13.60.31.141:500/lostfound/api/v1/categories |
| POST | http://13.60.31.141:500/lostfound/api/v1/categories |
| DELETE | http://13.60.31.141:500/lostfound/api/v1/categories/{id:int} |
| GET | http://13.60.31.141:500/lostfound/api/v1/categories/{id:int} |
| PUT | http://13.60.31.141:500/lostfound/api/v1/categories/{id:int} |
| GET | http://13.60.31.141:500/lostfound/api/v1/found-items |
| POST | http://13.60.31.141:500/lostfound/api/v1/found-items |
| DELETE | http://13.60.31.141:500/lostfound/api/v1/found-items/{id} |
| GET | http://13.60.31.141:500/lostfound/api/v1/found-items/{id} |
| PUT | http://13.60.31.141:500/lostfound/api/v1/found-items/{id} |
| GET | http://13.60.31.141:500/lostfound/api/v1/found-items/users/me |
| GET | http://13.60.31.141:500/lostfound/api/v1/found-items/users/me/{id} |
| GET | http://13.60.31.141:500/lostfound/api/v1/lost-items |
| POST | http://13.60.31.141:500/lostfound/api/v1/lost-items |
| DELETE | http://13.60.31.141:500/lostfound/api/v1/lost-items/{id} |
| GET | http://13.60.31.141:500/lostfound/api/v1/lost-items/{id} |
| PUT | http://13.60.31.141:500/lostfound/api/v1/lost-items/{id} |
| GET | http://13.60.31.141:500/lostfound/api/v1/lost-items/users/me |
| GET | http://13.60.31.141:500/lostfound/api/v1/lost-items/users/me/{id} |

### Attendance Service

Use gateway prefix /attendance for all attendance endpoints.

| Method | URL |
|---|---|
| GET | http://13.60.31.141:500/attendance/api/admin/courses |
| POST | http://13.60.31.141:500/attendance/api/admin/courses |
| DELETE | http://13.60.31.141:500/attendance/api/admin/courses/{courseId:int} |
| GET | http://13.60.31.141:500/attendance/api/admin/courses/{courseId:int} |
| PUT | http://13.60.31.141:500/attendance/api/admin/courses/{courseId:int} |
| GET | http://13.60.31.141:500/attendance/api/admin/courses/{courseId:int}/lessons |
| GET | http://13.60.31.141:500/attendance/api/admin/instructors/{instructorId:guid}/lessons |
| GET | http://13.60.31.141:500/attendance/api/admin/lessons |
| POST | http://13.60.31.141:500/attendance/api/admin/lessons |
| DELETE | http://13.60.31.141:500/attendance/api/admin/lessons/{lessonId:int} |
| GET | http://13.60.31.141:500/attendance/api/admin/lessons/{lessonId:int} |
| PUT | http://13.60.31.141:500/attendance/api/admin/lessons/{lessonId:int} |
| GET | http://13.60.31.141:500/attendance/api/admin/lessons/{lessonId:int}/enrollments |
| POST | http://13.60.31.141:500/attendance/api/admin/lessons/{lessonId:int}/enrollments |
| GET | http://13.60.31.141:500/attendance/api/admin/lessons/{lessonId:int}/sessions |
| POST | http://13.60.31.141:500/attendance/api/admin/lessons/{lessonId:int}/sessions/generate |
| GET | http://13.60.31.141:500/attendance/api/admin/lessons/by-term |
| GET | http://13.60.31.141:500/attendance/api/admin/lessons/scheduling |
| GET | http://13.60.31.141:500/attendance/api/admin/sessions/{sessionId:int}/attendance |
| DELETE | http://13.60.31.141:500/attendance/api/admin/sessions/{sessionId:int}/attendance/{attendanceId:int} |
| PATCH | http://13.60.31.141:500/attendance/api/admin/sessions/{sessionId:int}/attendance/{attendanceId:int} |
| GET | http://13.60.31.141:500/attendance/api/admin/users/roles/{role} |
| POST | http://13.60.31.141:500/attendance/api/admin/users/sync |
| GET | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/lessons |
| GET | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/lessons/{lessonId:int} |
| GET | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/lessons/{lessonId:int}/sessions |
| POST | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/lessons/{lessonId:int}/sessions |
| DELETE | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/lessons/{lessonId:int}/sessions/{sessionId:int} |
| GET | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/lessons/{lessonId:int}/sessions/{sessionId:int} |
| PATCH | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/lessons/{lessonId:int}/sessions/{sessionId:int} |
| POST | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/lessons/{lessonId:int}/sessions/generate |
| GET | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/sessions/{sessionId:int}/attendance |
| PATCH | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/sessions/{sessionId:int}/attendance/{studentId:guid} |
| POST | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/sessions/{sessionId:int}/attendance/activate/{round:int} |
| POST | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/sessions/{sessionId:int}/attendance/bulk-absent |
| POST | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/sessions/{sessionId:int}/attendance/deactivate/{round:int} |
| POST | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/sessions/{sessionId:int}/attendance/finalize |
| GET | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/sessions/{sessionId:int}/attendance/summary |
| POST | http://13.60.31.141:500/attendance/api/instructors/{instructorId:guid}/sessions/{sessionId:int}/qr-token |
| POST | http://13.60.31.141:500/attendance/api/students/{studentId:guid}/attendance/qr/scan |
| POST | http://13.60.31.141:500/attendance/api/students/{studentId:guid}/attendance/scan |
| GET | http://13.60.31.141:500/attendance/api/students/{studentId:guid}/enrollments |
| GET | http://13.60.31.141:500/attendance/api/students/{studentId:guid}/lessons/{lessonId:int}/attendance |

### Support Service

Support requests and support staff workflows.

| Method | URL |
|---|---|
| POST | http://13.60.31.141:500/support/api/Attachments |
| DELETE | http://13.60.31.141:500/support/api/Attachments/{attachmentId:int} |
| GET | http://13.60.31.141:500/support/api/Attachments/request/{requestId:int} |
| GET | http://13.60.31.141:500/support/api/Categories |
| POST | http://13.60.31.141:500/support/api/Categories |
| DELETE | http://13.60.31.141:500/support/api/Categories/{id} |
| GET | http://13.60.31.141:500/support/api/Categories/module/{module} |
| GET | http://13.60.31.141:500/support/api/SupportRequests |
| GET | http://13.60.31.141:500/support/api/SupportRequests/{requestId:int} |
| GET | http://13.60.31.141:500/support/api/SupportRequests/{requestId:int}/timeline |
| PUT | http://13.60.31.141:500/support/api/SupportRequests/{requestId:int}/assign/dispatcher/{dispatcherId:guid} |
| PUT | http://13.60.31.141:500/support/api/SupportRequests/{requestId:int}/cancel/member/{memberId:guid} |
| PUT | http://13.60.31.141:500/support/api/SupportRequests/{requestId:int}/complete/staff/{staffId:guid} |
| PUT | http://13.60.31.141:500/support/api/SupportRequests/{requestId:int}/dispatcher-instructions/dispatcher/{dispatcherId:guid} |
| PUT | http://13.60.31.141:500/support/api/SupportRequests/{requestId:int}/in-progress/staff/{staffId:guid} |
| PUT | http://13.60.31.141:500/support/api/SupportRequests/{requestId:int}/reassign/dispatcher/{dispatcherId:guid} |
| PUT | http://13.60.31.141:500/support/api/SupportRequests/{requestId:int}/start/staff/{staffId:guid} |
| GET | http://13.60.31.141:500/support/api/SupportRequests/member/{memberId:guid} |
| POST | http://13.60.31.141:500/support/api/SupportRequests/member/{memberId:guid} |
| GET | http://13.60.31.141:500/support/api/SupportRequests/staff/{staffId:guid} |
| POST | http://13.60.31.141:500/support/api/SupportRequests/uploads |
| POST | http://13.60.31.141:500/support/api/SupportStaffStatuses |
| POST | http://13.60.31.141:500/support/api/SupportStaffStatuses/internal/provision |
| GET | http://13.60.31.141:500/support/api/SupportStaffStatuses/member/{memberId:guid} |
| PUT | http://13.60.31.141:500/support/api/SupportStaffStatuses/member/{memberId:guid} |
| GET | http://13.60.31.141:500/support/api/SupportStaffStatuses/status/{status} |

### Notification Service

Notification list/detail/delete + direct email send.

| Method | URL |
|---|---|
| GET | http://13.60.31.141:500/notification/api/v1/notifications |
| GET | http://13.60.31.141:500/notification/api/v1/notifications/me |
| DELETE | http://13.60.31.141:500/notification/api/v1/notifications/{id:guid} |
| GET | http://13.60.31.141:500/notification/api/v1/notifications/{id:guid} |
| POST | http://13.60.31.141:500/notification/api/v1/notifications/email |


