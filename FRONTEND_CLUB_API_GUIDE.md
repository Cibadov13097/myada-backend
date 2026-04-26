# Club Management API — Frontend integration guide

This document describes backend behavior and contracts **implemented in this project** for FrontWeb / SPA integration. Base URL is assumed to be `{CLUB_API_BASE}/api/v1` (no trailing slash on the base). For the deployment shared in this thread, use `http://13.60.31.141:5000/api/v1`. API versioning uses `v1` in the path.

Unless noted otherwise, responses are wrapped by **AutoWrapper** as `{ "result": ... }` or `{ "data": ... }` (depending on configuration). Your `unwrapApiResponse` should read **`result`** or **`data`**.

Errors often surface as `{ "message": "..." }` and sometimes `{ "errors": { "field": ["..."] } }` (club proposal validation). Prefer the first available of **`message`**, **`title`**, or **`detail`** for user-facing text.

---

## 1. Authentication and user scoping

- **Bearer token** on protected routes (same as existing club APIs).
- **User-scoped routes** (e.g. `GET .../users/{userId}/...`): `{userId}` must match the JWT **`sub`** (or configured **`user_id`** claim), same pattern as `GET /api/v1/users/{userId}/club-memberships`. Admins may bypass per existing `IsMyAdaAdministrator()` rules where implemented.

---

## 2. Club proposals (student submit + staff queue)

### 2.1 Submit club proposal (multipart or JSON body)

**Endpoint**

- `POST /api/v1/club-proposals`  
  - Preferred: `Content-Type: multipart/form-data`  
  - Also accepts: `Content-Type: application/json` when `logoFile` and `constitutionFile` are already uploaded URLs  
  - **No auth required** (submitter is read from request body field `organizationId`, which must be exactly 9 digits).

**Swagger UI note**

- If you want `logoFile` and `constitutionFile` to appear as file attachments in Swagger, choose `multipart/form-data` in the request body content-type selector.
- If Swagger is set to `application/json`, those fields are expected to be URL strings, so Swagger shows text inputs instead of file pickers.

**Request body fields**

| Field | Required | Notes |
|-----------|----------|--------|
| `organizationId` | Yes | Submitter organization id, exactly **9 digits** |
| `name` | Yes | Club name |
| `description` | Yes for “legacy” mode | Single block (e.g. sections joined with `\n\n`), max **12000** chars. Prefixes used in UI: `Summary:`, `Uniqueness:`, `Goals:`, `Activities:`, `Mission alignment:`, `Vision:` |
| `logoFile` | Yes | In multipart mode: file attachment uploaded to S3 by backend. In JSON mode: already-uploaded URL/reference string |
| `constitutionFile` | Yes | In multipart mode: file attachment uploaded to S3 by backend. In JSON mode: already-uploaded URL/reference string |
| `presidentStudentId` | Optional | Exactly **9 digits** if structured path |
| `vicePresidentStudentId` | Optional | Exactly **9 digits** if structured path |
| `otherMembersJson` | Optional | List of user organization ids, each exactly **9 digits**. In multipart mode send repeated form fields using the same key |
| `commitment` | Optional | `"yes"` when structured officer data is used |
| `shortDesc`, `uniqueDesc`, `goals`, `activities`, `alignment`, `vision` | Optional | Plain strings; avoid relying on parsing `description` only |

**Behavior**

- **Legacy mode**: `name` + `description` + valid files; no structured officer fields required.
- **Structured mode**: if any structured / extra text field is present, **president**, **vice president**, and every `otherMembersJson` item are validated as **9-digit** ids, and **`commitment: "yes"`** is required.
- Empty title+description rows are normalized out server-side when building stored list from structured parts.

**Example multipart payload**

Use repeated `otherMembersJson` fields for the user list:

```text
organizationId=123456789
name=Club Propose
description=Good to go
logoFile=<binary file>
constitutionFile=<binary file>
presidentStudentId=123456780
vicePresidentStudentId=876543210
otherMembersJson=123456781
otherMembersJson=123456782
commitment=yes
shortDesc=short text
uniqueDesc=unique text
goals=goals text
activities=activities text
alignment=alignment text
vision=vision text
```

**Example multipart cURL**

