# LocationService Frontend API Documentation

Last updated: 2026-04-13

## Base URLs

- Deployed API Gateway URL: `http://13.60.196.144:5000/`
- Gateway route prefix for LocationService: `/location`
- LocationService API base path: `/api/v1`

Frontend should use:

- **`http://13.60.196.144:5000/location/api/v1`**

---

## Authentication

- Public endpoints: all `GET` endpoints and `POST /locations/validate-room-location`
- Admin-only endpoints:
  - `POST /buildings`
  - `PUT /buildings/{id}`
  - `DELETE /buildings/{id}`
  - `POST /rooms`
  - `PUT /rooms/{id}`
  - `DELETE /rooms/{id}`

Admin role required: `admin`

---

## Data Models

## BuildingDto

```json
{
  "id": 1,
  "name": "Main Building"
}
```

## RoomDto

```json
{
  "id": 10,
  "name": "Room 101",
  "number": "101",
  "capacity": 40,
  "roomType": 0,
  "buildingId": 1,
  "buildingName": "Main Building"
}
```

Important:

- `buildingName` is included in room GET responses.
- `roomType` is an enum and is currently serialized as an integer.

RoomType values:

- `0 = Classroom`
- `1 = Lab`
- `2 = Office`
- `3 = Auditorium`
- `4 = Other`

## ValidateRoomLocationDto

```json
{
  "roomId": 10,
  "buildingId": 1
}
```

---

## Endpoints

## Buildings

## 1) Get all buildings

- Method: `GET`
- URL: `/buildings`
- Full URL: `http://13.60.196.144:5000/location/api/v1/buildings`
- Auth: Optional

Response:

```json
[
  { "id": 1, "name": "Main Building" }
]
```

## 2) Get building by id

- Method: `GET`
- URL: `/buildings/{id}`
- Full URL: `http://13.60.196.144:5000/location/api/v1/buildings/{id}`
- Auth: Optional

Response:

```json
{ "id": 1, "name": "Main Building" }
```

If not found: `404`

## 3) Create building (admin)

- Method: `POST`
- URL: `/buildings`
- Full URL: `http://13.60.196.144:5000/location/api/v1/buildings`
- Auth: Required (`admin`)

Body:

```json
{ "name": "Library" }
```

Response: `201 Created`

## 4) Update building (admin)

- Method: `PUT`
- URL: `/buildings/{id}`
- Full URL: `http://13.60.196.144:5000/location/api/v1/buildings/{id}`
- Auth: Required (`admin`)

Body:

```json
{ "name": "Updated Name" }
```

Response: `200 OK`

## 5) Delete building (admin)

- Method: `DELETE`
- URL: `/buildings/{id}`
- Full URL: `http://13.60.196.144:5000/location/api/v1/buildings/{id}`
- Auth: Required (`admin`)

Response: `204 No Content`

---

## Rooms

## 6) Get all rooms

- Method: `GET`
- URL: `/rooms`
- Full URL: `http://13.60.196.144:5000/location/api/v1/rooms`
- Auth: Optional

Response:

```json
[
  {
    "id": 10,
    "name": "Room 101",
    "number": "101",
    "capacity": 40,
    "roomType": 0,
    "buildingId": 1,
    "buildingName": "Main Building"
  }
]
```

## 7) Get room by id

- Method: `GET`
- URL: `/rooms/{id}`
- Full URL: `http://13.60.196.144:5000/location/api/v1/rooms/{id}`
- Auth: Optional

Response:

```json
{
  "id": 10,
  "name": "Room 101",
  "number": "101",
  "capacity": 40,
  "roomType": 0,
  "buildingId": 1,
  "buildingName": "Main Building"
}
```

If not found: `404`

## 8) Get rooms by building id

- Method: `GET`
- URL: `/rooms/by-building/{buildingId}`
- Full URL: `http://13.60.196.144:5000/location/api/v1/rooms/by-building/{buildingId}`
- Auth: Optional

Response: array of `RoomDto`

## 9) Create room (admin)

- Method: `POST`
- URL: `/rooms`
- Full URL: `http://13.60.196.144:5000/location/api/v1/rooms`
- Auth: Required (`admin`)

Body:

```json
{
  "name": "Room 101",
  "number": "101",
  "capacity": 40,
  "roomType": 0,
  "buildingId": 1
}
```

Response: `201 Created`

## 10) Update room (admin)

- Method: `PUT`
- URL: `/rooms/{id}`
- Full URL: `http://13.60.196.144:5000/location/api/v1/rooms/{id}`
- Auth: Required (`admin`)

Body:

```json
{
  "name": "Room 101",
  "number": "101",
  "capacity": 45,
  "roomType": 0,
  "buildingId": 1
}
```

Response: `200 OK`

## 11) Delete room (admin)

- Method: `DELETE`
- URL: `/rooms/{id}`
- Full URL: `http://13.60.196.144:5000/location/api/v1/rooms/{id}`
- Auth: Required (`admin`)

Response: `204 No Content`

---

## Locations

## 12) Validate room-building relation

- Method: `POST`
- URL: `/locations/validate-room-location`
- Full URL: `http://13.60.196.144:5000/location/api/v1/locations/validate-room-location`
- Auth: Optional

Body:

```json
{
  "roomId": 10,
  "buildingId": 1
}
```

Response:

```json
true
```

Returns `false` if room/building does not exist or relation is invalid.

## 13) Location details (placeholder)

- Method: `GET`
- URL: `/locations/details`
- Full URL: `http://13.60.196.144:5000/location/api/v1/locations/details`
- Auth: Optional

Current implementation returns `200 OK` with empty body.

---

## Error Behavior

- Direct controller `GetById` not found: plain `404` (no custom payload)
- Service exceptions (`KeyNotFoundException`) are converted by middleware to:

```json
{
  "status": 404,
  "message": "..."
}
```

- Unhandled exceptions:

```json
{
  "status": 500,
  "message": "Internal server error",
  "detail": "..."
}
```

