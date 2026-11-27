# Bulk Lesson Create (Excel)

Last updated: 2026-09-10

Office/registrar users can download an Excel template, fill one lesson per row, and upload the file to create many lessons at once.

This is an admin-only flow. Courses must already exist (`POST /api/admin/courses`) before they can be referenced in the spreadsheet.

## Base URL

- Gateway: `http://13.60.31.141:500/attendance`
- Direct service (if exposed): `http://13.60.31.141:5009`

Always include `/attendance` when calling through the gateway.

## Auth Header

```http
Authorization: Bearer <accessToken>
```

## Workflow

1. Download the template.
2. Open `lesson-bulk-create-template.xlsx`.
3. Copy `Id` values from the **Courses** sheet into `CourseId` on the **Lessons** sheet.
4. Optionally copy `UserId` values from the **Instructors** sheet into `InstructorId`. Leave `InstructorId` blank to create an unassigned lesson.
5. Optionally fill `RoomId`, or leave it blank if no room is assigned yet. Fill `AcademicYear`, `Semester`, and `MaxCapacity`.
6. Save as `.xlsx` (do not convert to CSV or `.xls`).
7. Upload the file. If any row is invalid, **no lessons are created**. Fix the reported rows and upload again.

## Endpoints

| Method | Gateway path | Direct path | Description |
|---|---|---|---|
| `GET` | `/attendance/api/admin/lessons/bulk/template` | `/api/admin/lessons/bulk/template` | Download the Excel template. |
| `POST` | `/attendance/api/admin/lessons/bulk` | `/api/admin/lessons/bulk` | Upload the filled template and create lessons. |

Single-lesson create is unchanged: `POST /attendance/api/admin/lessons`.

---

### `GET /api/admin/lessons/bulk/template`

Downloads a ready-to-fill Excel workbook.

**Response**

- `200 OK`
- `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- `Content-Disposition: attachment; filename=lesson-bulk-create-template.xlsx`
- Body: binary `.xlsx` file (not JSON, not AutoWrapper-wrapped)

The template contains four sheets:

| Sheet | Purpose |
|---|---|
| `Lessons` | Fill this sheet. One lesson per row, starting at row 2. |
| `Instructions` | How to fill the file. |
| `Courses` | Current courses (`Id`, `Code`, `Name`, `Department`, `Credits`, `TimesPerWeek`). Use `Id` as `CourseId`. |
| `Instructors` | Synced instructors (`UserId`, `DisplayName`, `Email`, `UserName`). Use `UserId` as `InstructorId`. |

#### `Lessons` columns

Keep the header row exactly as downloaded. Extra columns are ignored. Missing required headers are rejected.

| Column | Required | Notes |
|---|---|---|
| `CourseId` | Yes | Existing course id (positive integer). Copy from the Courses sheet. |
| `InstructorId` | No | Instructor user GUID. Leave blank if unassigned. Empty GUID (`00000000-0000-0000-0000-000000000000`) is rejected. Keep the cell as **text** when pasting a GUID. |
| `RoomId` | No | Integer ≥ 0 when set. Leave blank if unassigned. |
| `AcademicYear` | Yes | Calendar year of the term, e.g. `2026` for Spring 2026. Must be between `2000` and `2100`. |
| `Semester` | Yes | `Fall`, `Spring`, or `Summer` (case-insensitive). Numeric `1` / `2` / `3` is also accepted. A dropdown is provided. |
| `MaxCapacity` | Yes | Integer ≥ 0. |

Do **not** include `CRN`. The server generates it for each created lesson.

Limits:

- Completely empty rows are ignored.
- At least one filled row is required.
- Maximum **500** lesson rows per upload.
- Maximum file size **5 MB**.
- Only `.xlsx` is accepted.

**cURL**

```bash
curl -X GET "http://13.60.31.141:500/attendance/api/admin/lessons/bulk/template" \
  -H "Authorization: Bearer <accessToken>" \
  -o lesson-bulk-create-template.xlsx
```

---

### `POST /api/admin/lessons/bulk`

Uploads a filled template and creates lessons.

**Request**

- `Content-Type: multipart/form-data`
- Form field name: `file`
- File: the filled `.xlsx` template

Let the HTTP client set the multipart boundary. Do not send `application/json`.

**Success — `201 Created`**

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
      "instructorDisplayName": "",
      "instructorEmail": "",
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

Responses may be wrapped by AutoWrapper (`result`, `statusCode`, etc.) depending on host configuration.

**Validation failure — `400 Bad Request`**

If any row is invalid, **nothing is created**.

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

`rowNumber` is the Excel row (header is row 1, first data row is 2).

**File-level errors — `400 Bad Request`**

```json
{
  "message": "An Excel file (.xlsx) is required. Download the template, fill it, and upload that file."
}
```

Typical file-level messages:

- Missing / empty file
- File is not `.xlsx`
- File larger than 5 MB
- Workbook cannot be read
- `Lessons` sheet missing
- Required headers missing or renamed
- No filled data rows
- More than 500 data rows

**cURL**

```bash
curl -X POST "http://13.60.31.141:500/attendance/api/admin/lessons/bulk" \
  -H "Authorization: Bearer <accessToken>" \
  -F "file=@lesson-bulk-create-template.xlsx;type=application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
```

**JavaScript (fetch)**

```javascript
const form = new FormData();
form.append("file", excelFile); // File from <input type="file">

const response = await fetch("/attendance/api/admin/lessons/bulk", {
  method: "POST",
  headers: { Authorization: `Bearer ${accessToken}` },
  body: form
});
```

Do not set `Content-Type` manually on this request. The browser/client must include the multipart boundary.

## Example `Lessons` sheet

| CourseId | InstructorId | RoomId | AcademicYear | Semester | MaxCapacity |
|---|---|---|---|---|---|
| 12 | | | 2026 | Spring | 35 |
| 12 | 11111111-1111-1111-1111-111111111111 | 402 | 2026 | Fall | 40 |

Row 2 creates a lesson with no instructor and no room. Row 3 assigns both.

## Frontend notes

1. Offer **Download template**, then **Upload filled file**.
2. Accept only `.xlsx`.
3. On `400` with `errors[]`, show each `rowNumber` + `message` and do not treat the upload as partial success.
4. After `201`, refresh the lessons list. Created lessons include generated `crn` values.
5. Instructors on the template come from the local synced user directory (`GET /api/admin/users/roles/instructor`). Sync users first if that sheet is empty.

## Related endpoints

- Create one lesson: `POST /api/admin/lessons`
- List courses: `GET /api/admin/courses`
- List instructors: `GET /api/admin/users/roles/instructor`
- Sync users into the local directory: `POST /api/admin/users/sync`
