# Lost & Found Frontend API Documentation

Last updated: 2026-04-19

## Base URLs

- API Gateway (recommended): `http://51.20.193.29:5000`
- Gateway route prefix for LostFound service: `/lostfound`
- LostFound API base path: `/api/lost-and-found`
- Direct service (diagnostics only):
  - Production: `http://51.20.193.29:5002`
  - Local: `http://localhost:5002`

Frontend should use:

- **`http://51.20.193.29:5000/lostfound/api/lost-and-found`**

## Common 404 Mistake (Important)

If you call through gateway (`:5000`), you must include `/lostfound`.

- Correct: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items`
- Wrong: `http://51.20.193.29:5000/api/lost-and-found/items` (returns 404 on gateway)

---

## Authentication

- Public endpoints: list, detail, stats, reports, upload, timeline
- User-auth endpoints: claims, `postedBy=me`
- Admin endpoints: update workflow, manual create, notify, confirm receipt, complete delivery, list claims
- Admin roles accepted: `admin`, `lost_found_leader`

---

## Core Item Response Shape

```json
{
  "id": 12,
  "type": "lost",
  "title": "Black Wallet",
  "location": "main, corridor",
  "category": "Personal Items",
  "status": "Pending Verification",
  "adminStatus": "Pending",
  "description": "Leather wallet with student card",
  "referenceNumber": "LF-000012",
  "datePosted": "2026-04-13",
  "daysAgo": 0,
  "postedAt": "2026-04-13T09:25:10.0000000Z",
  "image": "https://host/uploads/lost-and-found/file.jpg",
  "images": [
    "https://host/uploads/lost-and-found/file.jpg"
  ],
  "postedByUserId": "user-123",
  "collectionPlace": null,
  "locationType": "building",
  "buildingCode": "main",
  "isRoom": "no",
  "roomOrArea": "corridor",
  "campusLocation": null,
  "building": null,
  "floor": null,
  "roomArea": null,
  "lastKnownLocation": null,
  "dateLost": "2026-04-12",
  "timeLost": "14:30",
  "contactName": "John",
  "contactPhone": "050XXXXXXX",
  "storageBinId": "BIN-402-A",
  "condition": "good",
  "adminNotes": "verified",
  "reportedColor": "Black",
  "intakePhotoUrl": "https://host/...",
  "confirmAccuracy": true,
  "claimantName": null,
  "claimantStudentId": null,
  "studentIdVerified": null,
  "handoverProofUrl": null
}
```

### Value Notes

- `type`: `lost | found`
- `status`: `Active | Pending Verification`
- `adminStatus`: `Pending | Received | Delivered`
- `condition`: `good | fair | damaged`
- Category mapping in response:
  - `Clothing -> Personal Items`
  - `Other -> Accessories`

---

## Endpoints

## 1) List Items

- Method: `GET`
- URL: `/items`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items`
- Auth: Optional

Query params:

- `q` or `search` (if both sent, backend uses `q`)
- `type=lost|found|all`
- `postedBy=me` (requires auth)
- `category` (`All Items` is treated as no filter)
- `status=active|pending`
- `adminStatus=Pending|Received|Delivered`
- `page` (default `1`)
- `limit` (default `10`, max `100`)

Response:

```json
{
  "items": [],
  "total": 0
}
```

---

## 2) Get Item By ID

- Method: `GET`
- URL: `/items/{id}`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items/{id}`
- Auth: Optional

`404` response:

```json
{
  "error": "NOT_FOUND",
  "message": "Item 99 was not found.",
  "details": null
}
```

---

## 3) Stats

- Method: `GET`
- URL: `/stats`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/stats`
- Auth: Optional

Response:

```json
{
  "total": 0,
  "active": 0,
  "pendingVerification": 0
}
```

---

## 4) Create Lost Report

- Method: `POST`
- URL: `/reports/lost`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/reports/lost`
- Auth: Optional
- Content-Type: `application/json` or `multipart/form-data`

Required fields:

- `itemName`
- `category`

Optional fields:

- `description` (max 500)
- `locationType`, `buildingCode`, `isRoom`, `roomOrArea`, `campusLocation`
- `building`, `floor`, `roomArea`, `lastKnownLocation`, `dateLost`, `timeLost`
- `contactName`, `contactPhone`
- `photoUrls` (array)
- multipart files

Response `201`:

```json
{
  "id": 6,
  "referenceNumber": "LF-000006",
  "status": "Pending Verification",
  "message": "Lost item report submitted for review."
}
```

---

## 5) Create Found Report

