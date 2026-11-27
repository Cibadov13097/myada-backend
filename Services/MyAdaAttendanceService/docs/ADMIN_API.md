# Admin API (`/api/admin`)

Base path: **`/api/admin`**.

JSON serialization uses **`JsonStringEnumConverter`**, so enums such as **`AcademicSemester`** are typically sent and returned as **strings** (`"Fall"`, `"Spring"`, `"Summer"`) in JSON. Numeric values `1`, `2`, `3` match the underlying enum integers.

Responses may be wrapped by **AutoWrapper** (envelope with `result`, `statusCode`, etc.) depending on host configuration—check a live response or Swagger for the exact shape.

**Instructor & student APIs (not under `/api/admin`):** Lesson and session operations use explicit ids in the path, for example `GET /api/instructors/{instructorId}/lessons` and `GET /api/students/{studentId}/enrollments`. These ids are auth-service user **GUIDs**. Details for attendance and session listing are in **`ATTENDANCE_API.md`**.

**Bulk lesson create from Excel:** `GET /api/admin/lessons/bulk/template` and `POST /api/admin/lessons/bulk`. Full guide: **`LESSON_BULK_EXCEL.md`**.

---

## List sorting, search, and filtering

All collection GET endpoints accept optional `sortBy` (any scalar field on the item DTO) and `sortDir` (`asc` default, or `desc`). Nested collections such as `LessonDto.sessions` are not sortable. Unknown `sortBy` or invalid `sortDir` returns **400**.

Each collection also has:

- `GET {path}/search?q={term}` — case-insensitive contains across all scalar fields (`search` is an alias of `q`)
- `GET {path}/filter?{field}={value}` — AND of provided fields (strings: contains; numbers/bools/guids/enums: exact; date-only `DateTime` matches that calendar day)

Search and filter routes also accept `sortBy` / `sortDir`. Instructor and student collection GETs use the same convention (`/api/instructors/...` and `/api/students/...`).

---

## Courses

### `GET /api/admin/courses`

Lists all courses (ordered by department, then code, unless `sortBy` is set).

**Query:** optional `sortBy`, `sortDir`

**Response:** `CourseDto[]`

Sortable/filterable fields: `id`, `name`, `department`, `code`, `credits`, `timesPerWeek`.

---

### `GET /api/admin/courses/search`

Same collection as `GET /api/admin/courses`, filtered by `q` / `search`.

---

### `GET /api/admin/courses/filter`

Same collection as `GET /api/admin/courses`, filtered by course fields (AND).

Example: `/api/admin/courses/filter?department=Math&credits=3&sortBy=code`

---

### `GET /api/admin/courses/{courseId}`

Returns one course by id.

**Response:** `CourseDto`  
**Errors:** 404 if not found.

---

### `POST /api/admin/courses`

Creates a course.

**Body — `CreateCourseDto`:**

| Field          | Type   | Notes                          |
|----------------|--------|--------------------------------|
| `name`         | string | Catalog title                  |
| `department`   | string |                                |
| `code`         | string | Unique together with department |
| `credits`      | int    | ≥ 0                            |
| `timesPerWeek` | int    | ≥ 0                            |

**Response:** `201 Created`, body `CourseDto`, header `Location: /api/admin/courses/{id}`

---

### `PUT /api/admin/courses/{courseId}`

Updates a course.

**Body — `UpdateCourseDto`:** same fields as `CreateCourseDto`.

---

### `DELETE /api/admin/courses/{courseId}`

Deletes a course (may fail if lessons or other rules block deletion).

---

### `GET /api/admin/courses/{courseId}/lessons`

Returns all lessons for that course.

**Query:** optional `sortBy`, `sortDir`

**Response:** `LessonDto[]` (summary list; typically without nested `sessions`).

Also: `GET /api/admin/courses/{courseId}/lessons/search` and `GET /api/admin/courses/{courseId}/lessons/filter`.

---

### `POST /api/admin/courses/{courseId}/lessons/generate/preview`

