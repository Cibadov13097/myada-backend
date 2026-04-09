# Auth API Documentation

Bu sənəd `Services/AuthService/AuthService.Api` üçün endpoint-ləri təsvir edir.

## Base URLs

- Local Auth Service: `http://localhost:5001`
- Docker internal (services arasi): `http://authservice:8080`
- JWKS endpoint: `/.well-known/jwks.json`

## JWT Config

- Issuer: `myada-auth`
- Audience: `myada-api`
- Algorithm: `RS256`

---

## 1) Create Admin

- **Method**: `GET`
- **Path**: `/api/auth/create-admin`
- **Auth**: No

Default admin yaradir:
- `username`: `Admin`
- `password`: `Admin123*`

### Success Response
`200 OK`
```json
{}
```

---

## 2) Login

- **Method**: `POST`
- **Path**: `/api/auth/login`
- **Auth**: No

### Request Body
```json
{
  "username": "Admin",
  "password": "Admin123*"
}
```

### Success Response
`200 OK`
```json
{
  "accessToken": "<jwt-access-token>",
  "refreshToken": "<refresh-token>"
}
```

### Error Response
`401 Unauthorized`
```json
"Username or password is incorrect"
```

---

## 3) Refresh Token

- **Method**: `POST`
- **Path**: `/api/auth/refresh`
- **Auth**: No

### Request Body
```json
{
  "refreshToken": "<refresh-token>"
}
```

### Success Response
`200 OK`
```json
{
  "accessToken": "<new-access-token>",
  "refreshToken": "<new-refresh-token>"
}
```

### Error Responses
- `401 Unauthorized` -> `"Invalid refresh token"`
- `401 Unauthorized` -> `"Refresh token expired"`
- `401 Unauthorized` -> `"Refresh token revoked"`

---

## 4) Logout

- **Method**: `POST`
- **Path**: `/api/auth/logout`
- **Auth**: Yes (`Bearer`)

### Headers
`Authorization: Bearer <accessToken>`

### Request Body
```json
{
  "refreshToken": "<refresh-token>"
}
```

### Success Response
`200 OK`
```json
{
  "message": "Logged out successfully"
}
```

---

## 5) Forgot Password

- **Method**: `POST`
- **Path**: `/api/auth/forgot-password`
- **Auth**: No

### Request Body
```json
{
  "email": "admin@gmail"
}
```

### Success Response
`200 OK`
```json
{
  "message": "If this email exists, password reset link sent.",
  "email": "admin@gmail",
  "token": "<reset-token>"
}
```

Qeyd: Email tapilmasa da eyni message qaytarilir.

---

## 6) Reset Password

- **Method**: `POST`
- **Path**: `/api/auth/reset-password`
- **Auth**: No

### Request Body
```json
{
  "email": "admin@gmail",
  "token": "<reset-token>",
  "newPassword": "NewPassword123*"
}
```

### Success Response
`200 OK`
```json
"Password reset successfully"
```

### Error Responses
- `404 Not Found` -> `"User not found"`
- `400 Bad Request` -> Identity error list

---

## 7) Change Password

- **Method**: `PUT`
- **Path**: `/api/auth/change-password`
- **Auth**: No (hazirki implementasiyada)

### Request Body
```json
{
  "email": "admin@gmail",
  "oldPassword": "OldPassword123*",
  "newPassword": "NewPassword123*"
}
```

### Success Response
`200 OK`
```json
"Password Changed successfully"
```

### Error Responses
- `404 Not Found`
- `400 Bad Request` -> Identity error list

---

## 8) JWKS (Public Keys)

- **Method**: `GET`
- **Path**: `/.well-known/jwks.json`
- **Auth**: No

### Success Response
`200 OK`
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

---

## Frontend Integration Notes

- Access token-i request header-da gonderin:
  - `Authorization: Bearer <accessToken>`
- `401` gelende `/api/auth/refresh` ile token yenileyin.
- Logout zamani hem access token temizleyin, hem de `/api/auth/logout` cagirib refresh token-i revoke edin.
