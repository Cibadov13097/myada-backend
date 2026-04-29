# Call Service API Documentation for Frontend

Last updated: 2026-04-29

## What Frontend Should Use

Use API Gateway for realtime call signaling:
- Base URL: `https://myada.duckdns.org`
- Gateway prefix: `/call`
- Hub URL: `https://myada.duckdns.org/call/hub`
- Swagger URL (when enabled): `https://myada.duckdns.org/call/swagger/index.html`

Direct service (local / diagnostics only, not recommended for frontend):
- `http://localhost:5006/hub`

Local equivalents:
- Gateway hub: `http://localhost:5000/call/hub`
- Direct call hub: `http://localhost:5006/hub`

Common 404 rule:
- If host is the deployed gateway, call endpoints must include `/call`.
- Correct: `https://myada.duckdns.org/call/hub`
- Wrong: `https://myada.duckdns.org/hub`

## ICE Server Configuration (STUN/TURN)

CallService now issues short-lived TURN credentials for cross-network WebRTC calls.

HTTP endpoint (recommended):
- Gateway: `GET https://myada.duckdns.org/call/webrtc/ice-servers`
- Direct service (local / diagnostics): `GET http://localhost:5006/webrtc/ice-servers`
- Local gateway: `GET http://localhost:5000/call/webrtc/ice-servers`
- Local direct: `GET http://localhost:5006/webrtc/ice-servers`

SignalR options:
- `Connected` event includes `iceConfiguration`.
- Hub method `GetIceConfiguration()` returns the same payload shape.

Authentication:
- Requires JWT bearer token.

Payload example:
```json
{
  "generatedAtUtc": "2026-04-20T09:00:00Z",
  "expiresAtUtc": "2026-04-20T10:00:00Z",
  "iceServers": [
    {
      "urls": [
        "stun:myada.duckdns.org:3478",
        "turn:myada.duckdns.org:3478?transport=udp",
        "turn:myada.duckdns.org:3478?transport=tcp"
      ],
      "username": "1760944800:user-guid",
      "credential": "base64-hmac-value"
    }
  ]
}
```

Docker env keys used by current compose setup:
- `TURN_PUBLIC_HOST`: host/domain clients use in ICE URLs. For public deployment, set this to `myada.duckdns.org`.
- `TURN_EXTERNAL_IP`: public IP coturn advertises, or NAT mapping `PUBLIC_IP/CONTAINER_IP`
- `TURN_LISTENING_IP`: coturn listener interface (default `0.0.0.0`)
- `TURN_RELAY_IP`: coturn relay interface (default `0.0.0.0`)
- `TURN_SHARED_SECRET`: shared secret used by both coturn and CallService credential generation

Important runtime note:
- The actual `iceServers[].urls` values come from the service's `Turn:Urls` configuration.
- If the live API still returns `13.60.31.141` in ICE URLs, update `TURN_PUBLIC_HOST` or `Turn__Urls__*` in deployment config.

## Frontend Impact When Coturn Is Added

UI redesign is not required.

Frontend WebRTC setup must ensure it uses backend-issued ICE config:
- If your frontend already fetches `GET /call/webrtc/ice-servers` (or calls hub `GetIceConfiguration()`) and passes `iceServers` into `RTCPeerConnection`, no code change is required.
- If your frontend currently uses hardcoded STUN/TURN (or no TURN), update call setup to use dynamic ICE config from backend before creating `RTCPeerConnection`.

Minimum expected client flow:
1. Get JWT token.
2. Fetch `GET /call/webrtc/ice-servers`.
3. Create `new RTCPeerConnection({ iceServers })`.
4. Continue signaling via `SendOffer` / `SendAnswer` / `SendIceCandidate`.

## Coturn + Backend Runtime Verification

Run these checks on deployment host:

```bash
docker compose ps coturn callservice apigateway
docker logs --tail=100 myada-coturn
docker logs --tail=100 call-service
```

Verify ICE endpoint response is non-empty:

```bash
curl -H "Authorization: Bearer <JWT>" https://myada.duckdns.org/call/webrtc/ice-servers
```

Expected:
- `iceServers` contains at least one entry.
- Entry has `urls` with `stun:` and/or `turn:`.
- Entry includes non-empty `username` and `credential`.

Production requirement:
- Do not leave `TURN_PUBLIC_HOST=localhost` or `TURN_EXTERNAL_IP=127.0.0.1` in public deployments.
- Set both to your reachable public domain/IP so external clients can use TURN.