Suggests lesson sections for this course. **Does not create anything.** POST the returned body to `.../generate` after the user confirms (they may edit `proposedLessons` first).

**Body — `GenerateLessonsRequestDto`:**

| Field             | Type               | Notes |
|-------------------|--------------------|-------|
| `totalStudents`   | int                | Students taking the course this term; >= 1. Total seats will equal this. |
| `academicYear`    | int                | 2000..2100 |
| `semester`        | `AcademicSemester` | `Fall`, `Spring`, `Summer` |
| `sectionCount`    | int                | Number of lessons to create; 1..`totalStudents`, max 200 |
| `instructorCount` | int                | How many instructors will teach this course this term (example: `3`). >= 0. If `0` and `instructors` is non-empty, the list length is used. |
| `instructors`     | guid?[]            | Optional. One id per instructor slot, or `null` to assign later. Padded with `null` up to `instructorCount`. Empty GUID = unassigned. Cannot be longer than `instructorCount`. |

Instructors are assigned to sections **round-robin**. Capacity is split as evenly as possible (remainder seats go to the first sections). `roomId` is always `null` in the suggestion.

**Response:** `GenerateLessonsPreviewDto` including `proposedLessons`, `totalSeats`, `instructorSlots`, and `existingLessonCountForTerm` (informational; existing sections are not replaced).

---

### `POST /api/admin/courses/{courseId}/lessons/generate`

Creates the confirmed sections. Send the preview payload (optionally edited `proposedLessons`).

**Response:** `201 Created`, body `BulkCreateLessonsResultDto` (`createdCount`, `created` as `LessonDto[]`). CRNs are generated. Invalid preview returns `400` and creates nothing.

---

## Auth user directory

The attendance service can sync instructors/students from the auth microservice into a local directory cache. In the current ID-only validation mode, missing users may also be inserted as local placeholder rows.

### `POST /api/admin/users/sync`

Pulls users from the auth service and upserts them into the local directory.

**Query params:** `roles` (optional, repeatable). Default roles are `student` and `instructor`.

**Response — `UserSyncResultDto`:**

| Field           | Type       |
|----------------|------------|
| `importedCount` | int       |
| `roles`         | string[]   |
| `syncedAtUtc`   | datetime   |

---

### `GET /api/admin/users/roles/{role}`

Returns synced users for the requested role.

**Query:** optional `sortBy`, `sortDir`

**Response — `ExternalUserDirectoryDto[]`:**

| Field          | Type     |
|----------------|----------|
| `userId`       | guid     |
| `role`         | string   |
| `userName`     | string   |
| `email`        | string   |
| `firstName`    | string   |
| `lastName`     | string   |
| `displayName`  | string   |
| `phoneNumber`  | string?  |
| `userType`     | string?  |
| `status`       | string?  |
| `syncedAtUtc`  | datetime |

Also: `GET /api/admin/users/roles/{role}/search` and `GET /api/admin/users/roles/{role}/filter` using the same fields.

---

## Lessons

### `GET /api/admin/lessons`

Lists all lessons.

**Query:** optional `sortBy`, `sortDir`

**Response:** `LessonDto[]` (summary; no `sessions`).

Sortable/filterable fields: `id`, `name`, `code`, `instructorId`, `instructorDisplayName`, `instructorEmail`, `roomId`, `academicYear`, `semester`, `crn`, `maxCapacity`, `timesPerWeek`.

Also: `GET /api/admin/lessons/search` and `GET /api/admin/lessons/filter`.

---

### `GET /api/admin/lessons/by-term?academicYear={year}&semester={semester}`

Lists lessons by academic term.

**Query params:**

| Field          | Type               | Notes |
|----------------|--------------------|-------|
| `academicYear` | int                | Required; valid range 2000..2100 |
| `semester`     | `AcademicSemester` | Required; `Fall`, `Spring`, or `Summer` |
| `sortBy`       | string             | Optional |
| `sortDir`      | string             | Optional; `asc` or `desc` |

