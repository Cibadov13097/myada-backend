# LostFound API Documentation (Gateway First)

Last updated: 2026-04-29

This document covers only the LostFound service (`Services/LostFoundService`) and is aligned with:
- `Services/LostFoundService/LostFoundService.API/Controllers/LostAndFoundController.cs`
- `Services/LostFoundService/LostFoundService.API/Controllers/LostItemController.cs`
- `Services/LostFoundService/LostFoundService.API/Controllers/FoundItemController.cs`
- `Services/LostFoundService/LostFoundService.API/Controllers/CategoryController.cs`
- `Services/LostFoundService/LostFoundService.API/Controllers/Admin/AdminLostItemController.cs`
- `Services/LostFoundService/LostFoundService.API/Controllers/Admin/AdminFoundItemController.cs`

## Base URLs

- Gateway (recommended): `https://myada.site`
- Gateway prefix for LostFound: `/lostfound`
- Effective gateway base: `https://myada.site/lostfound`

Direct service (diagnostics only):
- Production direct: `http://13.60.31.141:5002`
- Local direct: `http://localhost:5002`

Swagger:
- Gateway: `https://myada.site/lostfound/swagger/index.html`
- Direct: `http://13.60.31.141:5002/swagger/index.html`

Important gateway rule:
- Correct: `https://myada.site/lostfound/api/lost-and-found/items`
- Wrong: `https://myada.site/api/lost-and-found/items` (gateway 404)

## Authentication and Roles

Bearer header for protected endpoints:

```http
Authorization: Bearer <accessToken>
```

Roles used by this service:
- `admin`
- `lost_found_leader`

Auth model:
- Public: most modern read/report/upload endpoints under `/api/lost-and-found`
- Auth required: claim submit and legacy `users/me` endpoints
- Admin/lost_found_leader: modern admin workflow endpoints and most legacy admin endpoints

## API Families

Modern API:
- `/api/lost-and-found/*`

Legacy APIs:
- `/api/v1/lost-items/*`
- `/api/v1/found-items/*`
- `/api/v1/categories/*`
- `/api/v1/admin/lost-items/*`
- `/api/v1/admin/found-items/*`

---

## 1) Modern API: `/api/lost-and-found`

Gateway prefix for all paths in this section:
- `https://myada.site/lostfound`

