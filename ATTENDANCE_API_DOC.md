# MyAda Attendance Service API Documentation

Last updated: 2026-09-12

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

## List sorting, search, and filtering

Every collection GET (list of courses, lessons, sessions, enrollments, attendance rows, users, etc.) accepts optional sort query params. Nested collections such as `sessions` on a lesson detail payload are not sortable.

| Query | Required | Description |
|---|---|---|
| `sortBy` | no | Scalar field name on the response item (`id`, `name`, `department`, `status`, …). Case-insensitive. Omit to keep the default order. |
| `sortDir` | no | `asc` (default) or `desc`. Aliases: `ascending`, `descending`. |

Unknown `sortBy` or invalid `sortDir` returns `400` with `{ "message": "..." }` listing allowed fields.

Each collection also has dedicated search and filter routes on the same path:

- `GET {list-path}/search?q={term}` — case-insensitive contains match across all scalar fields. `search` is an alias of `q`. Also accepts `sortBy` / `sortDir`.
- `GET {list-path}/filter?{field}={value}` — AND of provided fields. Strings use case-insensitive contains; numbers, bools, guids, and enums use exact match. A `DateTime` filter with time `00:00:00` matches that calendar date. Also accepts `sortBy` / `sortDir`.

Examples:

```http
GET /attendance/api/admin/courses?sortBy=department&sortDir=asc
GET /attendance/api/admin/courses/search?q=math&sortBy=code
GET /attendance/api/admin/courses/filter?department=Math&credits=3&sortBy=code&sortDir=desc
GET /attendance/api/admin/lessons/by-term/search?academicYear=2026&semester=Spring&q=algo
```

## Endpoint Groups

## 1) Admin APIs

Base path: `/attendance/api/admin`

### Users

| Method | Path | Description |
|---|---|---|
| `POST` | `/attendance/api/admin/users/sync` | Sync users from Auth service into local directory. |
| `GET` | `/attendance/api/admin/users/roles/{role}` | List synced users by role. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/users/roles/{role}/search` | Search users by role (`q` / `search`). |
| `GET` | `/attendance/api/admin/users/roles/{role}/filter` | Filter users by role on user fields. |

### Courses

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/admin/courses` | List all courses. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/courses/search` | Search courses (`q` / `search`). |
| `GET` | `/attendance/api/admin/courses/filter` | Filter courses by course fields. |
| `GET` | `/attendance/api/admin/courses/{courseId}` | Get one course by id. |
| `POST` | `/attendance/api/admin/courses` | Create course. |
| `PUT` | `/attendance/api/admin/courses/{courseId}` | Update course. |
| `DELETE` | `/attendance/api/admin/courses/{courseId}` | Delete course. |
| `GET` | `/attendance/api/admin/courses/{courseId}/lessons` | List lessons of a course. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/courses/{courseId}/lessons/search` | Search lessons of a course. |
| `GET` | `/attendance/api/admin/courses/{courseId}/lessons/filter` | Filter lessons of a course. |
| `POST` | `/attendance/api/admin/courses/{courseId}/lessons/generate/preview` | Suggest lesson sections from student count, section count, and instructor slots. Does not create. |
| `POST` | `/attendance/api/admin/courses/{courseId}/lessons/generate` | Bulk-create the confirmed (optionally edited) suggested sections. |

