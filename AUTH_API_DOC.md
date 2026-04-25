# Auth API Documentation (Gateway First)

Last updated: 2026-04-25

This document is verified against:
- `Services/AuthService/AuthService.Api/Controllers/AuthController.cs`
- `Services/AuthService/AuthService.Api/Controllers/JwksController.cs`

## Base URLs

Recommended for frontend (via API Gateway):
- Production: `http://13.60.31.141:500`
- Local: `http://localhost:5000`

Direct auth service (if bypassing gateway):
- Production: `http://13.60.31.141:5001`
- Local: `http://localhost:5001`

Gateway routing for auth:
- Primary auth API path: `/api/auth/...`
- Proxy alias path: `/auth/api/auth/...`
- JWKS path through gateway: `/auth/.well-known/jwks.json`
- Swagger through gateway (when auth service runs in Development): `/auth/swagger/index.html`

## Token Settings

- Issuer: `myada-auth`
- Audience: `myada-api`
- Algorithm: `RS256`
- Key ID (`kid`): `my-key-id-1`
- Access token lifetime: `15 minutes`
- Refresh token lifetime: `30 days`

## JWT Claims in Access Token

- `sub`: user id
- `jti`: token id
- `email`: user email
- `firstName`: user first name
- `userType`: user type value
- `role`: one or more role claims

## Authorization Header

Use for protected endpoints:

```http
Authorization: Bearer <accessToken>
```

## Verified Endpoint Inventory

| # | Method | Path | Auth | Notes |
|---|---|---|---|---|
| 1 | `GET` | `/api/auth/create-admin` | No | Bootstrap endpoint. |
| 2 | `POST` | `/api/auth/create-user-with-role` | Yes (`admin`) | Creates user and assigns role. |
| 3 | `GET` | `/api/auth/users-by-role/{role}` | Yes (`admin`) | Lists users for role. |
| 4 | `GET` | `/api/auth/users/{id}` | Yes | Admin can fetch any user; others can fetch self. |
| 5 | `GET` | `/api/auth/users/by-organizational-id/{organizationalId}` | Yes | Admin can fetch any user; others can fetch self by organizationalId. |
| 6 | `POST` | `/api/auth/users/{id}/profile-photo` | Yes | Multipart upload endpoint. |
| 7 | `PATCH` | `/api/auth/users/{id}/profile-photo` | Yes | Multipart profile-photo update alias. |
| 8 | `POST` | `/api/auth/forgot-password` | No | Sends password reset email (if user exists). |
| 9 | `POST` | `/api/auth/reset-password` | No | Resets password with reset token. |
| 10 | `PUT` | `/api/auth/change-password` | No | Not protected in current implementation. |
| 11 | `POST` | `/api/auth/login` | No | Returns access + refresh token. |
| 12 | `POST` | `/api/auth/refresh` | No | Rotates refresh token and returns new pair. |
| 13 | `POST` | `/api/auth/logout` | Yes | Revokes refresh token. |
| 14 | `GET` | `/.well-known/jwks.json` | No | Raw auth service route (gateway alias below). |

JWKS through gateway:
- `GET /auth/.well-known/jwks.json`

## Endpoint Details

### 1) Create Admin

- Method: `GET`
- Path: `/api/auth/create-admin`
- Auth: No

Creates default admin user/role if bootstrap path is used.

Default credentials:
- `username`: `Admin`
- `password`: `Admin123*`

Success response:
- `200 OK` with empty body.

Notes:
- Bootstrap/dev endpoint. Do not expose in production.

---

### 2) Create User With Role

- Method: `POST`
- Path: `/api/auth/create-user-with-role`
- Auth: Yes (`Bearer`, `admin` role required)

Request body:

```json
{
  "email": "user1@myada.local",
  "password": "User123*",
  "username": "user1",
  "firstName": "User",
  "lastName": "One",
  "phoneNumber": "123456789",
  "role": "student"
}
```

Success response:
- `200 OK`

```json
{
  "message": "User created successfully",
  "user": {
    "id": "<guid>",
    "userName": "user1",
    "email": "user1@myada.local",
    "firstName": "User",
    "lastName": "One",
    "phoneNumber": "123456789",
    "profileImage": null,
    "role": "student"
  }
}
```

Error responses:
- `400 Bad Request` -> role/email/username validation failures, or Identity errors
- `502 Bad Gateway` -> support staff provisioning failed (request rolled back)
- `401 Unauthorized` -> missing/invalid token
- `403 Forbidden` -> authenticated user not in `admin` role

---

### 3) Get Users By Role

- Method: `GET`
- Path: `/api/auth/users-by-role/{role}`
- Auth: Yes (`Bearer`, `admin` role required)

Success response:
- `200 OK`

```json
{
  "role": "student",
  "count": 2,
  "users": [
    {
      "id": "<guid>",
      "userName": "seed.user01",
      "email": "seed.user01@myada.local",
      "firstName": "SeedFirst1",
      "lastName": "SeedLast1",
      "phoneNumber": null,
      "profileImage": null,
      "userType": "Student",
      "status": "Active"
    }
  ]
}
```

---

### 4) Get User By Id

- Method: `GET`
- Path: `/api/auth/users/{id}`
- Auth: Yes (`Bearer`)

Access rules:
- `admin` can fetch any user.
- non-admin can fetch only their own user.

Success response:
- `200 OK`

```json
{
  "id": "<guid>",
  "userName": "seed.user01",
  "email": "seed.user01@myada.local",
  "firstName": "SeedFirst1",
  "lastName": "SeedLast1",
  "phoneNumber": null,
  "profileImage": null,
  "userType": "Student",
  "status": "Active",
  "roles": ["student"]
}
```

