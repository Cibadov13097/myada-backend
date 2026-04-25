# MyAda Attendance Service API Documentation

Last updated: 2026-04-25

## Base URL

- Gateway base URL: `http://13.60.31.141:500/`
- Attendance gateway prefix: `/attendance`
- Effective attendance base: `http://13.60.31.141:500/attendance`

Swagger:
- Gateway: `http://13.60.31.141:500/attendance/swagger/index.html`
- Direct service (if exposed): `http://13.60.31.141:5009/swagger/index.html`

## Auth Header

Protected endpoints require:

```http
Authorization: Bearer <accessToken>
```

## Route Rule

Always include `/attendance` when calling through gateway.

Example:
- Correct: `http://13.60.31.141:500/attendance/api/admin/courses`
- Wrong: `http://13.60.31.141:500/api/admin/courses`

## Endpoint Groups

## 1) Admin APIs

Base path: `/attendance/api/admin`

### Users

| Method | Path | Description |
|---|---|---|
| `POST` | `/attendance/api/admin/users/sync` | Sync users from Auth service into local directory. |
| `GET` | `/attendance/api/admin/users/roles/{role}` | List synced users by role. |

### Courses

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/admin/courses` | List all courses. |
| `GET` | `/attendance/api/admin/courses/{courseId}` | Get one course by id. |
| `POST` | `/attendance/api/admin/courses` | Create course. |
| `PUT` | `/attendance/api/admin/courses/{courseId}` | Update course. |
| `DELETE` | `/attendance/api/admin/courses/{courseId}` | Delete course. |
| `GET` | `/attendance/api/admin/courses/{courseId}/lessons` | List lessons of a course. |

### Lessons

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/admin/lessons` | List all lessons. |
| `GET` | `/attendance/api/admin/lessons/by-term?academicYear={year}&semester={semester}` | List lessons by term (`Fall`, `Spring`, `Summer`). |
| `GET` | `/attendance/api/admin/lessons/scheduling` | Scheduling dataset (lesson, instructor, enrollment, etc.). |
| `GET` | `/attendance/api/admin/lessons/{lessonId}` | Get lesson detail. |
| `POST` | `/attendance/api/admin/lessons` | Create lesson. |
| `PUT` | `/attendance/api/admin/lessons/{lessonId}` | Update lesson. |
| `DELETE` | `/attendance/api/admin/lessons/{lessonId}` | Delete lesson. |
| `GET` | `/attendance/api/admin/instructors/{instructorId}/lessons` | List lessons assigned to instructor. |

### Enrollments

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/admin/lessons/{lessonId}/enrollments` | List enrollments by lesson. |
| `POST` | `/attendance/api/admin/lessons/{lessonId}/enrollments` | Add one student enrollment to lesson. |

### Sessions and Attendance

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/admin/lessons/{lessonId}/sessions` | List sessions of a lesson. |
| `POST` | `/attendance/api/admin/lessons/{lessonId}/sessions/generate` | Bulk-generate sessions from weekly slots/date range. |
| `GET` | `/attendance/api/admin/sessions/{sessionId}/attendance` | Get full attendance roster for session. |
| `PATCH` | `/attendance/api/admin/sessions/{sessionId}/attendance/{attendanceId}` | Correct one attendance row. |
| `DELETE` | `/attendance/api/admin/sessions/{sessionId}/attendance/{attendanceId}` | Delete one attendance row. |

## 2) Instructor Lesson APIs

Base path: `/attendance/api/instructors/{instructorId}/lessons`

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/instructors/{instructorId}/lessons` | Get instructor lessons. |
| `GET` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}` | Get one instructor lesson. |
| `GET` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}/sessions` | List sessions of instructor lesson. |
| `GET` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}/sessions/{sessionId}` | Get one session. |
| `POST` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}/sessions` | Create one session. |
| `POST` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}/sessions/generate` | Bulk-generate sessions. |
| `PATCH` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}/sessions/{sessionId}` | Update one session. |
| `DELETE` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}/sessions/{sessionId}` | Delete one session. |