### Lessons

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/admin/lessons` | List all lessons. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/lessons/search` | Search all lessons (`q` / `search`). |
| `GET` | `/attendance/api/admin/lessons/filter` | Filter all lessons by lesson fields. |
| `GET` | `/attendance/api/admin/lessons/by-term?academicYear={year}&semester={semester}` | List lessons by term (`Fall`, `Spring`, `Summer`). Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/lessons/by-term/search` | Search lessons in a term (`academicYear`, `semester`, `q`). |
| `GET` | `/attendance/api/admin/lessons/by-term/filter` | Filter lessons in a term. |
| `GET` | `/attendance/api/admin/lessons/scheduling` | Scheduling dataset (lesson, instructor, enrollment, etc.). Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/lessons/scheduling/search` | Search scheduling rows in a term. |
| `GET` | `/attendance/api/admin/lessons/scheduling/filter` | Filter scheduling rows in a term. |
| `GET` | `/attendance/api/admin/lessons/{lessonId}` | Get lesson detail. |
| `POST` | `/attendance/api/admin/lessons` | Create lesson. |
| `GET` | `/attendance/api/admin/lessons/bulk/template` | Download Excel template for bulk lesson create. |
| `POST` | `/attendance/api/admin/lessons/bulk` | Upload filled Excel template to create lessons in bulk. |
| `PUT` | `/attendance/api/admin/lessons/{lessonId}` | Update lesson. |
| `DELETE` | `/attendance/api/admin/lessons/{lessonId}` | Delete lesson. |
| `GET` | `/attendance/api/admin/instructors/{instructorId}/lessons` | List lessons assigned to instructor. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/instructors/{instructorId}/lessons/search` | Search instructor lessons. |
| `GET` | `/attendance/api/admin/instructors/{instructorId}/lessons/filter` | Filter instructor lessons. |

### Enrollments

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/admin/lessons/{lessonId}/enrollments` | List enrollments by lesson. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/lessons/{lessonId}/enrollments/search` | Search enrollments of a lesson. |
| `GET` | `/attendance/api/admin/lessons/{lessonId}/enrollments/filter` | Filter enrollments of a lesson. |
| `POST` | `/attendance/api/admin/lessons/{lessonId}/enrollments` | Add one student enrollment to lesson. |

### Sessions and Attendance

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/admin/lessons/{lessonId}/sessions` | List sessions of a lesson. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/lessons/{lessonId}/sessions/search` | Search sessions of a lesson. |
| `GET` | `/attendance/api/admin/lessons/{lessonId}/sessions/filter` | Filter sessions of a lesson. |
| `POST` | `/attendance/api/admin/lessons/{lessonId}/sessions/generate` | Bulk-generate sessions from weekly slots/date range. |
| `GET` | `/attendance/api/admin/sessions/{sessionId}/attendance` | Get full attendance roster for session. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/admin/sessions/{sessionId}/attendance/search` | Search attendance rows of a session. |
| `GET` | `/attendance/api/admin/sessions/{sessionId}/attendance/filter` | Filter attendance rows of a session. |
| `PATCH` | `/attendance/api/admin/sessions/{sessionId}/attendance/{attendanceId}` | Correct one attendance row. |
| `DELETE` | `/attendance/api/admin/sessions/{sessionId}/attendance/{attendanceId}` | Delete one attendance row. |

## 2) Instructor Lesson APIs

Base path: `/attendance/api/instructors/{instructorId}/lessons`

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/instructors/{instructorId}/lessons` | Get instructor lessons. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/instructors/{instructorId}/lessons/search` | Search instructor lessons. |
| `GET` | `/attendance/api/instructors/{instructorId}/lessons/filter` | Filter instructor lessons. |
| `GET` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}` | Get one instructor lesson. |
| `GET` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}/sessions` | List sessions of instructor lesson. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}/sessions/search` | Search sessions of instructor lesson. |
| `GET` | `/attendance/api/instructors/{instructorId}/lessons/{lessonId}/sessions/filter` | Filter sessions of instructor lesson. |
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
| `GET` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance` | Get full attendance roster. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/search` | Search attendance rows of a session. |
| `GET` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/filter` | Filter attendance rows of a session. |
| `GET` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/summary` | Get attendance summary counts. |
| `PATCH` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/{studentId}` | Update one student's status. |
| `POST` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/finalize` | Finalize attendance for session. |
| `POST` | `/attendance/api/instructors/{instructorId}/sessions/{sessionId}/attendance/bulk-absent` | Mark remaining students absent in bulk. |

## 4) Student APIs

Base path: `/attendance/api/students/{studentId}`

| Method | Path | Description |
|---|---|---|
| `GET` | `/attendance/api/students/{studentId}/enrollments` | Get student's enrolled lessons and attendance aggregates. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/students/{studentId}/enrollments/search` | Search student enrollments. |
| `GET` | `/attendance/api/students/{studentId}/enrollments/filter` | Filter student enrollments. |
| `GET` | `/attendance/api/students/{studentId}/lessons/{lessonId}/attendance` | Get student's per-session attendance in lesson. Optional `sortBy` / `sortDir`. |
| `GET` | `/attendance/api/students/{studentId}/lessons/{lessonId}/attendance/search` | Search student's attendance rows in a lesson. |
| `GET` | `/attendance/api/students/{studentId}/lessons/{lessonId}/attendance/filter` | Filter student's attendance rows in a lesson. |
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

`instructorId` and `roomId` are optional (`null` = unassigned). Empty GUID is rejected for `instructorId`.

```json
{
  "courseId": 1,
  "instructorId": null,
  "roomId": null,
  "academicYear": 2026,
  "semester": "Spring",
  "maxCapacity": 40
}
```

### Bulk Create Lessons from Excel

Dedicated documentation: `Services/MyAdaAttendanceService/docs/LESSON_BULK_EXCEL.md`

1. Download template: `GET /attendance/api/admin/lessons/bulk/template` (returns `.xlsx`, not JSON).
2. Fill the `Lessons` sheet (`CourseId`, `InstructorId`, `RoomId`, `AcademicYear`, `Semester`, `MaxCapacity`). Leave `InstructorId` and/or `RoomId` blank if unassigned. Do not send `CRN`.
3. Upload: `POST /attendance/api/admin/lessons/bulk` as `multipart/form-data` with form field `file`.