```bash
curl -X POST "{CLUB_API_BASE}/api/v1/club-proposals" \
  -F "organizationId=123456789" \
  -F "name=Club Propose" \
  -F "description=Good to go" \
  -F "logoFile=@logo.png" \
  -F "constitutionFile=@constitution.pdf" \
  -F "presidentStudentId=123456780" \
  -F "vicePresidentStudentId=876543210" \
  -F "otherMembersJson=123456781" \
  -F "otherMembersJson=123456782" \
  -F "commitment=yes"
```

- In frontend code, when using `FormData`, do not set `Content-Type: multipart/form-data` yourself. The browser or HTTP client must add the boundary automatically.

**Example JSON request body**

```json
{
  "organizationId": "123456789",
  "name": "Club Propose",
  "description": "Good to go",
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

**Success (HTTP 200, inside AutoWrapper `result`)**

```json
{
  "proposalId": "PROP-2026-001",
  "id": "PROP-2026-001",
  "status": "pending_review",
  "submittedAt": "2026-04-20T12:00:00.0000000Z"
}
```

**Validation error (HTTP 400)**

```json
{
  "message": "Validation failed.",
  "errors": {
    "presidentStudentId": ["..."]
  }
}
```

---

### 2.2 Student Services — club proposal queue

| Method | Path |
|--------|---------|
| GET | `/api/v1/student-services/clubs/proposals` |
| GET | `/api/v1/student-services/clubs/proposals/{proposalId}` |
| POST | `/api/v1/student-services/clubs/proposals/{proposalId}/approve` |
| POST | `/api/v1/student-services/clubs/proposals/{proposalId}/reject` (body: `{ "reason": "..." }`) |
| POST | `/api/v1/student-services/clubs/proposals/{proposalId}/request-revision` (body: `{ "changes": "..." }`) |

**List query (recommended)**

- `status`, `search`, `page`, `limit`

**List response shape**

```json
{
  "items": [ /* proposal objects */ ],
  "page": 1,
  "totalCount": 0
}
```

List/detail items include a **superset** of fields aligned with your UI mapper (e.g. `id`, `proposalId`, `clubName` / `name`, `shortDesc`, `uniqueDesc`, `goals`, `activities`, `primaryOfficers`, `otherMembers`, `alignment`, `vision`, `commitment`, `constitutionDoc`, `logoUrl`, `status`, `submittedAt`, etc.). Exact keys are returned as implemented in the service; treat unknown fields as optional.

**Approve success (`result`)**

- Includes **`clubId`** (numeric club id as string), **`proposalId`**, **`status`: `"approved"`** (creates a real club in DB on approve).

**Reject / request-revision**

- Return objects like `{ "status": "rejected" }` or `{ "status": "under_revision" }` inside `result`.

**Persistence note**

- Proposals are stored **in-memory** in the current implementation (process lifetime). After restart, lists are empty unless you re-seed or move to a database.

---

### 2.3 Club proposal requirements (institution)

| Method | Path |
|--------|------|
| GET | `/api/v1/student-services/club-proposal-requirements` |
| PUT | `/api/v1/student-services/club-proposal-requirements` |

**PUT body**

```json
{
  "requirements": ["Minimum of 10 active student members", "..."],
  "deadline": "2026-05-01"
}
```

**GET/PUT response**

- Same DTO shape (`requirements`, `deadline`) inside AutoWrapper.

**Note**

- Stored in-memory; resets on service restart.

---

## 3. Event proposals (JSON or multipart)

### 3.1 Club admin — suggest event

- `POST /api/v1/club-admin/{clubId}/events/proposals`  
- `Content-Type: multipart/form-data`
- Multipart fields: `name`, `dateTime`, `duration`, `attendance`, `venue`, `description`, `objectives`, `submittedByOrganization?`, `imageFile?`, `subEventsJson?`, `logisticsJson?`
- Attach the proposal image using `imageFile`. This route rejects `imageUrl`.
- In Swagger, this route now shows the file picker directly because the request body is multipart-only.

**Success (`result`)**

```json
{
  "proposalId": "1",
  "id": "1",
  "status": "pending_review",
  "clubId": "{clubId}",
  "imageUrl": "https://cdn.example/event-proposals/poster.png"
}
```

(`clubId` is the route string; `proposalId` / `id` are numeric strings from a server counter. `imageUrl` is present in the response when an uploaded file was supplied.)

### 3.2 Student Services — staff event proposal + queue

**Create (staff)**

- `POST /api/v1/student-services/events/proposals`
- Accepts `application/json` or `multipart/form-data`.
- Multipart uploads use `imageFile`; JSON requests use `imageUrl` when the file is already uploaded elsewhere.

**List / detail / actions**

- GET `/api/v1/student-services/events/proposals` — query: `search`, `status`, `page`, `limit`
- GET `/api/v1/student-services/events/proposals/{proposalId}`
- POST `/api/v1/student-services/events/proposals/{proposalId}/approve` — body **`EventProposalApproveDto`** (see below)
- POST `/api/v1/student-services/events/proposals/{proposalId}/reject` — `{ "reason": "..." }`
- POST `/api/v1/student-services/events/proposals/{proposalId}/request-revision` — `{ "changes": "..." }`

**Event proposal body (shared DTO for reads and staff JSON create)**

Shared shape (`EventProposalSubmitDto`):

| Field | Type | Notes |
|-------|------|--------|
| `name` | string | Required on create |
| `dateTime` | string | Local `YYYY-MM-DDTHH:mm:ss` (no `Z`) |
| `duration` | string | Hours as string, e.g. `"3"` |
| `attendance` | string | Parsed to seat limit; minimum 1 if invalid |
| `venue` | string | |
| `description` | string | |
| `objectives` | string | |
| `imageUrl` | string? | Existing uploaded image URL/reference. Also returned on proposal reads. |
| `subEvents` | array | `{ title, capacity, start, end, date }` strings |
| `logistics` | object | `avSetup`, `security`, `catering`, `cleaning`, `otherNeeds` (camelCase JSON) |
| `submittedByOrganization` | string? | Staff flow only |

**Multipart-specific notes**

- `imageFile` is the direct upload field for Swagger/Postman/browser forms.
- Club-admin create rejects `imageUrl`; attach the image file instead.
- `subEventsJson` must be a JSON array string.
- `logisticsJson` must be a JSON object string.
- On the student-services create route, if both `imageUrl` and `imageFile` are sent, the uploaded file wins and its stored URL becomes the proposal image.

**Persistence**

- Event proposals are persisted to Postgres in `club.EventProposals`.
- Proposing an event (club admin or staff) **does not** create an `club.Events` row.
- Only Student Services **approval** creates the actual event in `club.Events`.
- Proposal list/detail payloads expose the proposal image as both `imageUrl` and legacy alias `posterUrl`.
- When Student Services approves the proposal, that same image URL is copied into the created event's `ImageUrl`.

**Approve body (`EventProposalApproveDto`)**

```json
{
  "assignments": [
    { "subEventIndex": 0, "buildingId": "b1", "roomId": "r1" }
  ]
}
```

**Approve rules**

- If **`subEvents`** on the proposal is **non-empty**, every index **`0 .. subEvents.length-1`** must appear in **`assignments`** with non-empty **`buildingId`** and **`roomId`**. Otherwise the API returns **400** with an **`ArgumentException`** message (AutoWrapper surfaces `message`).

**Approve success (`result`)**

- Includes **`eventId`** (numeric event id as string), **`proposalId`**, **`status`: `"approved"`** (creates a real event via `IEventService`).

---

## 4. Public club detail + focus areas

### 4.1 GET club by id (student / directory)

- `GET /api/v1/clubs/{clubId}` (`clubId` is **integer**)

**Response highlights (inside `result`)**

| Field | Notes |
|-------|--------|
| `focusAreas` | Array of `{ "icon", "title", "description" }`, **0–6** items, order = display order |
| `email` | Public contact email from DB; **`null`** when unset (no hardcoded fallback). |
| `website`, `instagram`, `x`, `tiktok` | Top-level strings from canonical `SocialLinksJson`; **`null`** when empty (omit empty rows in UI). |
| `twitter` | Legacy read alias: same value as **`x`** when `x` is set; otherwise **`null`**. |
| `socialLinks` | Optional object with the same keys; omitted when no link is stored. Always consistent with the top-level link fields (single source: `SocialLinksJson`). |
| `resources`, `documents` | Same array when present; **`null`** when there are no downloadable links. Each item is `{ "url", "title?", "id?" }` after normalization (see `ClubResourcesJson`). |
| `about` | Club description |
| `officers` | Array sourced from DB table `club.ClubEmployees`, ordered by insertion. Each item is `{ "id", "userId", "positionId" }` where `positionId` may be `null`. |
| Other fields | `memberProfiles`, `announcements` are still empty placeholders |

**Legacy `focusAreas` entries**

- If stored JSON had **string** elements, read API normalizes to `{ icon: "target", title: "<string>", description: "" }`.

---

### 4.2 PATCH club admin profile (focus areas + social + files)

- `PATCH /api/v1/club-admin/{clubId}/profile`  
- `Content-Type: multipart/form-data`  
- **Option A (immediate publish):** changes apply to the published club row; **`pendingReview`** in the response is always **`false`**. (Option B approval queue is not implemented.)

**Multipart part names (exact)**

| Part name | Type | When omitted |
|-----------|------|----------------|
| `focusAreasJson` | string (UTF-8 JSON) | Focus areas **unchanged** |
| `socialLinks` | string (JSON object) | Social links **unchanged** (merge-patch; see below) |
| `email` | string | Email **unchanged** |
| `resourcesJson` | string (UTF-8 JSON array) | Resources list **unchanged** |
| `logoFile` | file | When uploaded successfully, persisted into the club's published `ProfileImageUrl` |
| `backgroundFile` | file | When uploaded successfully, persisted into the club's published `BackgroundImageUrl` |

**`focusAreasJson`**

- Must be a **JSON array** (not wrapped in an object), e.g. two cards:

```json
[
  { "icon": "lightbulb", "title": "Innovation", "description": "Weekly design sprints and prototyping sessions." },
  { "icon": "users", "title": "Community", "description": "Peer mentoring and open collaboration hours." }
]
```

- **`[]`** clears all focus areas.
- **Replace semantics:** the saved list is **exactly** the validated normalized array (no merge by server ids).

**Normalization (server mirrors SPA rules)**

- Trim `title` / `description`.
- Drop items where **both** title and description are empty after trim.
- **Max 6** items after normalization → else **400** `"At most 6 focus areas are allowed"`.
- **`icon`**: required whitelist (case-sensitive set). Unknown icon on **write** → **400** `"Invalid icon for focus area at index {i}"` (no silent remap to `target` on write).
- **`title`** max **40** chars after trim; **`description`** max **180** after trim.
- Each element must be a **JSON object** (string elements rejected): `"Each focus area must be an object with icon, title, and description"`.
- **HTML:** `<` or `>` in title or description → **400**.

**`socialLinks`** (merge-patch)

- If the part is **omitted**, stored links are **unchanged**.
- If the part is **sent**, value must be non-whitespace JSON **object**. `{}` is valid (no-op: keeps existing links).
- **Per-key rules:** a key **absent** → leave that field unchanged. A key **present** with empty string / JSON null / whitespace → **clear** that field. Values are normalized to full URLs where applicable (`http` → `https`, scheme-less host → `https://…`).
- Keys: **`website`**, **`instagram`**, **`x`**, **`tiktok`**. **`twitter`** is accepted as an alias for **`x`** and is stored under **`x`** only.
- Invalid URL or non-string value → **400** with a message naming the field.