## Transport

SignalR negotiate endpoint:
- `POST /call/hub/negotiate?negotiateVersion=1`

Available transports:
- WebSockets (preferred)
- ServerSentEvents
- LongPolling

## Authentication

Hub connection requires JWT bearer authentication.

Header (negotiate / long polling / SSE):

```http
Authorization: Bearer <accessToken>
```

WebSocket connections should send token through SignalR `accessTokenFactory` (query `access_token`).

Token validation settings:
- Issuer: `myada-auth`
- Audience: `myada-api`
- Signing source: AuthService JWKS (`/.well-known/jwks.json`)

## Authorization Rules

- Any authenticated non-student user can call any online non-student user with `RequestCall`.
- Students cannot place calls.
- Students cannot receive calls.
- The called user can call `AcceptCall` and `RejectCall`.
- Caller who created the request can call `CancelCall`.
- WebRTC signaling methods require both connections to be in the same accepted room.
- `RequestCall(targetUserId)` must receive the called user's JWT `sub`.

## Persisted Call History

CallService now saves call history to PostgreSQL.

Saved fields include:
- call id and room id
- caller user id + display name
- dispatcher user id + display name
  This is a legacy field name and stores the called user's identity, even when the called user is not a dispatcher.
- request status (`pending`, `accepted`, `rejected`, `cancelled`, `timed-out`)
- requested / accepted / resolved timestamps
- call end time and duration in seconds for accepted calls
- resolve reason and end reason when present

Status behavior:
- Accepted calls keep status `accepted` in history.
- When an accepted call finishes, `endedAtUtc` and `durationSeconds` are filled.
- Rejected, cancelled, and timed-out requests are also persisted.

REST history endpoints:
- `GET /call/api/call-history`
- `GET /call/api/call-history/{callId}`
- Alias route also works: `GET /call/api/calls/history`

History query options:
- `status` = `pending`, `accepted`, `rejected`, `cancelled`, or `timed-out`
- `limit` = max number of rows to return, default `50`

History endpoint auth:
- Requires JWT bearer token.
- Returns only calls where current user is the caller or called user.

## Call History REST API

Recommended gateway URLs:
- List history: `GET https://myada.duckdns.org/call/api/call-history`
- Get single call: `GET https://myada.duckdns.org/call/api/call-history/{callId}`
- Alias list route: `GET https://myada.duckdns.org/call/api/calls/history`

Direct service URLs (local / diagnostics):
- List history: `GET http://localhost:5006/api/call-history`
- Get single call: `GET http://localhost:5006/api/call-history/{callId}`
- Alias list route: `GET http://localhost:5006/api/calls/history`

Local equivalents:
- Gateway list history: `GET http://localhost:5000/call/api/call-history`
- Gateway single call: `GET http://localhost:5000/call/api/call-history/{callId}`
- Direct list history: `GET http://localhost:5006/api/call-history`
- Direct single call: `GET http://localhost:5006/api/call-history/{callId}`

### `GET /api/call-history`
Returns call history for the authenticated user.

Query parameters:
- `status` (`string`, optional): `pending`, `accepted`, `rejected`, `cancelled`, `timed-out`
- `limit` (`number`, optional): default `50`, min `1`, max `200`

Auth:
- JWT bearer token required

Behavior:
- Returns calls where current user is either the caller or called user
- Ordered by newest `requestedAtUtc` first

Example request:

```http
GET /call/api/call-history?status=accepted&limit=20 HTTP/1.1
Authorization: Bearer <accessToken>
```

Example response:

```json
[
  {
    "callId": "6d5fd3a6e76d4120bd70d4f8d81d4b66",
    "roomId": "call-6d5fd3a6e76d4120bd70d4f8d81d4b66",
    "status": "accepted",
    "caller": {
      "userId": "member-user-id",
      "displayName": "Ali"
    },
    "dispatcher": {
      "userId": "dispatcher-user-id",
      "displayName": "Dispatcher A"
    },
    "requestedAtUtc": "2026-04-25T10:00:00Z",
    "acceptedAtUtc": "2026-04-25T10:00:08Z",
    "endedAtUtc": "2026-04-25T10:12:41Z",
    "durationSeconds": 753,
    "resolvedAtUtc": "2026-04-25T10:00:08Z",
    "resolveReason": null,
    "endReason": "Call ended explicitly."
  }
]
```