If any row is invalid, no lessons are created. Response lists `errors` with Excel `rowNumber`.

```http
POST /attendance/api/admin/lessons/bulk
Authorization: Bearer <accessToken>
Content-Type: multipart/form-data; boundary=----client-boundary

------client-boundary
Content-Disposition: form-data; name="file"; filename="lesson-bulk-create-template.xlsx"
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet

<binary xlsx>
------client-boundary--
```

Success `201`:

```json
{
  "totalRows": 2,
  "createdCount": 2,
  "failedCount": 0,
  "created": [
    {
      "id": 55,
      "name": "Algorithms",
      "code": "CSE201",
      "instructorId": null,
      "roomId": null,
      "academicYear": 2026,
      "semester": "Spring",
      "crn": "20001",
      "maxCapacity": 35,
      "timesPerWeek": 2
    }
  ],
  "errors": []
}
```

Validation failure `400`:

```json
{
  "totalRows": 2,
  "createdCount": 0,
  "failedCount": 1,
  "created": [],
  "errors": [
    {
      "rowNumber": 3,
      "message": "Course 99 was not found. Use an Id from the Courses sheet."
    }
  ]
}
```

### Generate Lesson Sections

Plan sections for an existing course, review the suggestion, then confirm to create them. Rooms stay unassigned. CRNs are generated on create.

`instructorCount` is how many instructors will teach the course this term (example: `3`). `instructors` is that many slots: each value is an instructor user id, or `null` if that person will be assigned later. Missing trailing slots are treated as `null`. Empty GUID is treated as unassigned.

Instructors are assigned to sections in round-robin order. Capacity is split as evenly as possible (`totalStudents` seats total).

#### Preview (`POST /attendance/api/admin/courses/{courseId}/lessons/generate/preview`)

Does not persist lessons.

```json
{
  "totalStudents": 100,
  "academicYear": 2026,
  "semester": "Spring",
  "sectionCount": 3,
  "instructorCount": 3,
  "instructors": [
    "11111111-1111-1111-1111-111111111111",
    null,
    "22222222-2222-2222-2222-222222222222"
  ]
}
```

Example response:

```json
{
  "courseId": 7,
  "courseCode": "CSE201",
  "courseName": "Algorithms",
  "academicYear": 2026,
  "semester": "Spring",
  "totalStudents": 100,
  "sectionCount": 3,
  "instructorCount": 3,
  "totalSeats": 100,
  "existingLessonCountForTerm": 0,
  "instructorSlots": [
    "11111111-1111-1111-1111-111111111111",
    null,
    "22222222-2222-2222-2222-222222222222"
  ],
  "proposedLessons": [
    {
      "sectionNumber": 1,
      "courseId": 7,
      "instructorId": "11111111-1111-1111-1111-111111111111",
      "instructorDisplayName": "Ada Lovelace",
      "instructorEmail": "ada@example.com",
      "roomId": null,
      "academicYear": 2026,
      "semester": "Spring",
      "maxCapacity": 34
    },
    {
      "sectionNumber": 2,
      "courseId": 7,
      "instructorId": null,
      "instructorDisplayName": "",
      "instructorEmail": "",
      "roomId": null,
      "academicYear": 2026,
      "semester": "Spring",
      "maxCapacity": 33
    },
    {
      "sectionNumber": 3,
      "courseId": 7,
      "instructorId": "22222222-2222-2222-2222-222222222222",
      "instructorDisplayName": "Alan Turing",
      "instructorEmail": "alan@example.com",
      "roomId": null,
      "academicYear": 2026,
      "semester": "Spring",
      "maxCapacity": 33
    }
  ]
}
```

#### Confirm (`POST /attendance/api/admin/courses/{courseId}/lessons/generate`)

Send the preview payload back (you may edit `proposedLessons` first). Response `201`: `BulkCreateLessonsResultDto`.

Validation:

- `totalStudents` >= 1
- `sectionCount` >= 1 and <= `totalStudents` (max 200)
- `instructorCount` >= 0 (max 200); if omitted as `0` but `instructors` is non-empty, the list length is used
- `instructors` cannot be longer than `instructorCount`
- `academicYear` 2000..2100; `semester` `Fall` / `Spring` / `Summer`
- known instructor ids must exist in role `instructor`

`existingLessonCountForTerm` is informational; existing sections are not blocked or replaced.

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
- Bulk lesson Excel download/upload: see `Services/MyAdaAttendanceService/docs/LESSON_BULK_EXCEL.md`.
- Collection GET endpoints support optional `sortBy` / `sortDir`. Dedicated `/search` and `/filter` routes exist for each list.
- Auto-generate course sections: preview then confirm at `POST /attendance/api/admin/courses/{courseId}/lessons/generate/preview` and `POST /attendance/api/admin/courses/{courseId}/lessons/generate`.