## 3) Instructor Attendance APIs

Base path: `/attendance/api/instructors/{instructorId}/sessions`

| Method | Path | Description |
|---|---|---|
| `POST` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/activate/{round}` | Open attendance round (`round` = `1` or `2`) and update session `roundNo`. |
| `POST` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/deactivate/{round}` | Close the currently active attendance round. |
| `POST` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/qr-token` | Issue QR token for check-in. |
| `GET` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance` | Get full attendance roster. |
| `GET` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/summary` | Get attendance summary counts. |
| `PATCH` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/{studentId}` | Update one student's status. |
| `POST` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/finalize` | Finalize attendance for session. |
| `POST` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/bulk-absent` | Mark remaining students absent in bulk. |

## 4) Student APIs

Base path: `/attendance/api/students/{studentId}`

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/students/{studentId}/enrollments` | Get student's enrolled lessons and attendance aggregates. |
| `GET` | `/attendance/api/students/{studentId}/lessons/{lessonId}/attendance` | Get student's per-session attendance in lesson. |
| `POST` | `/attendance/api/students/{studentId}/attendance/scan` | Submit QR attendance scan. |
| `POST` | `/attendance/api/students/{studentId}/attendance/qr/scan` | Submit QR attendance scan (alternate route). |

## Key Request Bodies

### Create Course (`POST /attendance/api/admin/courses`)

```json
{
  "name": "Calculus I",
  "department": "Math",
  "code": "MATH101",
  "credits": 3,
  "timesPerWeek": 2
}
```

### Create Lesson (`POST /attendance/api/admin/lessons`)

```json
{
  "courseId": 1,
  "instructorId": "00000000-0000-0000-0000-000000000000",
  "roomId": 101,
  "academicYear": 2026,
  "semester": "Spring",
  "maxCapacity": 40
}
```

### Create Enrollment (`POST /attendance/api/admin/lessons/{lessonId}/enrollments`)

```json
{
  "studentId": "00000000-0000-0000-0000-000000000000"
}
```

### Generate Sessions (`POST /attendance/api/admin/lessons/{lessonId}/sessions/generate`)

```json
{
  "fromDate": "2026-09-15",
  "toDate": "2026-12-20",
  "weeklySlots": [
    {
      "dayOfWeek": "Monday",
      "startTime": "09:00:00",
      "endTime": "10:30:00"
    }
  ],
  "topic": "Lecture"
}
```

### Update Attendance (`PATCH /attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/{studentId}`)

```json
{
  "status": "Present",
  "instructorNote": "Arrived on time"
}
```

### Student QR Scan (`POST /attendance/api/students/{studentId}/attendance/qr/scan`)

```json
{
  "studentId": "00000000-0000-0000-0000-000000000000",
  "token": "<qr-token>",
  "deviceInfo": "iPhone 14 / iOS",
  "qrContext": {
    "sessionId": 123
  }
}
```

## Attendance Round Behavior

- `LessonSessions` now stores `roundNo` for each session.
- Default `roundNo` is `0`.
- When round 1 is activated, `roundNo` becomes `1`.
- When round 2 is activated, `roundNo` becomes `2`.
- Session APIs return `roundNo` in both session list/detail payloads so frontend can resume from the last active/completed round state.

## Finalization / Status Rules

- If round 2 was ever activated for the session:
  - `2` distinct rounds scanned -> `Present`
  - `1` distinct round scanned -> `Late`
  - `0` scan -> `Absent`
- If round 2 was never activated:
  - `1` (or more) distinct scanned round -> `Present`
  - `0` scan -> `Absent`
- Manually adjusted attendance rows are not overwritten during finalize.

## Attendance Status Values

- `Present`
- `Late`
- `Absent`
- `Excused`

## Notes

- Through gateway, every attendance-service endpoint must start with `/attendance`.
- The service uses enum strings in JSON (for example semester: `Fall`, `Spring`, `Summer`).
- Runtime response body may include API wrapper envelope depending on service configuration.