### `GET /api/call-history/{callId}`
Returns one call-history record for the authenticated user.

Route parameters:
- `callId` (`string`, required)

Auth:
- JWT bearer token required

Behavior:
- Returns `404` if call does not exist
- Returns `404` if call exists but current user is not a participant

Example request:

```http
GET /call/api/call-history/6d5fd3a6e76d4120bd70d4f8d81d4b66 HTTP/1.1
Authorization: Bearer <accessToken>
```

Example response:

```json
{
  "callId": "6d5fd3a6e76d4120bd70d4f8d81d4b66",
  "roomId": "call-6d5fd3a6e76d4120bd70d4f8d81d4b66",
  "status": "rejected",
  "caller": {
    "userId": "member-user-id",
    "displayName": "Ali"
  },
  "dispatcher": {
    "userId": "dispatcher-user-id",
    "displayName": "Dispatcher A"
  },
  "requestedAtUtc": "2026-04-25T11:00:00Z",
  "acceptedAtUtc": null,
  "endedAtUtc": null,
  "durationSeconds": null,
  "resolvedAtUtc": "2026-04-25T11:00:12Z",
  "resolveReason": "Busy",
  "endReason": null
}
```

### History Response Fields

- `callId`: unique call request id
- `roomId`: realtime room id used by SignalR/WebRTC flow
- `status`: final or current request outcome
- `caller`: `{ userId, displayName }`
- `dispatcher`: `{ userId, displayName }`
  Legacy response field name for the called user.
- `requestedAtUtc`: when invite was created
- `acceptedAtUtc`: when the called user accepted the call
- `endedAtUtc`: when an accepted call finished
- `durationSeconds`: total accepted-call duration in seconds
- `resolvedAtUtc`: when the request reached its status
- `resolveReason`: reject / cancel / timeout reason when available
- `endReason`: accepted-call ending reason when available

### History Error Behavior

- `400 Bad Request`: invalid `status` query or missing `callId`
- `401 Unauthorized`: JWT missing or invalid
- `404 Not Found`: call record not found for current user

## Dispatcher Inbox Routing

- On hub connect, users with `dispatcher` role are automatically joined to:
  - `<DispatcherInboxPrefix><sub>`
- This group is still exposed in the `Connected` payload for dispatcher presence/debugging.
- Incoming call notifications are delivered with `Clients.User(targetUserId)`, so non-dispatcher callees can also receive `IncomingCall`.
- Default prefix is `dispatcher-inbox-`.
- Backend config key:
  - `Call:DispatcherInboxPrefix`
- Docker env key:
  - `Call__DispatcherInboxPrefix`
- Frontend env key (must match backend if overridden):
  - `VITE_DISPATCHER_INBOX_PREFIX`

## Hub Methods (Client -> Server)

### `RequestCall(targetUserId)`
Creates a pending call invite from current user to a target user.

Parameters:
- `targetUserId` (`string`, required)

Behavior:
- Creates `callId` and `roomId` internally.
- Allowed call matrix:
  - any authenticated non-student user -> any authenticated non-student user
  - students cannot place calls
  - students cannot receive calls
- Sends `IncomingCall` to all SignalR connections of the target user.
- Sends `CallRinging` to caller user connections.
- Starts server-side timeout for invite expiration.

### `AcceptCall(callId)`
The called user accepts a pending call.

Parameters:
- `callId` (`string`, required)

Behavior:
- Valid only for the called user and pending status.
- Automatically joins caller connection and accepting callee connection to room.
- Emits `JoinedRoom` and `CallAccepted`.

### `RejectCall(callId, reason?)`
The called user rejects a pending call.

Parameters:
- `callId` (`string`, required)
- `reason` (`string`, optional)

### `CancelCall(callId, reason?)`
Caller cancels a pending call before acceptance.

Parameters:
- `callId` (`string`, required)
- `reason` (`string`, optional)

### `LeaveCall()`
Leaves current room for current connection.

### `SendOffer(targetConnectionId, sdp)`
Send WebRTC offer SDP to target participant in same room.

Parameters:
- `targetConnectionId` (`string`, required)
- `sdp` (`string`, required)

### `SendAnswer(targetConnectionId, sdp)`
Send WebRTC answer SDP to target participant in same room.

Parameters:
- `targetConnectionId` (`string`, required)
- `sdp` (`string`, required)

