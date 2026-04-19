# Location Service API

Documentation for the Location microservice (`Services/LocationService`), including routes as exposed through the API Gateway under the `/location` prefix.

## Base URL (example deployment)

- **Gateway host:** `http://51.20.193.29:5000`
- **Path pattern:** `{gateway}/location` + service routes `/api/v1/...`

The gateway strips the `/location` prefix when forwarding to the service, so clients call:

`http://51.20.193.29:5000/location/api/v1/...`

**Example**

- `GET http://51.20.193.29:5000/location/api/v1/rooms` — list all rooms.

---

## Authentication

| Area | Requirement |
|------|-------------|
| **JWT** | Issuer `myada-auth`, audience `myada-api` (defaults); RS256 via AuthService JWKS. |
| **Read** | `GET` on **buildings** and **rooms** — anonymous (no `[Authorize]`). |
| **Write** | `POST` / `PUT` / `DELETE` on buildings and rooms — **`admin` role** — header `Authorization: Bearer <token>`. |
| **Validate room location** | `POST .../locations/validate-room-location` — anonymous. |

---

## Buildings — `api/v1/buildings`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/v1/buildings` | No | List all buildings. |
| `GET` | `/api/v1/buildings/{id}` | No | Get building by id. `404` if missing. |
| `POST` | `/api/v1/buildings` | admin | Create building. |
| `PUT` | `/api/v1/buildings/{id}` | admin | Update building. |
| `DELETE` | `/api/v1/buildings/{id}` | admin | Delete building. `204 No Content` on success. |

### Gateway URLs (prefix `/location`)

- `http://51.20.193.29:5000/location/api/v1/buildings`
- `http://51.20.193.29:5000/location/api/v1/buildings/{id}`

### Request bodies

**Create** — `CreateBuildingDto`

```json
{
  "name": "Building A"
}
```

**Update** — `UpdateBuildingDto`

```json
{
  "name": "New name"
}
```

### Response shape — `BuildingDto`

```json
{
  "id": 1,
  "name": "Building A"
}
```

`POST` returns `201 Created` with a `Location` header pointing to the new resource.

---

## Rooms — `api/v1/rooms`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `GET` | `/api/v1/rooms` | No | List all rooms. |
| `GET` | `/api/v1/rooms/{id}` | No | Get room by id. `404` if missing. |
| `GET` | `/api/v1/rooms/by-building/{buildingId}` | No | Rooms in a building. |
| `POST` | `/api/v1/rooms` | admin | Create room. |
| `PUT` | `/api/v1/rooms/{id}` | admin | Update room. |
| `DELETE` | `/api/v1/rooms/{id}` | admin | Delete room. `204 No Content`. |

### Gateway URLs (examples)

- `http://51.20.193.29:5000/location/api/v1/rooms`
- `http://51.20.193.29:5000/location/api/v1/rooms/{id}`
- `http://51.20.193.29:5000/location/api/v1/rooms/by-building/{buildingId}`

### Response shape — `RoomDto`

```json
{
  "id": 1,
  "name": "Lab 1",
  "number": "101",
  "capacity": 30,
  "roomType": 1,
  "buildingId": 2,
  "buildingName": "Building A"
}
```

`roomType` is the `RoomType` enum (default JSON serialization is numeric unless configured otherwise).

### Create — `CreateRoomDto`

```json
{
  "name": "Lab 1",
  "number": "101",
  "capacity": 30,
  "roomType": 1,
  "buildingId": 2
}
```

### Update — `UpdateRoomDto`

Same fields as create:

```json
{
  "name": "Lab 1",
  "number": "101",
  "capacity": 30,
  "roomType": 1,
  "buildingId": 2
}
```

---

## `RoomType` enum

| Value | Name |
|------:|------|
| 0 | `Classroom` |
| 1 | `Lab` |
| 2 | `Office` |
| 3 | `Auditorium` |
| 4 | `Other` |

---

## Locations — `api/v1/locations`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/v1/locations/validate-room-location` | No | Returns whether the room exists **and** belongs to the given building. |
| `GET` | `/api/v1/locations/details` | No | **Stub:** returns `200 OK` with an **empty** body (no payload implemented). |

### Validate room location

**Gateway example**

`POST http://51.20.193.29:5000/location/api/v1/locations/validate-room-location`

**Body** — `ValidateRoomLocationDto`

```json
{
  "roomId": 1,
  "buildingId": 2
}
```

**Response:** JSON boolean — `true` if the room exists and `room.buildingId` equals `buildingId`; otherwise `false` (including when room or building is missing).

---

## Cross-cutting behavior

- **CORS:** Permissive policy (`AllowAnyOrigin`, `AllowAnyMethod`, `AllowAnyHeader`) in the service.
- **HTTPS:** `UseHttpsRedirection` is enabled; behavior depends on gateway/TLS termination.
- **Swagger UI:** Only when `ASPNETCORE_ENVIRONMENT=Development`; OpenAPI server URL is set to `/location` for path-prefix awareness.
- **Errors:** `GlobalExceptionMiddleware` handles exceptions (exact payload format depends on that middleware).

---

## Source references

- Controllers: `Services/LocationService/LocationService.Api/Controllers/`
- DTOs: `Services/LocationService/LocationService.Application/Dtos/`
- Enum: `Services/LocationService/LocationService.Domain/Enums/RoomType.cs`
