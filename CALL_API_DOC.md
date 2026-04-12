# Call Service API Documentation for Frontend

Last updated: 2026-04-13

## Service Status

Call Service is currently in scaffold/minimal state.

Implemented endpoint count: `1`
- `GET /weatherforecast`

## Base URLs

Server (when deployed on `http://13.60.196.144`):
- `http://13.60.196.144:5006` (direct `call-service` container)
- `http://13.60.196.144:5000` (API Gateway)

Local direct service URLs:
- `http://localhost:5053`
- `https://localhost:7092`
- `http://localhost:5006` (docker-compose mapped port)

IIS Express (local profile):
- `http://localhost:14527`
- `https://localhost:44365`

## API Gateway Status

Call Service **is exposed** via `ApiGateway`.
- Gateway route: `/call/{**catch-all}`
- Backend cluster destination (docker): `http://callservice:8080/`
- Compose service: `callservice` (`5006:8080`)

Frontend should use gateway URL:
- `http://13.60.196.144:5000/call/weatherforecast`

## Authentication

- Auth required: `No`
- JWT validation: `Not configured`
- Authorization header: `Not required`

## CORS

CORS policy is `AllowAll`:
- Any origin
- Any header
- Any method

## Endpoints

### 1) Get Weather Forecast

- Method: `GET`
- Path: `/weatherforecast`
- Auth: `No`

Request examples:

```http
GET /weatherforecast HTTP/1.1
Host: localhost:5053
Accept: application/json
```

```http
GET /weatherforecast HTTP/1.1
Host: 13.60.196.144:5006
Accept: application/json
```

```http
GET /call/weatherforecast HTTP/1.1
Host: 13.60.196.144:5000
Accept: application/json
```

Success response:
- `200 OK`

Response body (array of 5 items):

```json
[
  {
    "date": "2026-04-14",
    "temperatureC": 12,
    "summary": "Mild",
    "temperatureF": 53
  }
]
```

Response field details:
- `date` (`string`, date): forecast date
- `temperatureC` (`number`): temperature in Celsius
- `summary` (`string | null`): random weather summary text
- `temperatureF` (`number`): calculated as `32 + temperatureC / 0.5556`

Notes:
- Data is generated randomly per request.
- This endpoint is template/demo behavior, not call domain logic yet.

## Swagger/OpenAPI

Swagger UI is available in Development environment:
- `http://localhost:5053/swagger`
- `https://localhost:7092/swagger`
- `http://13.60.196.144:5006/swagger` (if call-service container is running on server)

## Frontend Integration Notes

- For frontend, prefer gateway base URL `http://13.60.196.144:5000` and call path `/call/weatherforecast`.
- Direct service URL `http://13.60.196.144:5006` is still available for diagnostics.
- Do not build production call flows around `/weatherforecast`; it is a placeholder endpoint.
- Expect schema/paths to change once real call features are implemented.