**Response:** `LessonDto[]` (summary; no `sessions`).

Also: `GET /api/admin/lessons/by-term/search` and `GET /api/admin/lessons/by-term/filter` (same required term params).

---

### `GET /api/admin/lessons/scheduling`

Scheduling integration: instructor user id, enrollment headcount, weekly meeting count, course labels for **one academic term**.

**Query params:**

| Field          | Type               | Notes |
|----------------|--------------------|-------|
| `academicYear` | int                | Required; valid range 2000..2100 |
| `semester`     | `AcademicSemester` | Required; `Fall`, `Spring`, or `Summer` |
| `sortBy`       | string             | Optional |
| `sortDir`      | string             | Optional; `asc` or `desc` |

Example: `/api/admin/lessons/scheduling?academicYear=2026&semester=Spring`

**Response:** `LessonSchedulingRow[]`

| Field              | Type   | Description                                      |
|--------------------|--------|--------------------------------------------------|
| `lessonId`         | int    |                                                  |
| `instructorUserId` | guid?  | Instructor auth user id, or `null` if unassigned |
| `enrollment`       | int    | Count of enrollment rows for the lesson          |
| `maxCapacity`      | int    | Section capacity                                 |
| `timesPerWeek`     | int    | From the linked course                           |
| `courseCode`       | string | Course code                                      |
| `courseTitle`      | string | Course name / title                              |

Also: `GET /api/admin/lessons/scheduling/search` and `GET /api/admin/lessons/scheduling/filter` (same required term params).

---

### `GET /api/admin/lessons/{lessonId}`

Lesson detail, including ordered sessions when loaded from the service.

**Response:** `LessonDto` (often includes `sessions`).

---

### `POST /api/admin/lessons`

Creates a lesson for an **existing** course. **Do not send `crn`** (JSON camelCase for `CRN`)—the server generates it.

**Body — `CreateLessonDto`:**

| Field           | Type               | Notes                                                |
|-----------------|--------------------|------------------------------------------------------|
| `courseId`      | int                | Required; course must exist                          |
| `instructorId`  | guid?              | Optional; omit/`null` = unassigned. Empty GUID rejected. When set, user must exist in role `instructor`. |
| `roomId`        | int?               | Optional; omit/`null` = unassigned. Negative values rejected. |
| `academicYear`  | int                | Calendar year for the term (e.g. `2026` for Spring 2026); validated ~2000–2100 |
| `semester`      | `AcademicSemester` | See enum below                                       |
| `maxCapacity`   | int                | ≥ 0                                                  |

**Response:** `201 Created`, body `LessonDto`, header `Location: /api/admin/lessons/{id}`

---

### `GET /api/admin/lessons/bulk/template`

Downloads an Excel workbook for bulk lesson create. Full details: **`LESSON_BULK_EXCEL.md`**.

**Response:** `200 OK`, binary `.xlsx` (`lesson-bulk-create-template.xlsx`). Not JSON.

Sheets: `Lessons` (fill this), `Instructions`, `Courses` (use `Id` as `CourseId`), `Instructors` (use `UserId` as `InstructorId`).

---

### `POST /api/admin/lessons/bulk`

Uploads a filled template. Form field name: **`file`**. `multipart/form-data`. `.xlsx` only, max 5 MB, max 500 rows.

If any row is invalid, **no lessons are created**.

**Success:** `201 Created`, body `BulkCreateLessonsResultDto` (`totalRows`, `createdCount`, `failedCount`, `created`, `errors`).

**Errors:** `400` with the same DTO when rows fail, or `{ "message": "..." }` for file-level problems.

Full request/response examples: **`LESSON_BULK_EXCEL.md`**.

---

### `PUT /api/admin/lessons/{lessonId}`

Updates a lesson. If **term** (`academicYear` / `semester`) changes, the server may assign a **new CRN** for the new term.

**Body — `UpdateLessonDto`:**

