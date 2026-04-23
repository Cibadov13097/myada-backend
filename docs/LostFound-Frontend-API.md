# LostFound Backend API Documentation

Last updated: 2026-04-23

This document is verified against the backend controllers in:
- `Services/LostFoundService/LostFoundService.API/Controllers`

## Base URLs

- API Gateway (recommended): `http://13.60.31.141:5000`
- Gateway route prefix for LostFound service: `/lostfound`
- Direct service (diagnostics only):
  - Production: `http://13.60.31.141:5002`
  - Local: `http://localhost:5002`

Primary modern API base path:
- `/api/lost-and-found`

Legacy API base paths (still active):
- `/api/v1/lost-items`
- `/api/v1/found-items`
- `/api/v1/categories`
- `/api/v1/admin/lost-items`
- `/api/v1/admin/found-items`

Frontend through gateway should call:
- `http://13.60.31.141:5000/lostfound/api/lost-and-found/...`
- `http://13.60.31.141:5000/lostfound/api/v1/...` (legacy routes)

## Important Gateway Note

If you call through gateway (`:5000`), include `/lostfound`:
- Correct: `http://13.60.31.141:5000/lostfound/api/lost-and-found/items`
- Wrong: `http://13.60.31.141:5000/api/lost-and-found/items` (gateway 404)

## Auth Summary

- Public endpoints: most read/report/upload endpoints in `/api/lost-and-found`
- Auth required: claim creation and user-scoped legacy endpoints
- Admin/LostFoundLeader required: admin workflow endpoints

Accepted admin roles in current code:
- `admin`
- `lost_found_leader`

## A) Modern API (`/api/lost-and-found`)

Gateway prefix for all paths below:
- `http://13.60.31.141:5000/lostfound`

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/lost-and-found/items` | Optional |
| `GET` | `/api/lost-and-found/items/{id}` | Optional |
| `GET` | `/api/lost-and-found/stats` | Optional |
| `POST` | `/api/lost-and-found/reports/lost` | Optional |
| `POST` | `/api/lost-and-found/reports/found` | Optional |
| `PATCH` | `/api/lost-and-found/items/{id}` | `admin` or `lost_found_leader` |
| `POST` | `/api/lost-and-found/items` | `admin` or `lost_found_leader` |
| `POST` | `/api/lost-and-found/upload` | Optional |
| `POST` | `/api/lost-and-found/items/{id}/notify` | `admin` or `lost_found_leader` |
| `POST` | `/api/lost-and-found/items/{id}/claims` | Any authenticated user |
| `POST` | `/api/lost-and-found/items/{id}/confirm-receipt` | `admin` or `lost_found_leader` |
| `POST` | `/api/lost-and-found/items/{id}/complete-delivery` | `admin` or `lost_found_leader` |
| `GET` | `/api/lost-and-found/items/{id}/claims` | `admin` or `lost_found_leader` |
| `GET` | `/api/lost-and-found/items/{id}/timeline` | Optional |

### Modern API Notes

- `POST /reports/lost` and `POST /reports/found` accept:
  - `application/json`
  - `multipart/form-data`
- For create payloads, images can be sent either as uploaded files (`multipart/form-data`) or as pre-uploaded URL fields:
  - `imageUrl` (single string)
  - `imageUrls` (array or repeated form key; `imageUrls[]`, `images`, `photos`, `photoUrls` also supported in form-data)
- `location` is nullable in Lost/Found item models.
- If frontend sends `locationType=other`, it must also send `location` (string).
- For multipart forms, backend reads custom location from `location` (or fallback `customLocation`).
- `POST /items/{id}/confirm-receipt` expects `multipart/form-data` (`[FromForm]` in code).
- `POST /items/{id}/complete-delivery` expects `multipart/form-data` (`[FromForm]` in code).
- `POST /upload` accepts form file field `file`, `photo`, `image`, or `imageFile`.
- Image validation in code allows PNG/JPG/JPEG files.

## B) Legacy User APIs (`/api/v1/lost-items`, `/api/v1/found-items`)

### Lost Items (`/api/v1/lost-items`)

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/lost-items` | Optional |
| `GET` | `/api/v1/lost-items/{id}` | Optional |
| `GET` | `/api/v1/lost-items/users/me` | Auth required |
| `GET` | `/api/v1/lost-items/users/me/{id}` | Auth required |
| `POST` | `/api/v1/lost-items` | Auth required |
| `PUT` | `/api/v1/lost-items/{id}` | Auth required |
| `DELETE` | `/api/v1/lost-items/{id}` | Auth required |

Legacy lost create/update notes:
- Uses `multipart/form-data`
- `imageFile` is required on create

### Found Items (`/api/v1/found-items`)

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/found-items` | Optional |
| `GET` | `/api/v1/found-items/{id}` | Optional |
| `GET` | `/api/v1/found-items/users/me` | Auth required |
| `GET` | `/api/v1/found-items/users/me/{id}` | Auth required |
| `POST` | `/api/v1/found-items` | Auth required |
| `PUT` | `/api/v1/found-items/{id}` | Auth required |
| `DELETE` | `/api/v1/found-items/{id}` | Auth required |

Legacy found create/update notes:
- Uses `multipart/form-data`
- `imageFile` is required on create

## C) Categories API (`/api/v1/categories`)

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/categories` | Optional |
| `GET` | `/api/v1/categories/{id}` | Optional |
| `POST` | `/api/v1/categories` | `admin` or `lost_found_leader` |
| `PUT` | `/api/v1/categories/{id}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/categories/{id}` | `admin` or `lost_found_leader` |

## D) Legacy Admin APIs

### Admin Lost Items (`/api/v1/admin/lost-items`)

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/admin/lost-items` | `admin` or `lost_found_leader` |
| `GET` | `/api/v1/admin/lost-items/{id}` | `admin` |
| `POST` | `/api/v1/admin/lost-items` | `admin` or `lost_found_leader` |
| `PUT` | `/api/v1/admin/lost-items/{id}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/admin/lost-items/{id}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/admin/lost-items/{id}/force` | `admin` |

### Admin Found Items (`/api/v1/admin/found-items`)

| Method | Path | Auth |
|---|---|---|
| `GET` | `/api/v1/admin/found-items` | `admin` or `lost_found_leader` |
| `GET` | `/api/v1/admin/found-items/{id}` | `admin` |
| `POST` | `/api/v1/admin/found-items` | `admin` or `lost_found_leader` |
| `PUT` | `/api/v1/admin/found-items/{id}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/admin/found-items/{id}` | `admin` or `lost_found_leader` |
| `DELETE` | `/api/v1/admin/found-items/{id}/force` | No `[Authorize]` attribute in current code |

## Common Error Shape (modern controller)

```json
{
  "error": "VALIDATION_ERROR",
  "message": "Validation failed.",
  "details": {
    "fieldName": ["reason"]
  }
}
```

Note:
- Some legacy controllers may return service-specific error shapes.