**`email`**

- **Omitted** → unchanged. **Present** with only whitespace → clears public email. Otherwise must be a valid address (max 320 chars) or **400**.

**`resourcesJson`** (replace-only)

- **Omitted** → unchanged. **Sent** → must be a JSON **array** (possibly empty **`[]`** to clear all). Items: URL string, or object with one of `url` / `href` / `fileUrl` / `link`, optional `title` / `name` / `fileName`, optional `id`. Max **50** items. URLs normalized like social links.

**Success (`result`) — `ProfilePatchResultDto`**

```json
{
  "clubId": "1",
  "updated": true,
  "pendingReview": false,
  "profileImageUrl": "https://cdn.example/club/logo.png",
  "backgroundImageUrl": "https://cdn.example/club/background.jpg",
  "focusAreas": [
    { "icon": "code", "title": "Software", "description": "Open source and campus tooling." }
  ],
  "email": "contact@example.org",
  "socialLinks": { "website": "https://example.org", "instagram": null, "x": "https://x.com/club", "tiktok": null },
  "resources": [{ "url": "https://cdn.example/club/guide.pdf", "title": "Guide" }]
}
```

- **`focusAreas`**, **`email`**, **`socialLinks`**, **`resources`**, **`profileImageUrl`**, and **`backgroundImageUrl`** echo the published state after the call (even when `updated` is `false` because no part caused a write).
- **`updated`**: `true` if `FocusAreasJson`, `SocialLinksJson`, `Email`, `ResourcesJson`, `ProfileImageUrl`, and/or `BackgroundImageUrl` was written in that request.

