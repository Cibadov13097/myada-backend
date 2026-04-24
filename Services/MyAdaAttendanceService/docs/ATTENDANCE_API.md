# MyAdaAttendanceService API Documentation

This document describes the full MyAdaAttendanceService HTTP API exposed through your API Gateway.

## 1. Base URL and Routing

- Public base URL: `http://13.60.31.141:5000/attendance`
- Full endpoint format: `http://13.60.31.141:5000/attendance/api/...`
- Internal Docker destination (via gateway): `http://myadaattendanceservice:8080/`

Gateway route configuration removes `/attendance` and forwards the remaining path to the service.

## 2. Swagger

Swagger is enabled only when the attendance service runs in `Development` environment.

- UI (if enabled): `http://13.60.31.141:5000/attendance/swagger/index.html`
- OpenAPI JSON (if enabled): `http://13.60.31.141:5000/attendance/swagger/v1/swagger.json`

## 3. Authentication and Authorization Notes

- Send bearer token header in requests:
  - `Authorization: Bearer <token>`
- Current implementation has route-user checks as no-op (`EnsureRouteUserMatchesClaim` and `EnsureAuthenticatedUserMatches` do not enforce identity yet).
- Even with disabled checks, keep sending correct IDs and token for future compatibility.

## 4. Error and Status Behavior

Controller base maps known exceptions to these HTTP responses:

- `400 Bad Request`: `ArgumentException`, `InvalidOperationException`
- `401 Unauthorized`: `UnauthorizedAccessException`
- `404 Not Found`: `KeyNotFoundException`
- `200 OK`: standard read/update responses
- `201 Created`: specific create endpoints (course, lesson, enrollment, session)
- `204 No Content`: void action endpoints (finalize, bulk absent, deletes)

Typical error body from controller-level handling:

```json
{
  "message": "Human readable error message"
}
```

## 5. Data Formats and Enums

### 5.1 Date/Time

- `DateTime`: ISO-8601 string (`2026-04-24T10:30:00Z`)
- `DateOnly`: `YYYY-MM-DD`
- `TimeOnly`: `HH:mm:ss`

### 5.2 Common enums

- `AcademicSemester`: `Fall`, `Spring`, `Summer`
- `AttendanceStatus`: `Present`, `Late`, `Absent`, `Excused`
- `AttendanceMarkedSource`: `QR`, `Manual`
- `DayOfWeek` for bulk session slots: `Sunday`..`Saturday` (or numeric enum values)

## 6. Endpoint Catalog

All paths below are relative to `http://13.60.31.141:5000/attendance`.

### 6.1 Admin: User Directory

#### POST `/api/admin/users/sync`

Sync users from Auth service by roles.

- Query:
  - `roles` (optional, repeatable), example: `?roles=student&roles=instructor`
  - Default when omitted: `student`, `instructor`
- Response `200`: `UserSyncResultDto`

#### GET `/api/admin/users/roles/{role}`

Get locally synced users by role.

- Response `200`: `ExternalUserDirectoryDto[]`

### 6.2 Admin: Courses

#### GET `/api/admin/courses`

List all courses ordered by department/code.

- Response `200`: `CourseDto[]`

#### GET `/api/admin/courses/{courseId}`

Get single course.

- Response `200`: `CourseDto`
- Response `404`: course not found

#### POST `/api/admin/courses`

Create course.

- Body: `CreateCourseDto`
- Response `201`: `CourseDto`
- Validation:
  - name, department, code required
  - credits >= 0
  - timesPerWeek >= 0
  - department+code must be unique

#### PUT `/api/admin/courses/{courseId}`

Update course.

- Body: `UpdateCourseDto`
- Response `200`: `CourseDto`
- Validation same as create

#### DELETE `/api/admin/courses/{courseId}`

Delete course.

- Response `204`
- Response `400`: if course still has lessons

### 6.3 Admin: Lessons

#### GET `/api/admin/courses/{courseId}/lessons`

List lessons under a course.

- Response `200`: `LessonDto[]`

#### GET `/api/admin/instructors/{instructorId}/lessons`

List lessons for a specific instructor.

- Response `200`: `LessonDto[]`

#### GET `/api/admin/lessons`

List all lessons.

- Response `200`: `LessonDto[]`

#### GET `/api/admin/lessons/by-term?academicYear={year}&semester={semester}`

