# MyAdaSupportService API Endpoints

Last updated: 2026-04-19

Base path (service): `/api`

Gateway route format (frontend):
- `http://51.20.193.29:5000/support/api/...`

Direct service format (diagnostics):
- Production: `http://51.20.193.29:5008/api/...`
- Local: `http://localhost:5008/api/...`

Common 404 rule:
- If host is gateway (`:5000`), `/support` prefix is required.

Authentication: Bearer JWT is required for all endpoints.

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

### String value conventions used in DTO comments

- `CreateSupportRequestDto.area`: `it`, `fm`
- `CreateSupportRequestDto.locationType`: `building`, `campus`
- `CreateSupportRequestDto.placeType`: `room`, `nonRoom`
- `CreateSupportRequestDto.urgency`: `standard`, `critical`
- `RequestFilterDto.status`: `open`, `completed`, `cancelled`
- `RequestFilterDto.service`: `IT`, `FM`
- `RequestFilterDto.sortBy`: `newest`, `oldest`
- `SupportStaffStatusDto.status` / `UpdateSupportStaffStatusDto.status`: `Online`, `Offline`, `OnBreak`

---

## Categories

### GET `/api/Categories`

Returns all categories.

- Request body: none
- Response `200 OK` result:

```json
[
  {
    "id": 1,
    "module": "IT",
    "name": "Network"
  }
]
```

### GET `/api/Categories/module/{module}`

Returns categories by module.

- Path params:
  - `module` (string; usually `IT` or `FM`)
- Response `200 OK` result: `CategoryDto[]` (same shape as above)

### POST `/api/Categories`

Creates a category.

- Request body (`CategoryCreateDto`):

```json
{
  "module": "IT",
  "name": "Hardware"
}
```

- Response `201 Created` result (`CategoryDto`):

```json
{
  "id": 10,
  "module": "IT",
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

Adds an attachment to a request.

Upload the image first through `POST /api/SupportRequests/uploads`, then pass the returned bucket URL here.

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

### GET `/api/SupportStaffStatuses/member/{memberId}`

Gets status by member id.

- Path params:
  - `memberId` (guid, must match authenticated user `sub`)
- Response `200 OK` result (`SupportStaffStatusDto`):

```json
{
  "id": 1,
  "memberId": "7d75bf0d-c467-4ce7-8872-c58495a6ca8a",
  "status": "Online",
  "updatedAt": "2026-04-13T10:00:00Z"
}
```

- If not found, service auto-creates with status `Offline`.
- If authenticated `sub` does not match path `memberId`, returns `403 Forbidden`.

### GET `/api/SupportStaffStatuses/status/{status}`

Gets all staff statuses by status.

- Path params:
  - `status` (string enum-like: `Online`, `Offline`, `OnBreak`)
- Response `200 OK` result: `SupportStaffStatusDto[]`
- Access: any authenticated user

### PUT `/api/SupportStaffStatuses/member/{memberId}`

Updates staff status.

- Path params:
  - `memberId` (guid, must match authenticated user `sub`)
- Request body (`UpdateSupportStaffStatusDto`):

```json
{
  "status": "OnBreak"
}
```

- Response: `204 No Content`

- If not found, service auto-creates status row with the requested status.
- If authenticated `sub` does not match path `memberId`, returns `403 Forbidden`.

### POST `/api/SupportStaffStatuses`

Creates support staff status.

- Roles: `support`, `dispatcher`, `staff`
- Request body (`CreateSupportStaffStatusDto`):

```json
{
  "memberId": "7d75bf0d-c467-4ce7-8872-c58495a6ca8a",
  "status": "Online"
}
```

- `memberId` must match authenticated user `sub`.
- `memberId = 00000000-0000-0000-0000-000000000000` means "use authenticated user id (`sub`)".
- Response: `201 Created`

---

## Support Requests

### POST `/api/SupportRequests/uploads`

Uploads one or more support request images to the configured S3-compatible bucket.

- Content type: `multipart/form-data`
- Form field: `files`
- Accepted file types: `jpg`, `jpeg`, `png`, `webp`, `gif`
- Max file size: `10 MB` per file
- Response `200 OK` result: `string[]` of uploaded bucket URLs

### POST `/api/SupportRequests/member/{memberId}`

Creates a support request.

- Path params:
  - `memberId` (int; reporter/member id)
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

- Response `200 OK` result: `SupportRequestDetailDto` (shape shown below)

### GET `/api/SupportRequests/member/{memberId}`

Gets requests created by a member.

- Path params:
  - `memberId` (int)
- Response `200 OK` result: `SupportRequestListItemDto[]`

### GET `/api/SupportRequests/staff/{staffId}`

Gets requests assigned to a staff member.

- Path params:
  - `staffId` (int)
- Response `200 OK` result: `SupportRequestListItemDto[]`

### GET `/api/SupportRequests`

Gets all requests with optional filters.

- Query params (`RequestFilterDto`):
  - `status` (string; `open`, `completed`, `cancelled`)
  - `categoryId` (int)
  - `search` (string)
  - `service` (string; `IT`, `FM`)
  - `priority` (string)
  - `sortBy` (string; `newest`, `oldest`)
  - `page` (int, default `1`)
  - `pageSize` (int, default `10`)
  - `memberId` (int)
  - `staffId` (int)
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
  "location": "Building A / Room 204",
  "status": "New",
  "urgency": "Standard",
  "isUrgent": false,
  "assignedTo": "Jane Staff",
  "unseen": true,
  "createdAt": "2026-04-13T09:00:00Z",
  "completedAt": null
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
  "location": "Building A / Room 204",
  "status": "Assigned",
  "isUrgent": false,
  "priority": "Standard",
  "assignedTo": "Jane Staff",
  "createdAt": "2026-04-13T09:00:00Z",
  "assignedAt": "2026-04-13T09:10:00Z",
  "startedAt": null,
  "completedAt": null,
  "cancelReason": null,
  "rating": null,
  "reporter": {
    "memberId": 3001,
    "fullName": "John Member",
    "email": "john@example.com"
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
      "detail": "Assigned to Jane Staff",
      "done": true,
      "createdAt": "2026-04-13T09:10:00Z"
    }
  ],
  "dispatcherInstructions": "Check HDMI cable first."
}
```

### PUT `/api/SupportRequests/{requestId}/cancel/member/{memberId}`

Cancels a request by member.

- Path params:
  - `requestId` (int)
  - `memberId` (int)
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
  - `dispatcherId` (int)
- Request body (`AssignRequestDto`):

```json
{
  "staffId": 9001,
  "dispatcherInstructions": "Prioritize this request."
}
```

- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/reassign/dispatcher/{dispatcherId}`

Reassigns a request to another staff member.

- Path params:
  - `requestId` (int)
  - `dispatcherId` (int)
- Request body (`AssignRequestDto`): same as assign endpoint
- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/dispatcher-instructions/dispatcher/{dispatcherId}`

Adds or updates dispatcher instructions.

- Path params:
  - `requestId` (int)
  - `dispatcherId` (int)
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
  - `staffId` (int)
- Request body: none
- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/in-progress/staff/{staffId}`

Marks request as in progress.

- Path params:
  - `requestId` (int)
  - `staffId` (int)
- Request body: none
- Response: `204 No Content`

### PUT `/api/SupportRequests/{requestId}/complete/staff/{staffId}`

Marks request as completed.

- Path params:
  - `requestId` (int)
  - `staffId` (int)
- Request body: none
- Response: `204 No Content`