**Allowed `icon` keys (whitelist)**

`target`, `star`, `award`, `sparkles`, `flag`, `compass`, `layers`, `grid`, `circle`, `badge`, `book`, `book-open`, `graduation-cap`, `lightbulb`, `brain`, `notebook`, `pencil`, `microscope`, `atom`, `calculator`, `palette`, `brush`, `music`, `mic`, `guitar`, `camera`, `video`, `film`, `clapperboard`, `headphones`, `laptop`, `code`, `cpu`, `settings`, `database`, `server`, `wifi`, `cloud`, `users`, `user-plus`, `heart`, `handshake`, `message-circle`, `megaphone`, `globe`, `map`, `briefcase`, `bar-chart`, `calendar`, `ticket`, `rocket`, `trending-up`, `clipboard`, `check-circle`

---

## 5. TypeScript-friendly types (reference)

```ts
// Club proposal reject / revision
type ReasonBody = { reason: string }
type ChangesBody = { changes: string }

// Event proposal submit (club + staff)
type EventProposalSubmitDto = {
  name: string
  dateTime: string // "YYYY-MM-DDTHH:mm:ss"
  duration: string
  attendance: string
  venue: string
  description: string
  objectives: string
  imageUrl?: string
  subEvents: Array<{
    title: string
    capacity: string
    start: string
    end: string
    date: string
  }>
  logistics: {
    avSetup: boolean
    security: boolean
    catering: boolean
    cleaning: boolean
    otherNeeds?: string
  }
  submittedByOrganization?: string
}

// Event proposal approve
type EventProposalApproveDto = {
  assignments: Array<{
    subEventIndex: number
    buildingId: string
    roomId: string
  }>
}

// Focus area (read + PATCH response)
type ClubFocusAreaItemDto = {
  icon: string
  title: string
  description: string
}

// Requirements
type ClubProposalRequirementsDto = {
  requirements: string[]
  deadline: string | null // ISO date from server
}
```