Filter lessons by academic term.

- Example: `/api/admin/lessons/by-term?academicYear=2026&semester=Spring`
- Response `200`: `LessonDto[]`
- Validation:
  - academicYear between 2000 and 2100
  - semester must be Fall/Spring/Summer

#### GET `/api/admin/lessons/scheduling`

Returns lesson scheduling rows for external scheduling logic.

- Response `200`: `LessonSchedulingRow[]`

#### GET `/api/admin/lessons/{lessonId}`

Get lesson details including session short list.

- Response `200`: `LessonDto`
- Response `404`: not found

#### POST `/api/admin/lessons`

Create lesson.

- Body: `CreateLessonDto`
- Response `201`: `LessonDto`
- Validation:
  - courseId > 0
  - instructorId required
  - academicYear between 2000..2100
  - semester valid
  - maxCapacity >= 0

#### PUT `/api/admin/lessons/{lessonId}`

Update lesson.

- Body: `UpdateLessonDto`
- Response `200`: `LessonDto`

#### DELETE `/api/admin/lessons/{lessonId}`

Delete lesson.

- Response `204`

### 6.4 Admin: Enrollments

#### GET `/api/admin/lessons/{lessonId}/enrollments`

List enrollments for lesson.

- Response `200`: `EnrollmentDto[]`

#### POST `/api/admin/lessons/{lessonId}/enrollments`

Create enrollment.

- Body: `CreateEnrollmentDto`
- Response `201`: `EnrollmentDto`
- Validation:
  - studentId required
  - student cannot already be enrolled

### 6.5 Admin: Sessions

#### GET `/api/admin/lessons/{lessonId}/sessions`

List lesson sessions with attendance rollups.

- Response `200`: `SessionDto[]`

#### POST `/api/admin/lessons/{lessonId}/sessions/generate`

Bulk-generate sessions by weekly pattern.

- Body: `BulkGenerateSessionsDto`
- Response `200`: `BulkGenerateSessionsResponseDto`
- Validation:
  - fromDate <= toDate
  - at least one weekly slot
  - each slot startTime < endTime
  - max date span 731 days
  - max generated sessions per request 2000

### 6.6 Admin: Attendance Corrections

#### GET `/api/admin/sessions/{sessionId}/attendance`

Get attendance rows for a session.

- Response `200`: `AttendanceDto[]`

#### PATCH `/api/admin/sessions/{sessionId}/attendance/{attendanceId}`

Manually correct attendance record by attendance row ID.

- Body: `AdminAttendanceCorrectionDto`
- Response `200`: `AttendanceDto`
- Validation:
  - status must be one of Present/Late/Absent/Excused

#### DELETE `/api/admin/sessions/{sessionId}/attendance/{attendanceId}`

Delete attendance record.

- Response `204`

### 6.7 Instructor: Lessons and Sessions

Base: `/api/instructors/{instructorId}/lessons`

#### GET `/api/instructors/{instructorId}/lessons`

- Response `200`: `LessonDto[]`

#### GET `/api/instructors/{instructorId}/lessons/{lessonId}`

- Response `200`: `LessonDto`

#### GET `/api/instructors/{instructorId}/lessons/{lessonId}/sessions`

- Response `200`: `SessionDto[]`

#### GET `/api/instructors/{instructorId}/lessons/{lessonId}/sessions/{sessionId}`

- Response `200`: `SessionDto`

#### POST `/api/instructors/{instructorId}/lessons/{lessonId}/sessions`

Create a single session.

- Body: `CreateSessionDto`
- Response `201`: `SessionDto`

#### POST `/api/instructors/{instructorId}/lessons/{lessonId}/sessions/generate`

Bulk-generate sessions (instructor-owned lesson only).

- Body: `BulkGenerateSessionsDto`
- Response `200`: `BulkGenerateSessionsResponseDto`

#### PATCH `/api/instructors/{instructorId}/lessons/{lessonId}/sessions/{sessionId}`

Update session start/end/topic.

- Body: `UpdateSessionDto`
- Response `200`: `SessionDto`

#### DELETE `/api/instructors/{instructorId}/lessons/{lessonId}/sessions/{sessionId}`

- Response `204`

### 6.8 Instructor: Attendance and QR

Base: `/api/instructors/{instructorId}/sessions`

