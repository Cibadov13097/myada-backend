# Call Service Realtime (SignalR) Documentation for Frontend

Last updated: 2026-04-13

## What Frontend Should Use

Use API Gateway for realtime call signaling:
- Base URL: `http://13.60.196.144:5000`
- Hub URL: `http://13.60.196.144:5000/call/hub`

Direct service (for diagnostics only):
- `http://13.60.196.144:5006/hub`

## Transport

SignalR negotiate endpoint:
- `POST /call/hub/negotiate?negotiateVersion=1`

Available transports:
- WebSockets (preferred)
- ServerSentEvents
- LongPolling

## Authentication

Current implementation:
- No auth required on hub connection
- No bearer token required

## Hub Methods (Client -> Server)

### `JoinRoom(roomId, userId, displayName?)`
Join a call room. If the connection is already in another room, it is moved automatically.

Parameters:
- `roomId` (`string`, required)
- `userId` (`string`, required)
- `displayName` (`string`, optional)

### `LeaveRoom()`
Leave current room.

### `SendOffer(targetConnectionId, sdp)`
Send WebRTC offer SDP to a target participant in the same room.

Parameters:
- `targetConnectionId` (`string`, required)
- `sdp` (`string`, required)

### `SendAnswer(targetConnectionId, sdp)`
Send WebRTC answer SDP to a target participant in the same room.

Parameters:
- `targetConnectionId` (`string`, required)
- `sdp` (`string`, required)

### `SendIceCandidate(targetConnectionId, candidate, sdpMid, sdpMLineIndex)`
Send ICE candidate to a target participant in the same room.

Parameters:
- `targetConnectionId` (`string`, required)
- `candidate` (`string`, required)
- `sdpMid` (`string | null`, optional)
- `sdpMLineIndex` (`number | null`, optional)

### `EndCall()`
Broadcast call end event to all participants in current room.

## Server Events (Server -> Client)

### `Connected`
Emitted right after connection is established.

Payload:
```json
{ "connectionId": "abc123" }
```

### `JoinedRoom`
Emitted to caller after successful `JoinRoom`.

Payload shape:
```json
{
  "roomId": "room-42",
  "connectionId": "self-connection-id",
  "userId": "u1",
  "displayName": "Alice",
  "otherParticipants": [
    {
      "connectionId": "peer-connection-id",
      "userId": "u2",
      "displayName": "Bob"
    }
  ]
}
```

### `ParticipantJoined`
Broadcast to others in room when a new participant joins.

Payload:
```json
{
  "connectionId": "peer-connection-id",
  "userId": "u2",
  "displayName": "Bob"
}
```

### `ParticipantLeft`
Broadcast to others when a participant leaves/disconnects.

Payload:
```json
{
  "connectionId": "peer-connection-id",
  "userId": "u2",
  "displayName": "Bob"
}
```

### `LeftRoom`
Sent to caller after `LeaveRoom()`.

Payload:
```json
{
  "roomId": "room-42",
  "connectionId": "self-connection-id"
}
```

### `ReceiveOffer`
Sent to target participant.

Payload:
```json
{
  "roomId": "room-42",
  "fromConnectionId": "peer-connection-id",
  "fromUserId": "u2",
  "sdp": "v=0..."
}
```

### `ReceiveAnswer`
Sent to target participant.

Payload:
```json
{
  "roomId": "room-42",
  "fromConnectionId": "peer-connection-id",
  "fromUserId": "u2",
  "sdp": "v=0..."
}
```

### `ReceiveIceCandidate`
Sent to target participant.

Payload:
```json
{
  "roomId": "room-42",
  "fromConnectionId": "peer-connection-id",
  "fromUserId": "u2",
  "candidate": "candidate:...",
  "sdpMid": "0",
  "sdpMLineIndex": 0
}
```

### `CallEnded`
Broadcast to the room when `EndCall()` is called.

Payload:
```json
{
  "roomId": "room-42",
  "endedByConnectionId": "peer-connection-id",
  "endedByUserId": "u2"
}
```

## Error Behavior

Hub errors are returned as SignalR `HubException` messages, for example:
- `roomId is required.`
- `userId is required.`
- `You must join a room before sending signaling messages.`
- `Target connection is not online.`
- `Target connection is not in the same room.`

## Frontend Example (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://13.60.196.144:5000/call/hub")
  .withAutomaticReconnect()
  .build();

connection.on("Connected", ({ connectionId }) => {
  console.log("Connected:", connectionId);
});

connection.on("JoinedRoom", (payload) => {
  console.log("JoinedRoom", payload);
});

connection.on("ParticipantJoined", (participant) => {
  console.log("ParticipantJoined", participant);
});

connection.on("ParticipantLeft", (participant) => {
  console.log("ParticipantLeft", participant);
});

connection.on("ReceiveOffer", async ({ fromConnectionId, sdp }) => {
  // set remote offer + create/send answer
  await connection.invoke("SendAnswer", fromConnectionId, "answer-sdp");
});

connection.on("ReceiveAnswer", ({ fromConnectionId, sdp }) => {
  // apply remote answer
});

connection.on("ReceiveIceCandidate", ({ candidate, sdpMid, sdpMLineIndex }) => {
  // add ICE candidate to RTCPeerConnection
});

connection.on("CallEnded", ({ roomId, endedByUserId }) => {
  console.log(`Call ended in ${roomId} by ${endedByUserId}`);
});

await connection.start();
await connection.invoke("JoinRoom", "room-42", "user-123", "Ali");
```

## Recommended Call Flow

1. Connect hub.
2. Wait for `Connected`.
3. Call `JoinRoom(roomId, userId, displayName)`.
4. Use `otherParticipants` from `JoinedRoom` to start peer offers.
5. Exchange `SendOffer` / `ReceiveOffer`, `SendAnswer` / `ReceiveAnswer`, and ICE candidates.
6. On leave, call `LeaveRoom()`.
7. On call finish, call `EndCall()`.

## Health Endpoints

- Gateway health route for call service container reachability test:
  - `GET http://13.60.196.144:5000/call/health`
- Direct call service:
  - `GET http://13.60.196.144:5006/health`