---

## 6. Student Services GET club (staff view)

- `GET /api/v1/student-services/clubs/{clubId}`  
- Returns the **`ClubDto`** from `IClubService`: **`focusAreas`**, **`socialLinks`**, flattened **`website` / `instagram` / `x` / `twitter` / `tiktok`** (null when empty), **`email`**, **`resources`** (and alias **`documents`** when non-empty).

This is richer than the public `GET /api/v1/clubs/{clubId}` envelope (which uses `about` instead of `description`); use the appropriate route for each UI.

---

## 6.0 Student Services — clubs directory list (Command Center)

- `GET /api/v1/student-services/clubs`

**Response highlights (inside `result.items`)**

Each item includes at least:

```json
{
  "id": 5,
  "name": "Robotics Club",
  "category": "General",
  "status": "Active",
  "members": 0,
  "president": null,
  "presidentId": null,
  "profileImageUrl": "/uploads/clubs/5/logo-approved-...png",
  "image": "/clubs/default.png",
  "proposedProfileImageUrl": "/uploads/clubs/5/logo-proposed-...png"
}
```

Notes:

- `profileImageUrl` and `proposedProfileImageUrl` are **optional** (may be `null`).
- If `profileImageUrl` is missing, UI should fall back to `image`.
- If `proposedProfileImageUrl` exists, UI should show an **Approve logo** action.

## 6.1 Student Services — manage club employees (officers)

These routes write to `club.ClubEmployees`. Public club detail reads them back via `GET /api/v1/clubs/{clubId}` → `officers`.

| Method | Path |
|--------|------|
| POST | `/api/v1/student-services/clubs/{clubId}/employees` |
| PATCH | `/api/v1/student-services/clubs/{clubId}/employees/{employeeId}` |
| DELETE | `/api/v1/student-services/clubs/{clubId}/employees/{employeeId}` |

### POST add employee with position

- `POST /api/v1/student-services/clubs/{clubId}/employees`
- `Content-Type: application/json`

**Body**

```json
{
  "userId": "b3c2d1e0-1111-2222-3333-444455556666",
  "positionId": "5"
}
```