#### POST `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/activate/{round}`

Activate attendance round.

- `round` must be `1` or `2`
- Rules:
  - round 1 can be activated once
  - round 2 requires closed round 1
  - only one active round at a time
  - session `roundNo` is set to the activated round value (`1` or `2`)
- Response `200`: `AttendanceActivationResultDto`

#### POST `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/deactivate/{round}`

Deactivate active round.

- `round` must match currently active round
- Response `200`: `AttendanceActivationResultDto`

#### POST `/api/instructors/{instructorId}/sessions/{sessionId}/qr-token`

Issue QR JWT for currently active round.

- Response `200`: `QrTokenResponseDto`
- Important:
  - token contains claims: `sid`, `aid`, `rnd`, `ins`, `jti`
  - default token TTL now `120` seconds (`QrToken:LifetimeSeconds` can override)

#### GET `/api/instructors/{instructorId}/sessions/{sessionId}/attendance`

Get session attendance roster.

- Response `200`: `AttendanceDto[]`

#### GET `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/summary`

Get attendance summary counts.

- Response `200`: `AttendanceSummaryDto`

#### PATCH `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/{studentId}`

Manual attendance update for one student.

- Body: `UpdateAttendanceDto`
- Response `200`: `AttendanceDto`

#### POST `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/finalize`

Compute final attendance from accepted scan rounds for all enrolled students.

- Requires no active round
- Response `204`
- Logic:
  - if round 2 was activated:
    - 2 distinct rounds scanned -> Present
    - 1 distinct round scanned -> Late
  - if round 2 was never activated:
    - 1+ distinct scanned rounds -> Present
  - 0 rounds scanned -> Absent
  - manually adjusted records are not overwritten

#### POST `/api/instructors/{instructorId}/sessions/{sessionId}/attendance/bulk-absent`

Add absent records for enrolled students without attendance rows.

- Response `204`

### 6.9 Student Endpoints

Base: `/api/students/{studentId}`

#### GET `/api/students/{studentId}/enrollments`

Returns student lessons with aggregated attendance counts.

- Response `200`: `StudentLessonDto[]`

#### GET `/api/students/{studentId}/lessons/{lessonId}/attendance`

Returns per-session attendance rows for the lesson.

- Response `200`: `StudentAttendanceDto[]`

#### POST `/api/students/{studentId}/attendance/scan`

Submit QR scan.

- Body: `QrScanRequestDto`
- Response `200`: `QrScanResponseDto`

#### POST `/api/students/{studentId}/attendance/qr/scan`

Alias for `/attendance/scan`.

- Body/Response identical

## 7. QR Scan Behavior Details

### 7.1 Request token format

`token` accepts:

1. Plain JWT token
2. JWT bound to student ID using pipe suffix:
   - `{jwt}|{studentGuid}`

If bound student GUID does not match request student, request is rejected.

### 7.2 Scan acceptance checks

Scan is accepted only if all are true:

- valid signed JWT
- token not expired
- session exists
- token instructor matches session owner
- activation exists and is currently active with same round
- student is enrolled in session lesson
- student has not already scanned in same round
- token `jti` not replayed for same student/session

### 7.3 Scan status behavior

Computed status depends on whether round 2 was ever activated for the session:

- if round 2 exists:
  - one scanned round -> `Late`
  - two scanned rounds -> `Present`
- if round 2 does not exist:
  - one scanned round -> `Present`

### 7.4 QR scan error codes (`QrScanResponseDto.errorCode`)

- `student_id_missing`
- `token_missing`
- `student_token_mismatch`
- `invalid_token`
- `invalid_token_format`
- `invalid_token_signature`
- `invalid_token_payload`
- `token_expired`
- `session_not_found`
- `instructor_mismatch`
- `activation_inactive`
- `already_scanned_this_round`
- `student_not_enrolled`
- `replay_token`

## 8. Request and Response Schemas

### 8.1 Course

`CreateCourseDto` and `UpdateCourseDto`

```json
{
  "name": "Linear Algebra",
  "department": "MATH",
  "code": "201",
  "credits": 4,
  "timesPerWeek": 2
}
```

`CourseDto`

```json
{
  "id": 12,
  "name": "Linear Algebra",
  "department": "MATH",
  "code": "201",
  "credits": 4,
  "timesPerWeek": 2
}
```