- Method: `POST`
- URL: `/reports/found`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/reports/found`
- Auth: Optional
- Content-Type: `application/json` or `multipart/form-data`

Required:

- `itemName`
- `category`
- `locationType`
- `collectionPlace`
- At least one image (`photoUrls` or multipart file)

Validation:

- If `locationType=building`: `buildingCode` (or `building`) and `roomOrArea` (or `roomArea`) are required
- If `locationType=campus`: `campusLocation` is required

Response `201`:

```json
{
  "id": 7,
  "referenceNumber": "LF-000007",
  "status": "Pending Verification",
  "message": "Found item report submitted for review."
}
```

---

## 6) Patch Item (Admin)

- Method: `PATCH`
- URL: `/items/{id}`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items/{id}`
- Auth: Required (`admin` or `lost_found_leader`)

Body (partial):

```json
{
  "adminStatus": "Pending",
  "title": "optional",
  "description": "optional",
  "status": "Active",
  "storageBinId": "optional",
  "condition": "good",
  "adminNotes": "optional",
  "reportedColor": "optional"
}
```

---

## 7) Manual Create Item (Admin)

- Method: `POST`
- URL: `/items`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items`
- Auth: Required (`admin` or `lost_found_leader`)
- Content-Type: `application/json` or `multipart/form-data`

Required:

- `type` (`lost` or `found`)
- `itemName`
- `category`

Found-type rules are the same as `/reports/found`.

---

## 8) Upload Image

- Method: `POST`
- URL: `/upload`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/upload`
- Auth: Optional
- Content-Type: `multipart/form-data`

Form field:

- `file` or `photo`

Allowed extensions: `.png`, `.jpg`, `.jpeg`

Response `201`:

```json
{
  "url": "https://host/uploads/lost-and-found/<guid>.jpg"
}
```

---

## 9) Notify Owner (Admin)

- Method: `POST`
- URL: `/items/{id}/notify`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items/{id}/notify`
- Auth: Required (`admin` or `lost_found_leader`)

Body:

```json
{
  "channel": "email",
  "message": "optional"
}
```

Response `202`:

```json
{
  "queued": true
}
```

---

## 10) Create Claim (User)

- Method: `POST`
- URL: `/items/{id}/claims`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items/{id}/claims`
- Auth: Required

Body:

```json
{
  "claimType": "owner",
  "message": "optional"
}
```

`claimType` values:

- `owner` (for "This Is Mine")
- `finder` (for "Item Found")

Response `201`:

```json
{
  "claimId": 15,
  "status": "pending_review"
}
```

Duplicate claim by same user for same item + same claim type returns `409`.

---

## 11) Confirm Receipt (Admin)

- Method: `POST`
- URL: `/items/{id}/confirm-receipt`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items/{id}/confirm-receipt`
- Auth: Required (`admin` or `lost_found_leader`)

Body:

```json
{
  "storageBinId": "BIN-402-A",
  "condition": "good",
  "intakePhotoUrl": "optional",
  "confirmAccuracy": true,
  "adminNotes": "optional"
}
```

Effect:

- Sets `adminStatus` to `Received`

---

## 12) Complete Delivery (Admin)

- Method: `POST`
- URL: `/items/{id}/complete-delivery`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items/{id}/complete-delivery`
- Auth: Required (`admin` or `lost_found_leader`)

Body:

```json
{
  "claimantName": "optional",
  "claimantStudentId": "optional",
  "studentIdVerified": true,
  "handoverProofUrl": "optional"
}
```

Effect:

- Sets `adminStatus` to `Delivered`

---

## 13) List Claims for Item (Admin, Optional)

- Method: `GET`
- URL: `/items/{id}/claims`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items/{id}/claims`
- Auth: Required (`admin` or `lost_found_leader`)

Response:

```json
{
  "claims": [
    {
      "id": 1,
      "claimType": "owner",
      "message": "This is mine",
      "status": "pending_review",
      "userId": "user-123",
      "createdAt": "2026-04-13T10:20:00Z"
    }
  ]
}
```

---

## 14) Item Timeline (Optional)

- Method: `GET`
- URL: `/items/{id}/timeline`
- Full URL: `http://51.20.193.29:5000/lostfound/api/lost-and-found/items/{id}/timeline`
- Auth: Optional

Response:

```json
{
  "events": [
    { "at": "2026-04-12T09:00:00Z", "label": "Report submitted", "done": true },
    { "at": null, "label": "Owner notified", "done": false },
    { "at": null, "label": "Delivery completed", "done": false }
  ]
}
```

---

## Error Format

```json
{
  "error": "VALIDATION_ERROR",
  "message": "Validation failed.",
  "details": {
    "fieldName": ["reason"]
  }
}
```