| Field           | Type               | Notes |
|-----------------|--------------------|-------|
| `courseId`      | int                |       |
| `instructorId`  | guid?              | Optional; `null` clears the assignment. Empty GUID rejected. |
| `roomId`        | int?               | Optional; `null` clears the assignment. Negative values rejected. |
| `academicYear`  | int                |       |
| `semester`      | `AcademicSemester` |       |
| `maxCapacity`   | int                |       |

---

### `DELETE /api/admin/lessons/{lessonId}`

Deletes a lesson.

---

### `GET /api/admin/lessons/{lessonId}/enrollments`

Returns current enrollments for a lesson, enriched from the synced auth-user directory.

**Query:** optional `sortBy`, `sortDir`

**Response:** `EnrollmentDto[]`

| Field             | Type   |
|-------------------|--------|
| `id`              | int    |
| `lessonId`        | int    |
| `studentId`       | guid   |
| `studentFullName` | string |
| `studentCode`     | string |
| `studentEmail`    | string |

Also: `GET /api/admin/lessons/{lessonId}/enrollments/search` and `GET /api/admin/lessons/{lessonId}/enrollments/filter`.

---

### `POST /api/admin/lessons/{lessonId}/enrollments`

Enrolls one real auth-service student into a lesson.

**Body — `CreateEnrollmentDto`:**

| Field       | Type | Notes |
|-------------|------|-------|
| `studentId` | guid | Required id; validated in local directory/id-only mode |

**Response:** `201 Created`, body `EnrollmentDto`

---

## Instructors (admin)

### `GET /api/admin/instructors/{instructorId}/lessons`

Returns lessons assigned to the given instructor.

**Query:** optional `sortBy`, `sortDir`

**Response:** `LessonDto[]`

Also: `GET /api/admin/instructors/{instructorId}/lessons/search` and `GET /api/admin/instructors/{instructorId}/lessons/filter`.

---

## Sessions and attendance (admin)

### `GET /api/admin/lessons/{lessonId}/sessions`

All class sessions for a lesson, with attendance rollups.

**Query:** optional `sortBy`, `sortDir`

**Response:** `SessionDto[]`

**`SessionDto` (high level):** `id`, `lessonId`, `lessonName` (course title), `startTime`, `endTime`, `topic`, `isAttendanceActive`, activation timestamps, `totalStudents`, `presentCount`, `lateCount`, `absentCount`, `excusedCount`

Also: `GET /api/admin/lessons/{lessonId}/sessions/search` and `GET /api/admin/lessons/{lessonId}/sessions/filter`.

---

### `POST /api/admin/lessons/{lessonId}/sessions/generate`

Bulk-creates `LessonSession` rows between **`fromDate`** and **`toDate`** (inclusive) for each calendar day that matches a **weekly slot** (`dayOfWeek` + wall-clock `startTime` / `endTime`). Sessions that already exist with the same **date + start + end** are skipped.

**Limits:** date span ≤ 731 days; total proposed sessions ≤ 2000 per request.

**Body — `BulkGenerateSessionsDto`:**

| Field          | Type                      | Notes |
|----------------|---------------------------|--------|
| `fromDate`     | `DateOnly` (JSON date)    | Inclusive |
| `toDate`       | `DateOnly`                | Inclusive, must be ≥ `fromDate` |
| `weeklySlots`  | `WeeklySessionSlotDto[]`  | At least one slot |
| `topic`        | string?                   | Optional; applied to every created session |

**`WeeklySessionSlotDto`:**

| Field         | Type        | Notes |
|---------------|-------------|--------|
| `dayOfWeek`   | `DayOfWeek` | JSON string enum, e.g. `Monday`, `Tuesday` (server calendar; `Sunday` = 0 … `Saturday` = 6 in .NET) |
| `startTime`   | `TimeOnly`  | e.g. `"09:00:00"` |
| `endTime`     | `TimeOnly`  | Must be after `startTime` |

**Response — `BulkGenerateSessionsResponseDto`:**