### 8.2 Lesson

`CreateLessonDto`

```json
{
  "courseId": 12,
  "instructorId": "11111111-1111-1111-1111-111111111111",
  "roomId": 401,
  "academicYear": 2026,
  "semester": "Spring",
  "maxCapacity": 35
}
```

`LessonDto` (example)

```json
{
  "id": 55,
  "name": "Linear Algebra",
  "code": "201",
  "instructorId": "11111111-1111-1111-1111-111111111111",
  "instructorDisplayName": "Ada Instructor",
  "instructorEmail": "instructor@example.com",
  "academicYear": 2026,
  "semester": "Spring",
  "crn": "20001",
  "maxCapacity": 35,
  "sessions": []
}
```

### 8.3 Enrollment

`CreateEnrollmentDto`

```json
{
  "studentId": "22222222-2222-2222-2222-222222222222"
}
```

`EnrollmentDto`

```json
{
  "id": 99,
  "lessonId": 55,
  "studentId": "22222222-2222-2222-2222-222222222222",
  "studentFullName": "Student One",
  "studentCode": "S12345",
  "studentEmail": "student@example.com"
}
```

### 8.4 Session

`CreateSessionDto` / `UpdateSessionDto`

```json
{
  "startTime": "2026-05-01T09:00:00Z",
  "endTime": "2026-05-01T10:30:00Z",
  "topic": "Matrices"
}
```

`BulkGenerateSessionsDto`

```json
{
  "fromDate": "2026-05-01",
  "toDate": "2026-06-30",
  "weeklySlots": [
    {
      "dayOfWeek": "Monday",
      "startTime": "09:00:00",
      "endTime": "10:30:00"
    },
    {
      "dayOfWeek": "Wednesday",
      "startTime": "09:00:00",
      "endTime": "10:30:00"
    }
  ],
  "topic": "Regular Lecture"
}
```

`BulkGenerateSessionsResponseDto`

```json
{
  "createdCount": 12,
  "skippedDuplicateCount": 1,
  "createdSessions": []
}
```

`SessionDto` (example)

```json
{
  "id": 700,
  "lessonId": 55,
  "lessonName": "Linear Algebra",
  "startTime": "2026-05-01T09:00:00Z",
  "endTime": "2026-05-01T10:30:00Z",
  "topic": "Matrices",
  "isAttendanceActive": false,
  "roundNo": 0,
  "attendanceActivatedAt": null,
  "attendanceDeactivatedAt": null,
  "totalStudents": 32,
  "presentCount": 25,
  "lateCount": 3,
  "absentCount": 4,
  "excusedCount": 0
}
```

### 8.5 Attendance

`UpdateAttendanceDto`

```json
{
  "status": "Present",
  "instructorNote": "Came late but attended full class"
}
```

`AdminAttendanceCorrectionDto`

```json
{
  "status": "Excused",
  "note": "Medical note accepted"
}
```

`AttendanceDto` (example)

```json
{
  "id": 301,
  "sessionId": 700,
  "lessonId": 55,
  "studentId": "22222222-2222-2222-2222-222222222222",
  "studentFullName": "Student One",
  "studentCode": "S12345",
  "status": "Late",
  "markedAt": "2026-05-01T09:05:30Z",
  "markedSource": "QR",
  "updatedAt": "2026-05-01T09:05:30Z",
  "updatedBy": "22222222-2222-2222-2222-222222222222",
  "firstScanAt": "2026-05-01T09:05:30Z",
  "lastScanAt": "2026-05-01T09:05:30Z",
  "isManuallyAdjusted": false,
  "instructorNote": null
}
```

`AttendanceSummaryDto`

```json
{
  "sessionId": 700,
  "totalStudents": 32,
  "presentCount": 25,
  "lateCount": 3,
  "absentCount": 4,
  "excusedCount": 0
}
```

### 8.6 QR

`QrTokenResponseDto`

```json
{
  "sessionId": 700,
  "activationId": 41,
  "round": 1,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-05-01T09:12:00Z"
}
```

`QrScanRequestDto`

```json
{
  "studentId": "22222222-2222-2222-2222-222222222222",
  "token": "<jwt-or-jwt|studentGuid>"
}
```

`QrScanResponseDto` success

