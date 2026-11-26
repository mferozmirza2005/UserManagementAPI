# TechHive Solutions — User Management API

ASP.NET Core Web API (.NET 11) for managing internal users at TechHive Solutions. Covers full CRUD operations, input validation, debugging and performance optimizations, and a custom middleware pipeline for error handling, token authentication, and request/response auditing.

## Run locally

```bash
dotnet restore
dotnet build
dotnet run --launch-profile http
```

- API: `http://localhost:5016`
- Swagger UI: `http://localhost:5016/swagger`
- OpenAPI JSON: `http://localhost:5016/openapi/v1.json`

Development token (configured in `appsettings.json`):

```text
techhive-dev-token
```

Send it as `Authorization: Bearer techhive-dev-token` or `X-Api-Token: techhive-dev-token`.

## Endpoints

| Method | Path | Auth | Behavior |
|---|---|---|---|
| `GET` | `/api/users` | Required | Returns all users (`200 OK`) |
| `GET` | `/api/users/{id}` | Required | Returns one user (`200 OK`), `400` if `id <= 0`, `404` if missing |
| `POST` | `/api/users` | Required | Creates a user (`201 Created`) or `400` on invalid/duplicate input |
| `PUT` | `/api/users/{id}` | Required | Updates a user (`204 No Content`), `400` on invalid/duplicate/mismatch, `404` if missing |
| `DELETE` | `/api/users/{id}` | Required | Deletes a user (`204 No Content`), `400` if `id <= 0`, `404` if missing |
| `GET` | `/api/diagnostics/error` | Required | Intentionally throws an exception to verify error-handling middleware (`500`) |

Users are stored in a thread-safe in-memory store (`UserService`) combining a `List<User>` for ordered snapshots and a `Dictionary<int, User>` for `O(1)` ID lookups.

## Debugging & Optimization Summary

Issues identified during code review and debugging, along with the implemented fixes:

| Issue | Root Cause | Fix & Verification |
|---|---|---|
| Users created with blank or invalid data | Missing/incomplete validation allowed empty names, padded short values (e.g., `" a "`), and malformed emails | Added `[Required]`, `[StringLength]`, `[EmailAddress]`, `[RegularExpression]` on `User`, plus trimmed-length validation in `UsersController.TryValidateUser` |
| Duplicate emails under concurrent writes | Separate `EmailExists` and `Create`/`Update` calls released `_lock` between check and write | Replaced with atomic `TryCreate` and `TryUpdate` inside `_lock` in `UserService` |
| Missing/invalid user lookups failed unclearly | Unchecked IDs and missing records lacked standardized responses | Validated `id <= 0` (`400 Bad Request`) and missing users (`404 Not Found` JSON) across GET/PUT/DELETE |
| `GET /api/users/{id}` bottleneck & shared mutable state | Full list scans on ID lookups and returning mutable references | Added `Dictionary<int, User>` for `O(1)` lookups by ID and returned defensive copies (`CloneUser`) |
| Unhandled exceptions risked host crashes & mislogged status codes | No centralized exception barrier; inner logging middleware recorded `200` when an exception bubbled out | Added `ErrorHandlingMiddleware` returning `{ "error": "Internal server error." }` and updated `RequestResponseLoggingMiddleware` to record `500` on unhandled exceptions |

## Middleware Pipeline

Configured in `Program.cs` in the required order:

1. **`ErrorHandlingMiddleware`** — First in pipeline; catches unhandled exceptions and returns `{ "error": "Internal server error." }` with HTTP `500`.
2. **`TokenAuthenticationMiddleware`** — Second; validates `Authorization: Bearer <token>` or `X-Api-Token` using constant-time comparison (`CryptographicOperations.FixedTimeEquals`) and returns HTTP `401 Unauthorized` on missing/invalid tokens (Swagger/OpenAPI routes remain accessible).
3. **`RequestResponseLoggingMiddleware`** — Last among custom middleware; logs HTTP method, request path, final status code, and elapsed time in milliseconds.

## Example Verification Requests

```bash
# 401 without a token
curl -i http://localhost:5016/api/users

# 401 with an invalid token
curl -i http://localhost:5016/api/users \
  -H "Authorization: Bearer invalid-token"

# 200 with a valid token
curl -s http://localhost:5016/api/users \
  -H "Authorization: Bearer techhive-dev-token"

# 404 for a missing user
curl -i http://localhost:5016/api/users/99 \
  -H "Authorization: Bearer techhive-dev-token"

# 400 for invalid input
curl -i -X POST http://localhost:5016/api/users \
  -H "Authorization: Bearer techhive-dev-token" \
  -H "Content-Type: application/json" \
  -d '{"fullName":"","email":"not-an-email","department":""}'

# 500 JSON from error-handling middleware
curl -i http://localhost:5016/api/diagnostics/error \
  -H "Authorization: Bearer techhive-dev-token"
```
