# MyAdaSupportService API Endpoints

Last updated: 2026-04-26

Base path (service): `/api`

Gateway route format:
- Production: `http://13.60.31.141/support/api/...`
- Local: `http://localhost:5000/support/api/...`

Direct service format:
- Production diagnostics: `http://13.60.31.141:5008/api/...`
- Local launch profile: `http://localhost:5184/api/...`
- Local docker-compose: `http://localhost:5008/api/...`

Common 404 rule:
- If you call the deployed gateway, `/support` is required in the path.

Authentication:
- Bearer JWT is required for all endpoints except `POST /api/SupportStaffStatuses/internal/provision`.

## Response envelope

The API uses AutoWrapper middleware, so successful and error responses are wrapped.

Typical success shape:

```json
{
  "message": "Request successful.",
  "isError": false,
  "responseException": null,
  "result": {}
}
```

For `204 No Content` endpoints, the response body is empty.

## Enum and value choices

### Domain enums

- `SupportArea`: `IT`, `FM`
- `RequestStatus`: `New`, `Assigned`, `InProgress`, `Completed`, `Cancelled`
- `StaffAvailabilityStatus`: `Online`, `Offline`, `OnBreak`
- `UrgencyLevel`: `Standard`, `Critical`

### Wire-format notes

- `CreateSupportRequestDto.area`: `it`, `fm`
- `CreateSupportRequestDto.locationType`: `building`, `campus`
- `CreateSupportRequestDto.placeType`: `room`, `nonRoom`
- `CreateSupportRequestDto.urgency`: `standard`, `critical`
- `RequestFilterDto.status`: `open`, `New`, `Assigned`, `InProgress`, `Completed`, `Cancelled`
- `RequestFilterDto.service`: `IT`, `FM`
- `RequestFilterDto.sortBy`: `newest`, `oldest`
- `SupportStaffStatusDto.status` / `UpdateSupportStaffStatusDto.status`: `Online`, `Offline`, `OnBreak`
- `CategoryCreateDto.Module` and `CategoryDto.Module` use the `SupportArea` enum. If your JSON serializer does not convert enums to strings, send the numeric value (`0 = IT`, `1 = FM`).

---

## Categories

### GET `/api/Categories`

Returns all categories.

Response `200 OK` result:

```json
[
  {
    "id": 1,
    "module": 0,
    "name": "Network"
  }
]
```

### GET `/api/Categories/module/{module}`

Returns categories by module.

- Path params:
  - `module` (string; `IT` or `FM`)
- Response `200 OK` result: `CategoryDto[]`

### POST `/api/Categories`

Creates a category.

- Request body (`CategoryCreateDto`):

```json
{
  "module": 0,
  "name": "Hardware"
}
```

- Response `201 Created` result (`CategoryDto`):

```json
{
  "id": 10,
  "module": 0,
  "name": "Hardware"
}
```

### DELETE `/api/Categories/{id}`

Deletes a category.

- Path params:
  - `id` (int)
- Response: `204 No Content`

---

## Attachments

### POST `/api/Attachments`

Adds an attachment record to a request.

Upload the image first through `POST /api/SupportRequests/uploads` or `POST /api/Requests/uploads`, then pass the returned URL here.

- Request body (`AttachmentCreateDto`):

```json
{
  "requestId": 123,
  "url": "https://my-bucket.s3.eu-north-1.amazonaws.com/support/requests/attachments/abc123.png"
}
```

- Response `200 OK` result (`AttachmentDto`):

```json
{
  "id": 55,
  "url": "https://my-bucket.s3.eu-north-1.amazonaws.com/support/requests/attachments/abc123.png"
}
```

### GET `/api/Attachments/request/{requestId}`

Gets all attachments for a request.

- Path params:
  - `requestId` (int)
- Response `200 OK` result: `AttachmentDto[]`

### DELETE `/api/Attachments/{attachmentId}`

Removes an attachment.

- Path params:
  - `attachmentId` (int)
- Response: `204 No Content`

---

## Support Staff Statuses

### POST `/api/SupportStaffStatuses`

Creates support staff status.

- Roles: `support`, `dispatcher`, `staff`, `admin`
- Request body (`CreateSupportStaffStatusDto`):

```json
{
  "memberId": "7d75bf0d-c467-4ce7-8872-c58495a6ca8a",
  "status": "Online"
}
```

- `memberId` must match authenticated user `sub` unless the caller is `admin`.
- If `memberId` is `00000000-0000-0000-0000-000000000000`, the backend uses the authenticated user id (`sub`).
- Response: `201 Created`

### GET `/api/SupportStaffStatuses/member/{memberId}`