```json
{
  "success": true,
  "errorCode": null,
  "message": "Scan accepted.",
  "studentId": "22222222-2222-2222-2222-222222222222",
  "sessionId": 700,
  "activationId": 41,
  "round": 1,
  "validScanCount": 1,
  "status": "Late",
  "scannedAt": "2026-05-01T09:05:30Z"
}
```

`QrScanResponseDto` failure

```json
{
  "success": false,
  "errorCode": "token_expired",
  "message": "QR token has expired.",
  "studentId": "22222222-2222-2222-2222-222222222222",
  "sessionId": 700,
  "activationId": 41,
  "round": 1,
  "validScanCount": 0,
  "status": null,
  "scannedAt": "2026-05-01T09:10:00Z"
}
```

### 8.7 Student projections

`StudentLessonDto`

```json
{
  "lessonId": 55,
  "lessonName": "Linear Algebra",
  "lessonCode": "201",
  "totalSessions": 14,
  "presentCount": 10,
  "lateCount": 2,
  "absentCount": 2,
  "excusedCount": 0
}
```

`StudentAttendanceDto`

```json
{
  "attendanceId": 301,
  "sessionId": 700,
  "sessionStartTime": "2026-05-01T09:00:00Z",
  "sessionEndTime": "2026-05-01T10:30:00Z",
  "lessonName": "Linear Algebra",
  "lessonCode": "201",
  "status": "Late",
  "firstScanAt": "2026-05-01T09:05:30Z",
  "lastScanAt": "2026-05-01T09:05:30Z",
  "isManuallyAdjusted": false,
  "instructorNote": null
}
```

### 8.8 User sync models

`UserSyncResultDto`

```json
{
  "importedCount": 340,
  "roles": ["student", "instructor"],
  "syncedAtUtc": "2026-04-24T09:00:00Z"
}
```

`ExternalUserDirectoryDto`

```json
{
  "userId": "11111111-1111-1111-1111-111111111111",
  "role": "instructor",
  "userName": "i12345",
  "email": "instructor@example.com",
  "firstName": "Ada",
  "lastName": "Lovelace",
  "displayName": "Ada Lovelace",
  "phoneNumber": "+123456789",
  "userType": "Instructor",
  "status": "Active",
  "syncedAtUtc": "2026-04-24T09:00:00Z"
}
```

### 8.9 Scheduling row

`LessonSchedulingRow`

```json
{
  "lessonId": 55,
  "instructorUserId": "11111111-1111-1111-1111-111111111111",
  "enrollment": 32,
  "maxCapacity": 35,
  "timesPerWeek": 2,
  "courseCode": "MATH201",
  "courseTitle": "Linear Algebra"
}
```

## 9. End-to-End Attendance Flow (Recommended)

1. Admin creates course and lesson.
2. Admin enrolls students.
3. Instructor creates or generates sessions.
4. Instructor starts round 1:
   - `POST /api/instructors/{instructorId}/sessions/{sessionId}/attendance/activate/1`
5. Instructor requests QR token repeatedly as it expires (~120s default).
6. Students scan using:
   - `POST /api/students/{studentId}/attendance/scan`
7. Instructor closes round 1 and opens round 2 (optional):
   - deactivate 1 -> activate 2 -> scans -> deactivate 2
8. Instructor finalizes:
   - `POST /api/instructors/{instructorId}/sessions/{sessionId}/attendance/finalize`
   - if round 2 was skipped, a valid round-1 scan finalizes as `Present`
9. Instructor/Admin can manually correct records after finalization.

## 10. Quick cURL Examples (Gateway URL)

### Issue QR token

```bash
curl -X POST "http://13.60.31.141:5000/attendance/api/instructors/{instructorId}/sessions/{sessionId}/qr-token" \
  -H "Authorization: Bearer <token>"
```

### Student scan

```bash
curl -X POST "http://13.60.31.141:5000/attendance/api/students/{studentId}/attendance/scan" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <token>" \
  -d '{
    "studentId":"{studentId}",
    "token":"<jwt>"
  }'
```

### Get instructor attendance summary

```bash
curl "http://13.60.31.141:5000/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/summary" \
  -H "Authorization: Bearer <token>"
```

---

Source alignment: generated from current code in `MyAdaAttendanceService.Web/Controllers`, `MyAdaAttendanceService.Application/DTOs`, and service logic files.