- `userId` is required and must be a **GUID**.
- `positionId` is optional; when present must be a **numeric id** string.

**Success (`result`)**

```json
{
  "employeeId": "1",
  "clubId": "5",
  "userId": "b3c2d1e0-1111-2222-3333-444455556666",
  "positionId": "5"
}
```

### PATCH change employee position

- `PATCH /api/v1/student-services/clubs/{clubId}/employees/{employeeId}`
- `Content-Type: application/json`

**Body**

```json
{ "positionId": "7" }
```

- To clear a position, send `{ "positionId": null }` (or omit the property).

**Success (`result`)**: same shape as POST.

---

## 6.2 Student Services — edit club settings + logo

These routes power Student Services → Command Center → Club → Settings tab.

### PATCH update club settings (JSON)

- `PATCH /api/v1/student-services/clubs/{clubId}`
- `Content-Type: application/json`

**Body (merge semantics)**

```json
{
  "name": "string",
  "description": "string",
  "status": "Active | Inactive"
}
```

Notes:

- `name` is required by the UI; backend keeps existing when omitted/empty.
- `description` is public text shown to students (kept when omitted).
- `status`: UI values map to persisted enum:
  - `"Active"` → stored as `Approved`
  - `"Inactive"` → stored as `Archived`

**Response**

- Returns updated `ClubDto` (same shape as `GET /api/v1/student-services/clubs/{clubId}`).

### POST upload/replace club logo (multipart)

- `POST /api/v1/student-services/clubs/{clubId}/profile-image`
- `Content-Type: multipart/form-data`

**Multipart field**

- `logoFile`: image file

**Behavior**

- Saves the file under `wwwroot/uploads/clubs/{clubId}/...`
- Sets `proposedProfileImageUrl` on the club row

**Response**

- Returns `ClubDto` including `proposedProfileImageUrl`.

### POST approve the proposed logo

- `POST /api/v1/student-services/clubs/{clubId}/profile-image/approve`
- Body: `{}` (or empty)

**Behavior**

- Moves `proposedProfileImageUrl` → `profileImageUrl`
- Clears `proposedProfileImageUrl`

**Response**

- Returns updated `ClubDto` (now with `profileImageUrl` set and `proposedProfileImageUrl` cleared).

## 7. Gateway and environment

- Ensure your **API gateway** forwards the new paths if it uses an allowlist (`clubs/proposals`, `events/proposals`, `users/.../club-proposals`, etc.).
- **Database:** apply migrations including **`AddClubFocusAreasAndSocialLinksJson`** (`FocusAreasJson`, `SocialLinksJson`) and **`AddClubEmailAndResourcesJson`** (`Email`, `ResourcesJson` on `club.Clubs`).
- **Media:** the API serves `/uploads/...` via `UseStaticFiles()`; ensure gateway forwards these paths if needed.
- **Also required for recent features:** `AddClubEmployees` (officers) and `AddClubProfileImages` (`profileImageUrl`, `proposedProfileImageUrl`).

---

## 8. Quick checklist for FrontWeb

| Area | Action |
|------|--------|
| Proposals | Use canonical URLs; keep legacy aliases as fallback during rollout |
| unwrap | Read `result` / `data` from AutoWrapper |
| Club submit | Prefer `multipart/form-data` for direct file attachment uploads; use JSON only when `logoFile` and `constitutionFile` are already uploaded URLs |
| SS queues | Use `page` / `limit` / `totalCount` for proposals and event proposals |
| Event approve | Send `assignments` covering every `subEventIndex` when `subEvents.length > 0` |
| Focus areas | `PATCH` with `focusAreasJson` only for card save; send full array; max 6; icon whitelist |
| Club detail | Read `focusAreas` + contact/links from `GET /clubs/{id}`; map legacy string entries if any; use `null`/omitted fields to hide rows |
| Admin profile | `PATCH .../profile`: `socialLinks` merge-patch; optional `email`, `resourcesJson` |
| Officers | Use Student Services employee routes; `GET /clubs/{id}` returns `officers` from DB (`userId`, `positionId`) |

---

*Generated for frontend handoff. Backend implementation lives under `Services/ClubManagementService/` (controllers, `ProposalContractService`, `ClubFocusAreasJson`, `ClubSocialLinksJson`, `ClubResourcesJson`, migrations).*