### 1.1 Endpoint Matrix

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/lost-and-found/items` | Optional |
| `GET` | `/api/lost-and-found/items/{id:int}` | Optional |
| `GET` | `/api/lost-and-found/stats` | Optional |
| `POST` | `/api/lost-and-found/reports/lost` | Optional |
| `POST` | `/api/lost-and-found/reports/found` | Optional |
| `PATCH` | `/api/lost-and-found/items/{id:int}` | `admin` or `lost_found_leader` |
| `POST` | `/api/lost-and-found/items` | `admin` or `lost_found_leader` |
| `POST` | `/api/lost-and-found/upload` | Optional |
| `POST` | `/api/lost-and-found/items/{id:int}/notify` | `admin` or `lost_found_leader` |
| `POST` | `/api/lost-and-found/items/{id:int}/claims` | Any authenticated user |
| `POST` | `/api/lost-and-found/items/{id:int}/confirm-receipt` | `admin` or `lost_found_leader` |
| `POST` | `/api/lost-and-found/items/{id:int}/complete-delivery` | `admin` or `lost_found_leader` |
| `GET` | `/api/lost-and-found/items/{id:int}/claims` | `admin` or `lost_found_leader` |
| `GET` | `/api/lost-and-found/items/{id:int}/timeline` | Optional |

### 1.2 Public catalog vs admin visibility

For the unified API, **admin workflow** uses `adminStatus`: `pending` → `received` (intake) → `delivered` (handover).

- **Callers without `admin` or `lost_found_leader`** (no token, or a token without those roles):  
  - `GET /api/lost-and-found/items` includes items across all public and admin workflow states unless the caller explicitly filters by `status` or `adminStatus`.  
  - `GET /api/lost-and-found/items/{id}` and `GET /api/lost-and-found/items/{id}/timeline` return **404** for delivered items.  
  - `POST /api/lost-and-found/items/{id}/claims` returns **404** for delivered items.
- **`admin` / `lost_found_leader`**: list, detail, and timeline include **all** items, including `received` and `delivered`.
- **`GET /api/lost-and-found/stats`**: `total`, `active`, and `pendingVerification` count only items **not** in admin `delivered` (same notion as the public board), regardless of caller role.

### 1.3 List Items

`GET /api/lost-and-found/items`

Query params:
- `q` or `search`: text search (`q` takes priority)
- `type`: `lost`, `found`, `all`
- `postedBy`: `me` to filter current user posts (requires `Authorization`; otherwise 401)
- `categoryId`: optional category id (integer)
- `status` (filter): `active` or any of `pending`, `pending verification`, `pending_verification` (maps to pending verification)
- `adminStatus` (filter): `pending`, `received`, `delivered` (case-insensitive)
- `page`: default `1`, min `1`
- `limit`: default `10`, clamped to `1..100`

Response:
- `{ items: [...], total: number }`
- By default, the list includes items across all statuses. Use `status` or `adminStatus` query params only when you want to narrow the result set.

Each item includes a public `status` string; see **§7 Enums and Accepted Values** for response values. Admin-only fields on list responses stay omitted unless noted otherwise (list uses the public projection).

### 1.4 Get Item by Id

`GET /api/lost-and-found/items/{id}`

Response:
- Item object with public fields.
- Admin-only fields (`adminStatus`, `storageBinId`, `condition`, etc.) are included only on **admin write** responses (manual create, patch, confirm receipt, complete delivery), not on this public GET—even for admins. Use an admin write response or a future admin-read endpoint if you need those fields on read by id.

For non-admin callers, delivered items return **404**.

### 1.5 Stats

`GET /api/lost-and-found/stats`

Counts match the **public board** (items excluding admin `received` and `delivered`). See §1.2.

Response:

```json
{
  "total": 0,
  "active": 0,
  "pendingVerification": 0
}
```

### 1.6 Create Lost Report

`POST /api/lost-and-found/reports/lost`

Accepted content types:
- `application/json`
- `multipart/form-data`

Minimum required fields:
- `itemName`
- `categoryId` (integer)

Response:

```json
{
  "id": 123,
  "referenceNumber": "LF-000123",
  "status": "pending_verification",
  "message": "Lost item report submitted for review."
}
```

### 1.7 Create Found Report

`POST /api/lost-and-found/reports/found`

Accepted content types:
- `application/json`
- `multipart/form-data`

Required for found reports:
- `itemName`
- `categoryId` (integer)
- `locationType`
- `collectionPlace`
- at least one photo (uploaded file or image URL)

Extra validation for `locationType`:
- `building`: requires `buildingCode` or `building`, and `roomOrArea` or `roomArea`
- `campus`: requires `campusLocation`
- `other`: requires `location`

Response:

```json
{
  "id": 124,
  "referenceNumber": "LF-000124",
  "status": "pending_verification",
  "message": "Found item report submitted for review."
}
```

### 1.8 Manual Admin Create

`POST /api/lost-and-found/items` (admin/lost_found_leader)

Accepted content types:
- `application/json`
- `multipart/form-data`

Additional required field:
- `type` (`lost` or `found`)

### 1.9 Admin Patch Item

`PATCH /api/lost-and-found/items/{id}`

Body fields (all optional; strings are case-insensitive):
- `adminStatus`: `pending`, `received`, `delivered`
- `title`
- `description`
- `status`: `active` or any of `pending`, `pending verification`, `pending_verification`
- `storageBinId`
- `condition`: `good`, `fair`, `damaged`
- `adminNotes`
- `reportedColor`

### 1.10 Upload Endpoint

`POST /api/lost-and-found/upload`

Content type:
- `multipart/form-data`

Accepted file form keys:
- `file`
- `photo`
- `image`
- `imageFile`

Response:

```json
{
  "url": "https://..."
}
```

### 1.11 Claims

Create claim:
- `POST /api/lost-and-found/items/{id}/claims`
- auth required
- body:

```json
{
  "claimType": "owner",
  "message": "Optional proof details"
}
```

Allowed `claimType`:
- `owner`
- `finder`

Get item claims:
- `GET /api/lost-and-found/items/{id}/claims`
- admin/lost_found_leader only

### 1.12 Notify Owner

`POST /api/lost-and-found/items/{id}/notify`

Auth:
- admin/lost_found_leader

Body:

```json
{
  "channel": "sms",
  "message": "Optional message"
}
```

### 1.13 Confirm Receipt

`POST /api/lost-and-found/items/{id}/confirm-receipt`

Content type:
- `multipart/form-data`

Fields:
- `storageBinId` (required)
- `condition` (required: `good|fair|damaged`)
- `intakePhotoFile` (optional file)
- `confirmAccuracy` (bool)
- `adminNotes` (optional)

On success the item is updated to `adminStatus` **`received`** and public `status` **`active`**, and it **drops off** the public list/detail/timeline for non-admin callers (see §1.2).

### 1.14 Complete Delivery

`POST /api/lost-and-found/items/{id}/complete-delivery`

Content type:
- `multipart/form-data`

Fields:
- `claimantName` (optional)
- `claimantStudentId` (optional)
- `studentIdVerified` (bool)
- `handoverProofFile` (optional file)

On success, `adminStatus` becomes **`delivered`**; the item stays out of the public catalog and cannot receive new claims.

### 1.15 Timeline

`GET /api/lost-and-found/items/{id}/timeline`

Non-admin callers receive **404** for delivered items (same as get-by-id).

Response:

```json
{
  "events": [
    { "at": "2026-04-23T12:00:00Z", "label": "Report submitted", "done": true },
    { "at": null, "label": "Owner notified", "done": false },
    { "at": null, "label": "Delivery completed", "done": false }
  ]
}
```

### 1.16 Modern Report Payload Reference

Common payload keys accepted by modern create endpoints:
- `type`
- `itemName`
- `categoryId`
- `description`
- `location` (nullable)
- `locationType`
- `buildingCode`
- `isRoom`
- `roomOrArea`
- `campusLocation`
- `building`
- `floor`
- `roomArea`
- `lastKnownLocation`
- `dateLost`
- `timeLost`
- `contactName`
- `contactPhone`
- `collectionPlace`
- `status`
- `adminStatus`
- `storageBinId`
- `condition`
- `adminNotes`
- `reportedColor`
- `confirmAccuracy`
- `claimantName`
- `claimantStudentId`
- `studentIdVerified`

Image inputs supported in modern create:
- Uploaded files (`multipart/form-data`):
  - any non-reserved uploaded files are treated as report photos
  - reserved file names:
    - `intakePhoto`, `intakePhotoFile`
    - `handoverProof`, `handoverProofFile`
- URL body fields are rejected on modern create/report endpoints.
- Use attached files on the request itself, or upload first with `/api/lost-and-found/upload` and store that returned URL only in your own client state.

Location rule:
- `location` is nullable.
- If `locationType=other`, send `location` as string.
- For multipart forms, backend also checks `customLocation` fallback.

### 1.17 Admin Page Endpoint Usage

Use the **modern** `/api/lost-and-found/*` endpoints for all new admin pages. Only use the legacy admin endpoints in §5-§6 if you are intentionally keeping old screens alive during migration.

Recommended admin page mapping:

| Admin page / UI surface | Endpoint(s) it should use | Notes |
|---|---|---|
| Admin dashboard summary cards | `GET /api/lost-and-found/items?adminStatus=pending`, `GET /api/lost-and-found/items?adminStatus=received`, `GET /api/lost-and-found/items?adminStatus=delivered` | Use `total` from each filtered list for true admin workflow counts. Do **not** rely on `/stats` for received/delivered counts because `/stats` follows public-board visibility rules. |
| Admin dashboard activity feed / recent queue | `GET /api/lost-and-found/items?page=1&limit=...&adminStatus=pending` | Add `type`, `q`, or `categoryId` filters if the dashboard supports them. |
| Admin items list page | `GET /api/lost-and-found/items` | This is the main admin table endpoint. Wire filters for `q`, `type`, `categoryId`, `status`, `adminStatus`, `page`, and `limit`. |
| Admin item detail page | `GET /api/lost-and-found/items/{id}`, `GET /api/lost-and-found/items/{id}/timeline`, `GET /api/lost-and-found/items/{id}/claims` | Detail page should load the item, timeline, and claims together. Claims endpoint is admin-only and belongs on the admin detail/claims tab. |
| Admin create item page | `POST /api/lost-and-found/items`, optionally `POST /api/lost-and-found/upload` | Use manual admin create for staff-entered lost/found items. Use `/upload` only if your UI uploads files ahead of final submit. |
| Admin edit item form | `PATCH /api/lost-and-found/items/{id}` | Use for title/description/status/adminStatus/storage bin/condition/admin notes/reported color edits. |
| Admin notify owner modal/action | `POST /api/lost-and-found/items/{id}/notify` | This should be a dedicated admin action on the item detail or row action menu. |
| Admin confirm receipt / intake page or modal | `POST /api/lost-and-found/items/{id}/confirm-receipt` | Use multipart form with `storageBinId`, `condition`, optional `intakePhotoFile`, `confirmAccuracy`, and `adminNotes`. |
| Admin complete delivery / handover page or modal | `POST /api/lost-and-found/items/{id}/complete-delivery` | Use multipart form with claimant fields and optional handover proof file. |
| Admin category management page | `GET /api/v1/categories`, `GET /api/v1/categories/{id}`, `POST /api/v1/categories`, `PUT /api/v1/categories/{id}`, `DELETE /api/v1/categories/{id}` | Categories are still managed through the legacy categories API. This is the correct place to use those endpoints. |

### 1.18 Admin Workflow Rules

To make sure the admin pages actually cover the full workflow, wire them like this:

1. Admin list page:
- Must use `GET /api/lost-and-found/items`.
- Should expose `adminStatus` filters for `pending`, `received`, and `delivered`.
- Should expose pagination using `page` and `limit`.

2. Admin detail page:
- Must use `GET /api/lost-and-found/items/{id}` for the core record.
- Must use `GET /api/lost-and-found/items/{id}/timeline` for workflow history.
- Must use `GET /api/lost-and-found/items/{id}/claims` if claims are shown or reviewed by admins.

3. Admin actions:
- `Notify owner` button -> `POST /api/lost-and-found/items/{id}/notify`
- `Confirm receipt` button -> `POST /api/lost-and-found/items/{id}/confirm-receipt`
- `Complete delivery` button -> `POST /api/lost-and-found/items/{id}/complete-delivery`
- Inline edit or edit form save -> `PATCH /api/lost-and-found/items/{id}`

4. Admin create flow:
- If the form uploads images immediately, call `POST /api/lost-and-found/upload` first.
- If the form submits everything together, call `POST /api/lost-and-found/items` with multipart form data and skip the separate upload endpoint.

5. Dashboard caveat:
- `GET /api/lost-and-found/stats` is useful for the public-facing board summary.
- It is **not** a complete admin dashboard summary because it excludes admin `delivered` items from the counts.

6. Legacy compatibility:
- If there are still separate legacy admin lost-item and found-item pages, those pages may keep using `/api/v1/admin/lost-items/*` and `/api/v1/admin/found-items/*`.
- Do not mix legacy admin endpoints into new unified admin pages unless the page is explicitly a migration shim.

---

## 2) Legacy User API: Lost Items

Base path:
- `/api/v1/lost-items`

### 2.1 Endpoint Matrix

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/lost-items` | Optional |
| `GET` | `/api/v1/lost-items/{id}` | Optional |
| `GET` | `/api/v1/lost-items/users/me` | Required |
| `GET` | `/api/v1/lost-items/users/me/{id}` | Required |
| `POST` | `/api/v1/lost-items` | Required |
| `PUT` | `/api/v1/lost-items/{id}` | Required |
| `DELETE` | `/api/v1/lost-items/{id}` | Required |

### 2.2 Create/Update Payload (multipart)

Form fields:
- `title`
- `description`
- `buildingId` (optional)
- `roomId` (optional)
- `categoryId`
- `imageFile` (required on create)

Notes:
- `buildingId` and `roomId` must be provided together when used.
- File field aliases are accepted by controller fallback: `imageFile`, `image`, `photo`.

---

## 3) Legacy User API: Found Items

Base path:
- `/api/v1/found-items`

### 3.1 Endpoint Matrix

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/found-items` | Optional |
| `GET` | `/api/v1/found-items/{id}` | Optional |
| `GET` | `/api/v1/found-items/users/me` | Required |
| `GET` | `/api/v1/found-items/users/me/{id}` | Required |
| `POST` | `/api/v1/found-items` | Required |
| `PUT` | `/api/v1/found-items/{id}` | Required |
| `DELETE` | `/api/v1/found-items/{id}` | Required |

### 3.2 Create/Update Payload (multipart)

Form fields:
- `description`
- `location` (nullable)
- `buildingId` (optional)
- `roomId` (optional)
- `status` (enum value)
- `categoryId`
- `imageFile` (required on create)

Notes:
- `buildingId` and `roomId` must be provided together when used.
- `location` max length: 400.
- File field aliases are accepted by controller fallback: `imageFile`, `image`, `photo`.

---

## 4) Categories API

Base path:
- `/api/v1/categories`

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/categories` | Optional |
| `GET` | `/api/v1/categories/{id:int}` | Optional |
| `POST` | `/api/v1/categories` | `admin` or `lost_found_leader` |
| `PUT` | `/api/v1/categories/{id:int}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/categories/{id:int}` | `admin` or `lost_found_leader` |

Create/Update body:

```json
{
  "name": "Electronics"
}
```

---

## 5) Legacy Admin API: Lost Items

Base path:
- `/api/v1/admin/lost-items`

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/admin/lost-items?includeDeleted=false` | `admin` or `lost_found_leader` |
| `GET` | `/api/v1/admin/lost-items/{id}` | `admin` |
| `POST` | `/api/v1/admin/lost-items` | `admin` or `lost_found_leader` |
| `PUT` | `/api/v1/admin/lost-items/{id}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/admin/lost-items/{id}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/admin/lost-items/{id}/force` | `admin` |

Create/update use multipart form payload similar to legacy user lost-item API, with image upload handled through S3 service.

---

## 6) Legacy Admin API: Found Items

Base path:
- `/api/v1/admin/found-items`

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/admin/found-items?includeDeleted=false` | `admin` or `lost_found_leader` |
| `GET` | `/api/v1/admin/found-items/{id}` | `admin` |
| `POST` | `/api/v1/admin/found-items` | `admin` or `lost_found_leader` |
| `PUT` | `/api/v1/admin/found-items/{id}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/admin/found-items/{id}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/admin/found-items/{id}/force` | currently no `[Authorize]` attribute in controller code |

Create/update use multipart form payload similar to legacy user found-item API, with image upload handled through S3 service.

---

## 7) Enums and Accepted Values

Modern API accepted values:
- `type`: `lost`, `found`
- public `status`: `active`, `pending`, `pending verification`, `pending_verification`
- `adminStatus`: `pending`, `received`, `delivered`
- `condition`: `good`, `fair`, `damaged`
- `claimType`: `owner`, `finder`

Modern API response formatting (JSON):
- Public `status` is always **`active`** or **`pending_verification`** (snake_case).
- When admin fields are included (admin write responses), `adminStatus` is **`pending`**, **`received`**, or **`delivered`** (snake_case).
- After **confirm receipt**, the API sets public `status` to **`active`** and `adminStatus` to **`received`** (item then disappears from non-admin list/detail).

---

## 8) S3 Image Behavior

Image storage implementation:
- `IImageStorageService` is backed by `S3ImageStorageService`
- Supported extensions: `.png`, `.jpg`, `.jpeg`

Persistence behavior for modern create:
- uploaded image files are sent to S3 and resulting URLs are stored
- first image becomes `PrimaryImageUrl`
- all images are written into `LostFoundItemImages`

Relevant runtime config keys:
- `S3__BucketName`
- `S3__Region`
- `S3__AccessKey`
- `S3__SecretKey`
- `S3__ServiceUrl` (optional for non-AWS S3 compatible storage)
- `S3__ForcePathStyle`
- `S3__PublicBaseUrl`
- `S3__KeyPrefix`
- `S3__AutoCreateBucket`

---

## 9) Common Error Shape (Modern API)

```json
{
  "error": "VALIDATION_ERROR",
  "message": "Validation failed.",
  "details": {
    "fieldName": ["reason"]
  }
}
```

Legacy endpoints may return service-specific exception shapes.

---

## 10) Quick Examples

Create found report with attached image:

```http
POST /lostfound/api/lost-and-found/reports/found
Content-Type: multipart/form-data
```

Form fields:
- `itemName`: `Black Wallet`
- `categoryId`: `5`
- `description`: `Found near cafeteria.`
- `locationType`: `other`
- `location`: `Main cafeteria entrance`
- `collectionPlace`: `Security Desk A`
- `files`: `<binary>`

Upload one image separately:

```http
POST /lostfound/api/lost-and-found/upload
Content-Type: multipart/form-data
```

Form field:
- `imageFile`: `<binary>`
