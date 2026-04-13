# Auth API Documentation for Frontend

Last updated: 2026-04-13

## Base URLs

Recommended for frontend (via gateway):
- `http://localhost:5000`
- `https://localhost:7001`

Direct auth service (if bypassing gateway):
- `http://localhost:5001`
- `https://localhost:7140`

Base path for auth endpoints:
- `/api/auth`

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

## Auth Header

For protected endpoints:

```http
Authorization: Bearer <accessToken>
```

## Endpoints

### 1) Create Admin

- Method: `GET`
- Path: `/api/auth/create-admin`
- Auth: No

Creates default admin user/role if bootstrap path is used.

Default credentials:
- `username`: `Admin`
- `password`: `Admin123*`

Success response:
- `200 OK`
- Empty body

Notes:
- This is a bootstrap/dev-style endpoint and should not be exposed in production.

---

### 2) Create User With Role

- Method: `POST`
- Path: `/api/auth/create-user-with-role`
- Auth: Yes (`Bearer` access token required)
- Required role: `admin`

Headers:

```http
Authorization: Bearer <admin-access-token>
```

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
    "role": "student"
  }
}
```

Error responses:
- `400 Bad Request` -> `"Role '<role>' does not exist"`
- `400 Bad Request` -> `"Email is already in use"`
- `400 Bad Request` -> `"Username is already in use"`
- `400 Bad Request` -> Identity error array (password/validation failures)
- `401 Unauthorized` when access token is missing/invalid
- `403 Forbidden` when authenticated user is not in `admin` role

Notes:
- `role` is normalized to lowercase before assignment.
- `userType` is inferred from role:
  - `student` -> `Student`
  - `instructor` -> `Faculty`
  - all other roles -> `Staff`

---

### 3) Get Users By Role

- Method: `GET`
- Path: `/api/auth/users-by-role/{role}`
- Auth: Yes (`Bearer` access token required)
- Required role: `admin`

Headers:

```http
Authorization: Bearer <admin-access-token>
```

Path parameter:
- `role` (string, required) e.g. `student`, `instructor`, `dispatcher`, `admin`

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
      "userType": "Student",
      "status": "Active"
    }
  ]
}
```

Error responses:
- `400 Bad Request` -> `"Role is required"`
- `400 Bad Request` -> `"Role '<role>' does not exist"`
- `401 Unauthorized` when access token is missing/invalid
- `403 Forbidden` when authenticated user is not in `admin` role

Notes:
- Role is normalized to lowercase before querying.

---

### 4) Login

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
  "refreshToken": "<refresh-token>"
}
```

Error response:
- `401 Unauthorized`

```json
"Username or password is incorrect"
```

---

### 5) Refresh Token

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

Notes:
- Refresh token rotation is enabled. On success, old refresh token is revoked and a new one is returned.

---

### 6) Logout

- Method: `POST`
- Path: `/api/auth/logout`
- Auth: Yes (`Bearer` access token required)

Headers:

```http
Authorization: Bearer <accessToken>
```

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

Error response:
- `401 Unauthorized` when access token is missing/invalid.

Notes:
- Revocation call is effectively idempotent; if refresh token does not exist, endpoint still returns success.

---

### 7) Forgot Password

- Method: `POST`
- Path: `/api/auth/forgot-password`
- Auth: No

Request body:

```json
{
  "email": "admin@gmail"
}
```

Success response when user exists:
- `200 OK`

```json
{
  "message": "If this email exists, password reset link sent.",
  "email": "admin@gmail",
  "token": "<reset-token>"
}
```

Success response when user does not exist:
- `200 OK`

```json
{
  "message": "If this email exists, password reset link sent."
}
```

Notes:
- Current implementation returns reset token directly in API response for existing users.

---

### 8) Reset Password

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

Identity error array shape:

```json
[
  {
    "code": "PasswordTooShort",
    "description": "Passwords must be at least 6 characters."
  }
]
```

---

### 9) Change Password

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
- `404 Not Found` (empty body)
- `400 Bad Request` -> Identity error array

Notes:
- Endpoint is not protected by `[Authorize]` right now.

---

### 10) JWKS Public Key

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
- `500 Internal Server Error` -> `"JWT public key file was not found."`

---

## Frontend Integration Notes

- Store `accessToken` in memory when possible; store `refreshToken` in secure storage strategy suitable for your app.
- Add `Authorization: Bearer <accessToken>` to protected calls.
- On `401` from protected APIs, call `/api/auth/refresh` once, update both tokens, then retry original request.
- If refresh fails (`401`), clear session and redirect to login.
- On logout, call `/api/auth/logout` with refresh token and then clear local tokens.
- There is no open/public register endpoint. User creation is available through `POST /api/auth/create-user-with-role` for `admin` users.