| Field                     | Type          |
|---------------------------|---------------|
| `createdCount`            | int           |
| `skippedDuplicateCount`   | int           |
| `createdSessions`         | `SessionDto[]` | New rows only, ordered by date/time |

**Instructor mirror:** `POST /api/instructors/{instructorId}/lessons/{lessonId}/sessions/generate` (same body; `instructorId` must match the authenticated user; caller must own the lesson).

---

### `GET /api/admin/sessions/{sessionId}/attendance`

Full attendance roster for a session.

**Query:** optional `sortBy`, `sortDir`

**Response:** `AttendanceDto[]`

**`AttendanceDto` (high level):** `id`, `sessionId`, `lessonId`, `studentId`, `studentFullName`, `studentCode`, `status`, marking/scan fields, `isManuallyAdjusted`, `instructorNote`, etc.

Also: `GET /api/admin/sessions/{sessionId}/attendance/search` and `GET /api/admin/sessions/{sessionId}/attendance/filter`.

---

### `PATCH /api/admin/sessions/{sessionId}/attendance/{attendanceId}`

Admin correction for one attendance row (`attendanceId`). `sessionId` is part of the route for REST consistency.

**Body — `AdminAttendanceCorrectionDto`:**

| Field    | Type    | Notes                                                |
|----------|---------|------------------------------------------------------|
| `status` | string  | One of: `Present`, `Late`, `Absent`, `Excused`       |
| `note`   | string? | Optional                                             |

---

### `DELETE /api/admin/sessions/{sessionId}/attendance/{attendanceId}`

Deletes an attendance record (admin).

---

## Shared DTOs and enums

### `CourseDto`

| Field            | Type   |
|------------------|--------|
| `id`             | int    |
| `name`           | string |
| `department`     | string |
| `code`           | string |
| `credits`        | int    |
| `timesPerWeek`   | int    |

### `LessonDto`

| Field           | Type               | Notes |
|-----------------|--------------------|-------|
| `id`            | int                |       |
| `name`          | string             | **Course catalog title** (not stored separately on the lesson) |
| `code`          | string             | Course code |
| `instructorId`  | guid?              | Auth-service instructor id, or `null` if unassigned |
| `instructorDisplayName` | string     | Synced auth user display name |
| `instructorEmail` | string           | Synced auth user email |
| `roomId`        | int?               | Assigned room id, or `null` if unassigned |
| `academicYear`  | int                |       |
| `semester`      | `AcademicSemester` |       |
| `crn`           | string             | Five characters; see CRN rules below (property `CRN` in C#) |
| `maxCapacity`   | int                |       |
| `timesPerWeek`  | int                | From the linked course (weekly meetings) |
| `sessions`      | `SessionShortDto[]`? | Present on detail reads when returned by the service |

### `SessionShortDto` (nested on lesson detail)

| Field                 | Type    |
|-----------------------|---------|
| `id`                  | int     |
| `date`                | date    |
| `startTime` / `endTime` | time  |
| `topic`               | string? |
| `isAttendanceActive`  | bool    |

### `AcademicSemester` (enum)

Used in lesson DTOs and for CRN generation (first digit).

| JSON (typical) | Integer | CRN first digit |
|----------------|---------|-----------------|
| `Fall`         | `1`     | `1`             |
| `Spring`       | `2`     | `2`             |
| `Summer`       | `3`     | `3`             |

### `AttendanceStatus` (for `AttendanceDto.status` / corrections)

String values: **`Present`**, **`Late`**, **`Absent`**, **`Excused`**.

---

## CRN rules (reference)

- **Format:** 5 characters: **one** digit (`1` / `2` / `3` matching semester) + **four** digits (`0001`–`9999`).
- **Scope:** Sequence resets per **`(academicYear, semester)`**.
- **Uniqueness:** **`(academicYear, Semester, CRN)`** is unique in the database.
- **Client rule:** Omit `crn` on create; it is assigned by the server.

---

## Document version

Aligned with `AdminController` and application DTOs in this repository. Regenerate or edit this file when routes or contracts change.