---

### 5) Get User By OrganizationalId

- Method: `GET`
- Path: `/api/auth/users/by-organizational-id/{organizationalId}`
- Auth: Yes (`Bearer`)

Access rules:
- `admin` can fetch any user.
- non-admin can fetch only their own user by matching the JWT `organizationalId` claim.

Success response:
- `200 OK`

```json
{
  "id": "<guid>",
  "userName": "seed.user01",
  "email": "seed.user01@myada.local",
  "firstName": "SeedFirst1",
  "lastName": "SeedLast1",
  "organizationalId": "100000001",
  "phoneNumber": null,
  "profileImage": null,
  "userType": "Student",
  "status": "Active",
  "roles": ["student"]
}
```

---

### 6) Upload User Profile Photo

- Method: `POST`
- Path: `/api/auth/users/{id}/profile-photo`
- Auth: Yes (`Bearer`)
- Content type: `multipart/form-data`

Form field:
- `file` (required): image file

Validation:
- Allowed extensions: `png`, `jpg`, `jpeg`, `webp`
- Max size: `5 MB`

Success response:
- `200 OK`

```json
{
  "userId": "<guid>",
  "organizationalId": "100000001",
  "profileImage": "https://<bucket-or-cdn>/users/<file>"
}
```

---

### 7) Update User Profile Photo

- Method: `PATCH`
- Path: `/api/auth/users/{id}/profile-photo`
- Auth: Yes (`Bearer`)
- Content type: `multipart/form-data`

Behavior:
- Uses the same validation and S3 upload flow as the `POST` variant.
- Intended for clients that prefer a partial-update verb for changing the existing profile photo.

Success response:
- `200 OK`

```json
{
  "userId": "<guid>",
  "organizationalId": "100000001",
  "profileImage": "https://<bucket-or-cdn>/users/<file>"
}
```

---

### 8) Forgot Password

- Method: `POST`
- Path: `/api/auth/forgot-password`
- Auth: No

Request body:

```json
{
  "email": "admin@gmail"
}
```

Success response:
- `200 OK`

```json
{
  "message": "If this email exists, password reset link sent."
}
```

Notes:
- Current implementation always returns the same message to avoid user enumeration.
- Reset token is not returned in API response.

---

### 9) Reset Password

- Method: `POST`
- Path: `/api/auth/reset-password`
- Auth: No

Request body:

```json
{
  "email": "admin@gmail",
  "token": "<reset-token>",
  "newPassword": "NewPassword123*"
}
```

Success response:
- `200 OK`

```json
"Password reset successfully"
```

Error responses:
- `404 Not Found` -> `"User not found"`
- `400 Bad Request` -> Identity error array

---

### 10) Change Password

- Method: `PUT`
- Path: `/api/auth/change-password`
- Auth: No (current implementation)

Request body:

```json
{
  "email": "admin@gmail",
  "oldPassword": "OldPassword123*",
  "newPassword": "NewPassword123*"
}
```

Success response:
- `200 OK`

```json
"Password Changed successfully"
```

Error responses:
- `404 Not Found` (empty body when user not found)
- `400 Bad Request` -> Identity error array

---

### 11) Login

- Method: `POST`
- Path: `/api/auth/login`
- Auth: No

Request body:

```json
{
  "username": "Admin",
  "password": "Admin123*"
}
```

Success response:
- `200 OK`

```json
{
  "accessToken": "<jwt-access-token>",
  "refreshToken": "<refresh-token>",
  "profileImage": "https://<bucket-or-cdn>/users/<file-or-null>"
}
```

Error response:
- `401 Unauthorized` -> `"Username or password is incorrect"`

---

### 12) Refresh Token

- Method: `POST`
- Path: `/api/auth/refresh`
- Auth: No

Request body:

```json
{
  "refreshToken": "<refresh-token>"
}
```

Success response:
- `200 OK`

```json
{
  "accessToken": "<new-access-token>",
  "refreshToken": "<new-refresh-token>"
}
```

Error responses:
- `401 Unauthorized` -> `"Invalid refresh token"`
- `401 Unauthorized` -> `"Refresh token expired"`
- `401 Unauthorized` -> `"Refresh token revoked"`
- `401 Unauthorized` -> empty body (if linked user no longer exists)

---

### 13) Logout

- Method: `POST`
- Path: `/api/auth/logout`
- Auth: Yes (`Bearer`)

Request body:

```json
{
  "refreshToken": "<refresh-token>"
}
```

Success response:
- `200 OK`

```json
{
  "message": "Logged out successfully"
}
```

---

### 14) JWKS Public Key

- Method: `GET`
- Direct auth path: `/.well-known/jwks.json`
- Via gateway path: `/auth/.well-known/jwks.json`
- Auth: No

Success response:
- `200 OK`

```json
{
  "keys": [
    {
      "kty": "RSA",
      "use": "sig",
      "alg": "RS256",
      "kid": "my-key-id-1",
      "n": "<modulus-base64url>",
      "e": "<exponent-base64url>"
    }
  ]
}
```

Potential server error:
- `500 Internal Server Error` -> `"JWT public key file was not found. ..."`

## Frontend Integration Notes

- Send protected requests with `Authorization: Bearer <accessToken>`.
- On `401` from protected APIs, call `/api/auth/refresh` once, then retry original request.
- If refresh fails, clear session and redirect to login.
- On logout, call `/api/auth/logout` with refresh token, then clear local tokens.
- There is no public register endpoint.