### `SendIceCandidate(targetConnectionId, candidate, sdpMid, sdpMLineIndex)`
Send ICE candidate to target participant in same room.

Parameters:
- `targetConnectionId` (`string`, required)
- `candidate` (`string`, required)
- `sdpMid` (`string | null`, optional)
- `sdpMLineIndex` (`number | null`, optional)

### `EndCall()`
Broadcasts `CallEnded` to the room and removes room membership for all current participants.

### `GetIceConfiguration()`
Returns TURN/STUN ICE configuration for the current authenticated user.

Parameters:
- none

Returns:
- same payload shape as `GET /webrtc/ice-servers`

## Server Events (Server -> Client)

### `Connected`
Emitted right after successful authenticated hub connection.

Payload:
```json
{
  "connectionId": "abc123",
  "userId": "user-guid",
  "displayName": "Ali",
  "isDispatcher": false,
  "dispatcherInboxGroup": null,
  "roles": ["student"],
  "iceConfiguration": {
    "generatedAtUtc": "2026-04-20T09:00:00Z",
    "expiresAtUtc": "2026-04-20T10:00:00Z",
    "iceServers": [
      {
        "urls": [
          "stun:myada.duckdns.org:3478",
          "turn:myada.duckdns.org:3478?transport=udp",
          "turn:myada.duckdns.org:3478?transport=tcp"
        ],
        "username": "1760944800:user-guid",
        "credential": "base64-hmac-value"
      }
    ]
  }
}
```

### `IncomingCall`
Sent to the called user when caller requests a call.

Payload:
```json
{
  "callId": "f9f2...",
  "roomId": "call-f9f2...",
  "fromUserId": "caller-user-id",
  "fromConnectionId": "caller-connection-id",
  "fromDisplayName": "Ali",
  "requestedAtUtc": "2026-04-15T10:00:00Z",
  "expiresAtUtc": "2026-04-15T10:00:30Z"
}
```

### `CallRinging`
Sent to caller right after request is created.

Payload:
```json
{
  "callId": "f9f2...",
  "roomId": "call-f9f2...",
  "dispatcherUserId": "dispatcher-user-id",
  "targetUserId": "dispatcher-user-id",
  "requestedAtUtc": "2026-04-15T10:00:00Z",
  "expiresAtUtc": "2026-04-15T10:00:30Z"
}
```

Notes:
- `targetUserId` is the canonical field for the called user's id.
- `dispatcherUserId` is kept for backward compatibility and contains the same value.

### `CallAccepted`
Sent to caller + called user after successful acceptance.

Payload:
```json
{
  "callId": "f9f2...",
  "roomId": "call-f9f2...",
  "acceptedByUserId": "dispatcher-user-id",
  "acceptedByConnectionId": "dispatcher-connection-id",
  "acceptedAtUtc": "2026-04-15T10:00:05Z",
  "participants": [
    {
      "connectionId": "caller-connection-id",
      "userId": "caller-user-id",
      "displayName": "Ali"
    },
    {
      "connectionId": "dispatcher-connection-id",
      "userId": "dispatcher-user-id",
      "displayName": "Dispatcher A"
    }
  ]
}
```

### `CallRejected`
Sent when the called user rejects invite.

Payload:
```json
{
  "callId": "f9f2...",
  "roomId": "call-f9f2...",
  "rejectedByUserId": "dispatcher-user-id",
  "rejectedAtUtc": "2026-04-15T10:00:08Z",
  "reason": "Busy"
}
```

### `CallCancelled`
Sent when caller cancels pending invite.

Payload:
```json
{
  "callId": "f9f2...",
  "roomId": "call-f9f2...",
  "cancelledByUserId": "caller-user-id",
  "cancelledAtUtc": "2026-04-15T10:00:07Z",
  "reason": "No longer needed"
}
```

### `CallTimedOut`
Sent when pending invite expires without accept/reject.

Payload:
```json
{
  "callId": "f9f2...",
  "roomId": "call-f9f2...",
  "timedOutAtUtc": "2026-04-15T10:00:30Z",
  "reason": "No response before timeout."
}
```

### `JoinedRoom`
Sent to each joined participant after accept.

Payload:
```json
{
  "roomId": "call-f9f2...",
  "connectionId": "self-connection-id",
  "userId": "self-user-id",
  "displayName": "Ali",
  "otherParticipants": [
    {
      "connectionId": "peer-connection-id",
      "userId": "peer-user-id",
      "displayName": "Dispatcher A"
    }
  ]
}
```

