# LostFound API Documentation

Last updated: 2026-04-26

## Base URLs

- Through API Gateway: `http://13.60.31.141:500/lostfound`
- Direct service: `http://13.60.31.141:5002`

Examples below use the gateway form, so `/api/lost-and-found/items` becomes:

```text
http://13.60.31.141:500/lostfound/api/lost-and-found/items
```

## Authentication

- Public endpoints do not require a token.
- Protected endpoints require `Authorization: Bearer <jwt>`.
- Admin endpoints require role `admin` or `lost_found_leader`.

## Recommended API Surface

- Use the unified LostFound API under `/api/lost-and-found`.
- Category management is under `/api/v1/categories`.
- Legacy endpoints under `/api/v1/found-items`, `/api/v1/lost-items`, `/api/v1/admin/*` still exist, but they are hidden from Swagger and should not be used for new client work.

## Common Values

### Item type

- `lost`
- `found`

### Public status (`status` on item responses)

**Query / PATCH input** (case-insensitive; accepted synonyms):

- `active`
- `pending`, `pending verification`, `pending_verification` (all map to pending verification)

**JSON response values** (always snake_case):

- `active`
- `pending_verification`

### Admin status (`adminStatus` when returned on admin write responses)

**Query filter / PATCH input** (case-insensitive):

- `pending`
- `received`
- `delivered`

**JSON response values** (snake_case when admin fields are included):

- `pending`
- `received`
- `delivered`

### Condition

- `good`
- `fair`
- `damaged`

### Claim type

- `owner`
- `finder`

## Error Format

Most validation errors use:

```json
{
  "error": "VALIDATION_ERROR",
  "message": "Validation failed.",
  "details": {
    "fieldName": [
      "field-specific message"
    ]
  }
}
```

Common error codes:

- `VALIDATION_ERROR`
- `UNAUTHORIZED`
- `NOT_FOUND`
- `DUPLICATE_CLAIM`

## Response Shape

Main item responses use:

```json
{
  "id": 12,
  "type": "found",
  "title": "AirPods Pro Case",
  "location": "Main Building, Lobby",
  "categoryId": 3,
  "categoryName": "Electronics",
  "status": "pending_verification",
  "adminStatus": "pending",
  "description": "White charging case with scratches.",
  "referenceNumber": "LF-000012",
  "datePosted": "2026-04-25",
  "daysAgo": 0,
  "postedAt": "2026-04-25T09:30:00Z",
  "image": "https://...",
  "images": [
    "https://..."
  ],
  "postedByUserId": "user-id",
  "collectionPlace": "Lost and Found Office",
  "locationType": "building",
  "buildingCode": "MB",
  "isRoom": "true",
  "roomOrArea": "Lobby",
  "campusLocation": null,
  "building": "Main Building",
  "floor": "1",
  "roomArea": null,
  "lastKnownLocation": null,
  "dateLost": "2026-04-24",
  "timeLost": "14:30",
  "contactName": "Ali Mammadov",
  "contactPhone": "+994501112233",
  "storageBinId": "B-12",
  "condition": "good",
  "adminNotes": "Received at desk",
  "reportedColor": "white",
  "intakePhotoUrl": "https://...",
  "confirmAccuracy": true,
  "claimantName": null,
  "claimantStudentId": null,
  "studentIdVerified": null,
  "handoverProofUrl": null
}
```

The example above shows **all** fields; public `GET /api/lost-and-found/items/{id}` returns `null` for admin-only properties (`adminStatus`, `storageBinId`, `condition`, `adminNotes`, `reportedColor`, `intakePhotoUrl`, `confirmAccuracy`, `claimantName`, `claimantStudentId`, `studentIdVerified`, `handoverProofUrl`) even when the caller is an admin. Those fields are populated on **admin workflow write** responses (manual create, patch, confirm receipt, complete delivery).

### Public catalog visibility

- Callers **without** roles `admin` or `lost_found_leader`: `GET /api/lost-and-found/items`, `GET .../items/{id}`, and `GET .../items/{id}/timeline` **exclude** items whose `adminStatus` is `received` or `delivered`. Those paths return **404** for a terminal item by id. `POST .../items/{id}/claims` also returns **404** for terminal items.
- Callers **with** `admin` or `lost_found_leader`: list, detail, and timeline include **all** items.
- `GET /api/lost-and-found/stats` counts only items **not** in admin `received` or `delivered` (matches the public board), for every caller.

Fields populated only on admin write responses:

- `adminStatus`
- `storageBinId`
- `condition`
- `adminNotes`
- `reportedColor`
- `intakePhotoUrl`
- `confirmAccuracy`
- `claimantName`
- `claimantStudentId`
- `studentIdVerified`
- `handoverProofUrl`

## Unified LostFound Endpoints

### 1. List items

- Method: `GET`
- Path: `/api/lost-and-found/items`
- Auth: optional

Query params:

- `q`: free-text search
- `search`: alias of `q`
- `type`: `lost`, `found`, or `all`
- `postedBy`: use `me` to limit to the authenticated user (requires JWT)
- `categoryId`: optional integer filter
- `status`: `active` or `pending` / `pending_verification` / `pending verification` (filter)
- `adminStatus`: `pending`, `received`, `delivered` (filter; case-insensitive)
- `page`: default `1`
- `limit`: default `10`, max `100`

Example:

```http
GET /lostfound/api/lost-and-found/items?type=found&status=pending&page=1&limit=10
```

Response:

```json
{
  "items": [
    {
      "id": 12,
      "type": "found",
      "title": "AirPods Pro Case",
      "location": "Main Building, Lobby",
      "categoryId": 3,
      "categoryName": "Electronics",
      "status": "pending_verification",
      "referenceNumber": "LF-000012",
      "datePosted": "2026-04-25",
      "daysAgo": 0,
      "postedAt": "2026-04-25T09:30:00Z",
      "image": "https://...",
      "images": [
        "https://..."
      ]
    }
  ],
  "total": 1
}
```

### 2. Get item by id

- Method: `GET`
- Path: `/api/lost-and-found/items/{id}`
- Auth: optional

Non-admin callers receive **404** for items in admin `received` or `delivered`. Admins can open any item. Public GET responses do not populate admin-only fields (see **Response Shape**).

### 3. Get stats

- Method: `GET`
- Path: `/api/lost-and-found/stats`
- Auth: optional

Counts exclude items in admin `received` or `delivered` (same scope as the public catalog). See **Response Shape → Public catalog visibility**.

Response:

```json
{
  "total": 120,
  "active": 48,
  "pendingVerification": 72
}
```

### 4. User lost report

- Method: `POST`
- Path: `/api/lost-and-found/reports/lost`
- Auth: optional
- Content types: `application/json`, `multipart/form-data`

This endpoint is strict. Only these fields are accepted:

- `type`
- `itemName`
- `categoryId`
- `description`
- `location`
- `dateLost`
- `timeLost`
- `contactName`
- `contactPhone`
- `status`
- `files` or `files[]` when using multipart

Rules:

- If `type` is sent, it must be `lost`.
- If `status` is sent, it must be `pending`.
- Extra fields are rejected.
- `imageUrl` and `imageUrls` are not supported.
- Attached image files are allowed only as multipart file fields.

Working JSON example:

```json
{
  "type": "lost",
  "itemName": "Black Wallet",
  "categoryId": 2,
  "description": "Black leather wallet with student ID and bank cards.",
  "location": "Main Building lobby",
  "dateLost": "2026-04-24",
  "timeLost": "14:30",
  "contactName": "Ali Mammadov",
  "contactPhone": "+994501112233",
  "status": "pending"
}
```

Success response:

```json
{
  "id": 25,
  "referenceNumber": "LF-000025",
  "status": "pending_verification",
  "message": "Lost item report submitted for review."
}
```

### 5. User found report

- Method: `POST`
- Path: `/api/lost-and-found/reports/found`
- Auth: optional
- Content types: `application/json`, `multipart/form-data`

Rules:

- `type` is forced to `found` by the API.
- `imageUrl` and `imageUrls` are rejected. Use attached files only.
- At least one image file is required.
- `collectionPlace` is required.
- `locationType` is required.
- If `locationType=building`, one of `buildingCode` or `building` is required, and one of `roomOrArea` or `roomArea` is required.
- If `locationType=campus`, `campusLocation` is required.
- If `locationType=other`, `location` is required.

Recommended multipart example:

```bash
curl -X POST "http://13.60.31.141:500/lostfound/api/lost-and-found/reports/found" \
  -F "itemName=AirPods Pro Case" \
  -F "categoryId=3" \
  -F "description=White charging case with small scratches" \
  -F "locationType=building" \
  -F "buildingCode=MB" \
  -F "roomOrArea=Lobby" \
  -F "collectionPlace=Lost and Found Office" \
  -F "files=@/path/to/airpods.jpg"
```

### 6. Manual item create

- Method: `POST`
- Path: `/api/lost-and-found/items`
- Auth: `admin`, `lost_found_leader`
- Content types: `application/json`, `multipart/form-data`

Allowed fields:

- `type`
- `itemName`
- `categoryId`
- `description`
- `location`
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
- `files` or `files[]` when using multipart

Rules:

- `type` is required and must be `lost` or `found`.
- Extra fields are rejected.
- For manual `found` create, the same found-report rules apply, including required photo and `collectionPlace`.

Working lost JSON example:

```json
{
  "type": "lost",
  "itemName": "Black Wallet",
  "categoryId": 2,
  "description": "Black leather wallet with student ID and bank cards.",
  "location": "Main Building lobby",
  "dateLost": "2026-04-24",
  "timeLost": "14:30",
  "contactName": "Ali Mammadov",
  "contactPhone": "+994501112233",
  "status": "pending",
  "adminStatus": "pending"
}
```

### 7. Patch item

- Method: `PATCH`
- Path: `/api/lost-and-found/items/{id}`
- Auth: `admin`, `lost_found_leader`
- Content type: `application/json`

Allowed body:

```json
{
  "adminStatus": "received",
  "title": "Black Wallet",
  "description": "Updated description",
  "status": "active",
  "storageBinId": "B-12",
  "condition": "good",
  "adminNotes": "Checked by team",
  "reportedColor": "black"
}
```

### 8. Upload image

- Method: `POST`
- Path: `/api/lost-and-found/upload`
- Auth: optional
- Content type: `multipart/form-data`

Accepted file field names:

- `file`
- `photo`
- `image`
- `imageFile`

Success response:

```json
{
  "url": "https://..."
}
```

### 9. Notify owner

- Method: `POST`
- Path: `/api/lost-and-found/items/{id}/notify`
- Auth: `admin`, `lost_found_leader`
- Content type: `application/json`

Body:

```json
{
  "channel": "email",
  "message": "Your item may have been found."
}
```

Response:

```json
{
  "queued": true
}
```

### 10. Create claim

- Method: `POST`
- Path: `/api/lost-and-found/items/{id}/claims`
- Auth: any authenticated user
- Content type: `application/json`

Body:

```json
{
  "claimType": "owner",
  "message": "This wallet belongs to me."
}
```

Rules:

- `claimType` must be `owner` or `finder`.
- The same user cannot submit the same claim type twice for the same item.

Success response:

```json
{
  "claimId": 8,
  "status": "pending_review"
}
```

### 11. Confirm receipt

- Method: `POST`
- Path: `/api/lost-and-found/items/{id}/confirm-receipt`
- Auth: `admin`, `lost_found_leader`
- Content type: `multipart/form-data`

Body fields:

- `storageBinId`
- `condition`
- `confirmAccuracy`
- `adminNotes`
- `intakePhotoFile`

Rules:

- `condition` must be `good`, `fair`, or `damaged`.
- `intakePhotoFile` must be PNG or JPG when supplied.

After success, the item has `adminStatus` **`received`** and public `status` **`active`**. It is then **hidden** from non-admin `GET` list/detail/timeline (see **Public catalog visibility**).

Example:

```bash
curl -X POST "http://13.60.31.141:500/lostfound/api/lost-and-found/items/25/confirm-receipt" \
  -H "Authorization: Bearer <jwt>" \
  -F "storageBinId=B-12" \
  -F "condition=good" \
  -F "confirmAccuracy=true" \
  -F "adminNotes=Item received and checked" \
  -F "intakePhotoFile=@/path/to/intake.jpg"
```

### 12. Complete delivery

- Method: `POST`
- Path: `/api/lost-and-found/items/{id}/complete-delivery`
- Auth: `admin`, `lost_found_leader`
- Content type: `multipart/form-data`

Body fields:

- `claimantName`
- `claimantStudentId`
- `studentIdVerified`
- `handoverProofFile`

Rules:

- `handoverProofFile` must be PNG or JPG when supplied.

After success, `adminStatus` becomes **`delivered`**. The item remains excluded from non-admin catalog reads and from new claims.

### 13. Get item claims

- Method: `GET`
- Path: `/api/lost-and-found/items/{id}/claims`
- Auth: `admin`, `lost_found_leader`

Response:

```json
{
  "claims": [
    {
      "id": 8,
      "claimType": "owner",
      "message": "This wallet belongs to me.",
      "status": "pending_review",
      "userId": "user-id",
      "createdAt": "2026-04-25T10:00:00Z"
    }
  ]
}
```

### 14. Get item timeline

- Method: `GET`
- Path: `/api/lost-and-found/items/{id}/timeline`
- Auth: optional

Response:

```json
{
  "events": [
    {
      "at": "2026-04-25T09:30:00Z",
      "label": "Report submitted",
      "done": true
    },
    {
      "at": null,
      "label": "Owner notified",
      "done": false
    },
    {
      "at": null,
      "label": "Delivery completed",
      "done": false
    }
  ]
}
```

## Category Endpoints

### 1. List categories

- Method: `GET`
- Path: `/api/v1/categories`
- Auth: optional

### 2. Get category by id

- Method: `GET`
- Path: `/api/v1/categories/{id}`
- Auth: optional

### 3. Create category

- Method: `POST`
- Path: `/api/v1/categories`
- Auth: `admin`, `lost_found_leader`

Body:

```json
{
  "name": "Electronics"
}
```

### 4. Update category

- Method: `PUT`
- Path: `/api/v1/categories/{id}`
- Auth: `admin`, `lost_found_leader`

Body:

```json
{
  "name": "Documents"
}
```

### 5. Delete category

- Method: `DELETE`
- Path: `/api/v1/categories/{id}`
- Auth: `admin`, `lost_found_leader`

## Image Handling

- LostFound item images must be uploaded as attached files.
- `imageUrl` and `imageUrls` are rejected on unified LostFound create/report flows.
- Supported image formats are PNG and JPG.
- Files are uploaded through `IImageStorageService`, which is configured to use S3 in this service.

## Notes

- User lost report is intentionally narrow and rejects admin-only workflow fields.
- Manual create is for staff/admin flows.
- `postedBy=me` on item listing requires authentication.
- `referenceNumber` is assigned by the backend after create.