Gets status by member id.

- Path params:
  - `memberId` (guid, must match authenticated user `sub` unless the caller is `admin`)
- Response `200 OK` result (`SupportStaffStatusDto`):

```json
{
  "id": 1,
  "memberId": "7d75bf0d-c467-4ce7-8872-c58495a6ca8a",
  "status": "Online",
  "updatedAt": "2026-04-13T10:00:00Z"
}
```

- If not found, the service auto-creates a row with status `Offline`.

### GET `/api/SupportStaffStatuses/status/{status}`

Gets all staff statuses by status.

- Path params:
  - `status` (string enum-like: `Online`, `Offline`, `OnBreak`)
- Response `200 OK` result: `SupportStaffStatusDto[]`
- Access: any authenticated user

### PUT `/api/SupportStaffStatuses/member/{memberId}`

Updates staff status.

- Path params:
  - `memberId` (guid, must match authenticated user `sub` unless the caller is `admin`)
- Request body (`UpdateSupportStaffStatusDto`):

```json
{
  "status": "OnBreak"
}
```

- Response: `204 No Content`

- If not found, the service auto-creates a status row with the requested status.

### POST `/api/SupportStaffStatuses/internal/provision`

Internal backend provisioning endpoint.

- Authentication: none
- Header required:

```http
X-Internal-Key: <shared-internal-key>
```

- Request body (`CreateSupportStaffStatusDto`):

```json
{
  "memberId": "7d75bf0d-c467-4ce7-8872-c58495a6ca8a",
  "status": "Offline"
}
```

- If `status` is omitted or blank, the backend uses `Offline`.
- If `memberId` is missing or empty, the endpoint returns `400 Bad Request`.
- Response: `200 OK`

Typical responses:
- `201 Created` (POST)
- `200 OK` (GET or internal provision)
- `204 No Content` (PUT)
- `403 Forbidden` when `memberId` does not match authenticated `sub` for non-admin roles
- `401 Unauthorized` when token/user id claim is missing
- `400 Bad Request` when `sub` is not a valid GUID or status value is invalid

---

## Support Requests

### POST `/api/SupportRequests/uploads`

Legacy alias: `POST /api/Requests/uploads`

Uploads one or more support request images to the configured S3-compatible bucket.

- Content type: `multipart/form-data`
- Form field: `files`
- Accepted file types: `jpg`, `jpeg`, `png`, `webp`, `gif`
- Max file size: `10 MB` per file
- Response `200 OK` result: `string[]` of uploaded bucket URLs

### POST `/api/SupportRequests/member/{memberId}`

Creates a support request.

- Path params:
  - `memberId` (guid; reporter/member id)
- Request body (`CreateSupportRequestDto`):

```json
{
  "area": "it",
  "categoryId": 3,
  "locationType": "building",
  "buildingId": 1,
  "placeType": "room",
  "roomId": 204,
  "areaDetails": null,
  "description": "Projector is not turning on",
  "urgency": "standard",
  "attachmentUrls": [
    "https://my-bucket.s3.eu-north-1.amazonaws.com/support/requests/attachments/photo1.jpg"
  ]
}
```

- Response `200 OK` result: `SupportRequestDetailDto`

### GET `/api/SupportRequests/member/{memberId}`

Gets requests created by a member.

- Path params:
  - `memberId` (guid)
- Response `200 OK` result: `SupportRequestListItemDto[]`

### GET `/api/SupportRequests/staff/{staffId}`

Gets requests assigned to a staff member.

- Path params:
  - `staffId` (guid)
- Response `200 OK` result: `SupportRequestListItemDto[]`

### GET `/api/SupportRequests`

Gets all requests with optional filters.

- Query params (`RequestFilterDto`):
  - `status` (string; `open`, `New`, `Assigned`, `InProgress`, `Completed`, `Cancelled`)
  - `categoryId` (int)
  - `search` (string)
  - `service` (string; `IT`, `FM`)
  - `priority` (string)
  - `sortBy` (string; `newest`, `oldest`)
  - `page` (int, default `1`)
  - `pageSize` (int, default `10`)
  - `memberId` (guid)
  - `staffId` (guid)

Current implementation note:
- The controller/service currently applies `status`, `categoryId`, `service`, `memberId`, and `staffId`.
- `search`, `priority`, `sortBy`, `page`, and `pageSize` are present in the DTO but are not used yet by the current service implementation.

- Response `200 OK` result: `SupportRequestListItemDto[]`

`SupportRequestListItemDto` shape:

```json
{
  "id": 1001,
  "ticketNo": "SR-2026-0001",
  "title": "Projector issue",
  "description": "Projector is not turning on",
  "category": "Hardware",
  "service": "IT",
  "isBuilding": true,
  "buildingId": 1,
  "isRoom": true,
  "roomId": 204,
  "areaDetails": null,
  "location": "Building A / Room 204",
  "status": "New",
  "urgency": "Standard",
  "isUrgent": false,
  "assignedStaffId": "b2c8de34-6f0d-4c4c-b319-4035c4d70f9f",
  "assignedTo": "Jane Staff",
  "unseen": true,
  "createdAt": "2026-04-13T09:00:00Z",
  "completedAt": null,
  "imgUrl": "https://example.com/photo1.jpg"
}
```

### GET `/api/SupportRequests/{requestId}`

Gets request details by id.

- Path params:
  - `requestId` (int)
- Response `200 OK` result (`SupportRequestDetailDto`):

```json
{
  "id": 1001,
  "ticketNo": "SR-2026-0001",
  "description": "Projector issue",
  "descriptionFull": "Projector in room 204 is not turning on.",
  "category": "Hardware",
  "service": "IT",
  "isBuilding": true,
  "buildingId": 1,
  "isRoom": true,
  "roomId": 204,
  "areaDetails": null,
  "location": "Building A / Room 204",
  "status": "Assigned",
  "isUrgent": false,
  "priority": "Standard",
  "assignedStaffId": "b2c8de34-6f0d-4c4c-b319-4035c4d70f9f",
  "assignedTo": "Jane Staff",
  "createdAt": "2026-04-13T09:00:00Z",
  "assignedAt": "2026-04-13T09:10:00Z",
  "startedAt": null,
  "completedAt": null,
  "cancelReason": null,
  "rating": null,
  "imgUrl": "https://example.com/photo1.jpg",
  "reporter": {
    "memberId": "7d75bf0d-c467-4ce7-8872-c58495a6ca8a",
    "fullName": "",
    "email": ""
  },
  "attachments": [
    {
      "id": 55,
      "url": "https://example.com/image.jpg"
    }
  ],
  "timeline": [
    {
      "step": "Assigned",
      "detail": "Assigned to staff member b2c8de34-6f0d-4c4c-b319-4035c4d70f9f.",
      "done": true,
      "createdAt": "2026-04-13T09:10:00Z"
    }
  ],
  "dispatcherInstructions": "Check HDMI cable first."
}
```

### GET `/api/SupportRequests/{requestId}/timeline`

Returns the request timeline.

- Path params:
  - `requestId` (int)
- Response `200 OK` result: `TimelineItemDto[]`

### PUT `/api/SupportRequests/{requestId}/cancel/member/{memberId}`

Cancels a request by member.

- Path params:
  - `requestId` (int)
  - `memberId` (guid)
- Request body (`CancelRequestDto`):

```json
{
  "reason": "Issue resolved by user."
}
```

- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/assign/dispatcher/{dispatcherId}`

Assigns a request to staff.

- Path params:
  - `requestId` (int)
  - `dispatcherId` (guid)
- Request body (`AssignRequestDto`):

```json
{
  "staffId": "b2c8de34-6f0d-4c4c-b319-4035c4d70f9f",
  "dispatcherInstructions": "Prioritize this request."
}
```

- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/reassign/dispatcher/{dispatcherId}`

Reassigns a request to another staff member.

- Path params:
  - `requestId` (int)
  - `dispatcherId` (guid)
- Request body (`AssignRequestDto`): same as assign endpoint
- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/dispatcher-instructions/dispatcher/{dispatcherId}`

Adds or updates dispatcher instructions.

- Path params:
  - `requestId` (int)
  - `dispatcherId` (guid)
- Request body (`DispatcherInstructionDto`):

```json
{
  "dispatcherInstructions": "Notify security if access is needed."
}
```

- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/start/staff/{staffId}`

Marks request as started.

- Path params:
  - `requestId` (int)
  - `staffId` (guid)
- Request body: none
- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/in-progress/staff/{staffId}`

Marks request as in progress.

- Path params:
  - `requestId` (int)
  - `staffId` (guid)
- Request body: none
- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/complete/staff/{staffId}`

Marks request as completed.

- Path params:
  - `requestId` (int)
  - `staffId` (guid)
- Request body: none
- Response: `204 No Content`

---

## Realtime

Hub endpoint:
- Gateway: `/support/hubs/support-updates`
- Local gateway: `http://localhost:5000/support/hubs/support-updates`
- Direct local: `http://localhost:5184/hubs/support-updates`
- Direct prod: `http://13.60.31.141:5008/hubs/support-updates`

JWT token is required for hub access.

