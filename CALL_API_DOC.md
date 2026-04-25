# Call Service Realtime (SignalR) Documentation for Frontend

Last updated: 2026-04-25

## What Frontend Should Use

Use API Gateway for realtime call signaling:
- Base URL: `http://13.60.31.141:500`
- Gateway prefix: `/call`
- Hub URL: `http://13.60.31.141:500/call/hub`

Direct service (diagnostics only):
- `http://13.60.31.141:5006/hub`

Local equivalents:
- Gateway hub: `http://localhost:5000/call/hub`
- Direct call hub: `http://localhost:5006/hub`

Common 404 rule:
- If host is the deployed gateway (`:500`), call endpoints must include `/call`.
- Correct: `http://13.60.31.141:500/call/hub`
- Wrong: `http://13.60.31.141:500/hub`

## ICE Server Configuration (STUN/TURN)

CallService now issues short-lived TURN credentials for cross-network WebRTC calls.

HTTP endpoint (recommended):
- Gateway: `GET http://13.60.31.141:500/call/webrtc/ice-servers`
- Direct service: `GET http://13.60.31.141:5006/webrtc/ice-servers`
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
        "stun:13.60.31.141:3478",
        "turn:13.60.31.141:3478?transport=udp",
        "turn:13.60.31.141:3478?transport=tcp"
      ],
      "username": "1760944800:user-guid",
      "credential": "base64-hmac-value"
    }
  ]
}
```

Docker env keys used by current compose setup:
- `TURN_PUBLIC_HOST`: host/domain clients use in ICE URLs (default `13.60.31.141`)
- `TURN_EXTERNAL_IP`: public IP coturn advertises (default `13.60.31.141`, or NAT mapping `PUBLIC_IP/CONTAINER_IP`)
- `TURN_LISTENING_IP`: coturn listener interface (default `0.0.0.0`)
- `TURN_RELAY_IP`: coturn relay interface (default `0.0.0.0`)
- `TURN_SHARED_SECRET`: shared secret used by both coturn and CallService credential generation

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
curl -H "Authorization: Bearer <JWT>" http://localhost:5000/call/webrtc/ice-servers
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

- Any authenticated user can call `RequestCall`.
- Only dispatcher users can call `AcceptCall` and `RejectCall`.
- Caller who created the request can call `CancelCall`.
- WebRTC signaling methods require both connections to be in the same accepted room.
- `dispatcherUserId` in `RequestCall(dispatcherUserId)` must be the target dispatcher's JWT `sub`.

## Dispatcher Inbox Routing

- On hub connect, users with `dispatcher` role are automatically joined to:
  - `<DispatcherInboxPrefix><sub>`
- Default prefix is `dispatcher-inbox-`.
- Backend config key:
  - `Call:DispatcherInboxPrefix`
- Docker env key:
  - `Call__DispatcherInboxPrefix`
- Frontend env key (must match backend if overridden):
  - `VITE_DISPATCHER_INBOX_PREFIX`

## Hub Methods (Client -> Server)

### `RequestCall(dispatcherUserId)`
Creates a pending call invite from current user to a target dispatcher.

Parameters:
- `dispatcherUserId` (`string`, required)

Behavior:
- Creates `callId` and `roomId` internally.
- Sends `IncomingCall` to dispatcher inbox group:
  - `<DispatcherInboxPrefix><dispatcherUserId>`
- Sends `CallRinging` to caller user connections.
- Starts server-side timeout for invite expiration.

### `AcceptCall(callId)`
Dispatcher accepts a pending call.

Parameters:
- `callId` (`string`, required)

Behavior:
- Valid only for target dispatcher and pending status.
- Automatically joins caller connection and accepting dispatcher connection to room.
- Emits `JoinedRoom` and `CallAccepted`.

### `RejectCall(callId, reason?)`
Dispatcher rejects a pending call.

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
          "stun:13.60.31.141:3478",
          "turn:13.60.31.141:3478?transport=udp",
          "turn:13.60.31.141:3478?transport=tcp"
        ],
        "username": "1760944800:user-guid",
        "credential": "base64-hmac-value"
      }
    ]
  }
}
```

### `IncomingCall`
Sent to dispatcher when caller requests a call.

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
  "requestedAtUtc": "2026-04-15T10:00:00Z",
  "expiresAtUtc": "2026-04-15T10:00:30Z"
}
```

### `CallAccepted`
Sent to caller + dispatcher after successful acceptance.

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
Sent when dispatcher rejects invite.

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
  "reason": "No dispatcher response before timeout."
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
- `dispatcherUserId is required.`
- `Dispatcher is offline.`
- `Target user does not have dispatcher role.`
- `Only dispatchers can accept calls.`
- `Call request expired.`
- `You must be in a call room before sending signaling messages.`
- `Target connection is not online.`
- `Target connection is not in the same room.`

## Frontend Example (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

const accessToken = "<JWT_ACCESS_TOKEN>";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://13.60.31.141:500/call/hub", {
    accessTokenFactory: () => accessToken
  })
  .withAutomaticReconnect()
  .build();

connection.on("IncomingCall", ({ callId, fromUserId, fromDisplayName }) => {
  console.log("Incoming call:", callId, fromUserId, fromDisplayName);
  // dispatcher can accept/reject:
  // connection.invoke("AcceptCall", callId);
  // connection.invoke("RejectCall", callId, "Busy");
});

connection.on("JoinedRoom", ({ roomId, otherParticipants }) => {
  console.log("Joined room:", roomId, otherParticipants);
});

const iceResponse = await fetch("http://13.60.31.141:500/call/webrtc/ice-servers", {
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

Dispatcher id note:
- Replace `"dispatcher-user-id"` with the exact `sub` claim value from the dispatcher's JWT.

## Recommended Dispatcher Flow

1. Caller and dispatcher keep a single app-level SignalR connection to `/call/hub` with JWT (do not tie connection lifecycle to one page only).
2. Caller invokes `RequestCall(dispatcherUserId)`.
3. Dispatcher receives `IncomingCall` in real-time.
4. Dispatcher invokes `AcceptCall(callId)` or `RejectCall(callId, reason?)`.
5. On accept, fetch ICE config (`GET /call/webrtc/ice-servers`) or call `GetIceConfiguration()`.
6. Create `RTCPeerConnection({ iceServers })`.
7. Start WebRTC signaling using `CallAccepted` / `JoinedRoom` data.
8. During call, exchange `SendOffer` / `SendAnswer` / `SendIceCandidate`.
9. Use `LeaveCall()` for single participant leave or `EndCall()` to end for everyone.
10. If dispatcher takes no action, both sides receive `CallTimedOut`.

## Health Endpoints

- Gateway health route for call service container reachability test:
  - `GET http://13.60.31.141:500/call/health`
- Direct call service:
  - `GET http://13.60.31.141:5006/health`