### `ParticipantLeft`
Sent to other room participants when someone disconnects/leaves room.

Payload:
```json
{
  "connectionId": "peer-connection-id",
  "userId": "peer-user-id",
  "displayName": "Ali"
}
```

### `LeftRoom`
Sent to caller after `LeaveCall()`.

Payload:
```json
{
  "roomId": "call-f9f2...",
  "connectionId": "self-connection-id"
}
```

### `ReceiveOffer`
Sent to target participant.

Payload:
```json
{
  "roomId": "call-f9f2...",
  "fromConnectionId": "peer-connection-id",
  "fromUserId": "peer-user-id",
  "sdp": "v=0..."
}
```

### `ReceiveAnswer`
Sent to target participant.

Payload:
```json
{
  "roomId": "call-f9f2...",
  "fromConnectionId": "peer-connection-id",
  "fromUserId": "peer-user-id",
  "sdp": "v=0..."
}
```

### `ReceiveIceCandidate`
Sent to target participant.

Payload:
```json
{
  "roomId": "call-f9f2...",
  "fromConnectionId": "peer-connection-id",
  "fromUserId": "peer-user-id",
  "candidate": "candidate:...",
  "sdpMid": "0",
  "sdpMLineIndex": 0
}
```

### `CallEnded`
Broadcast to all room participants when `EndCall()` is called.

Payload:
```json
{
  "roomId": "call-f9f2...",
  "endedByConnectionId": "peer-connection-id",
  "endedByUserId": "peer-user-id",
  "endedAtUtc": "2026-04-15T10:05:00Z"
}
```

## Error Behavior

Hub errors are returned as SignalR `HubException` messages, for example:
- `targetUserId is required.`
- `Target user is offline.`
- `Students cannot place calls.`
- `Students cannot receive calls.`
- `Call request expired.`
- `You must be in a call room before sending signaling messages.`
- `Target connection is not online.`
- `Target connection is not in the same room.`

## Frontend Example (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

const accessToken = "<JWT_ACCESS_TOKEN>";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://myada.duckdns.org/call/hub", {
    accessTokenFactory: () => accessToken
  })
  .withAutomaticReconnect()
  .build();

connection.on("IncomingCall", ({ callId, fromUserId, fromDisplayName }) => {
  console.log("Incoming call:", callId, fromUserId, fromDisplayName);
  // the called user can accept/reject:
  // connection.invoke("AcceptCall", callId);
  // connection.invoke("RejectCall", callId, "Busy");
});

connection.on("JoinedRoom", ({ roomId, otherParticipants }) => {
  console.log("Joined room:", roomId, otherParticipants);
});

const iceResponse = await fetch("https://myada.duckdns.org/call/webrtc/ice-servers", {
  headers: {
    Authorization: `Bearer ${accessToken}`
  }
});
const { iceServers } = await iceResponse.json();
const peerConnection = new RTCPeerConnection({ iceServers });

connection.on("ReceiveOffer", async ({ fromConnectionId, sdp }) => {
  // set remote offer + create/send answer
  await connection.invoke("SendAnswer", fromConnectionId, "answer-sdp");
});

await connection.start();
await connection.invoke("RequestCall", "dispatcher-user-id");
```

Target id note:
- Replace `"dispatcher-user-id"` with the exact `sub` claim value from the user you are calling.

## Recommended Call Flow

1. Caller and callee keep a single app-level SignalR connection to `/call/hub` with JWT (do not tie connection lifecycle to one page only).
2. Caller invokes `RequestCall(targetUserId)`.
3. The called user receives `IncomingCall` in real-time.
4. The called user invokes `AcceptCall(callId)` or `RejectCall(callId, reason?)`.
5. On accept, fetch ICE config (`GET /call/webrtc/ice-servers`) or call `GetIceConfiguration()`.
6. Create `RTCPeerConnection({ iceServers })`.
7. Start WebRTC signaling using `CallAccepted` / `JoinedRoom` data.
8. During call, exchange `SendOffer` / `SendAnswer` / `SendIceCandidate`.
9. Use `LeaveCall()` for single participant leave or `EndCall()` to end for everyone.
10. If the called user takes no action, both sides receive `CallTimedOut`.

## Health Endpoints

- Gateway health route for call service container reachability test:
  - `GET https://myada.duckdns.org/call/health`
- Direct call service:
  - `GET http://localhost:5006/health`

