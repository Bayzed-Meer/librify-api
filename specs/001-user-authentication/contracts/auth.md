# API Contracts: Auth Endpoints

All error responses use RFC 7807 Problem Details via `Problem()` / `ValidationProblem()`.

---

## POST /api/auth/register

**Request body**

```json
{
  "displayName": "Jane Doe",
  "email": "jane@example.com",
  "password": "Passw0rd!"
}
```

| Field | Rules |
|-------|-------|
| `displayName` | Required, 1–100 chars |
| `email` | Required, valid email format, max 256 chars |
| `password` | Required, ≥8 chars, ≥1 uppercase, ≥1 lowercase, ≥1 digit |

**Responses**

| Status | Description | Body |
|--------|-------------|------|
| `201 Created` | Account created | `{ "userId": "<guid>", "email": "jane@example.com" }` |
| `400 Bad Request` | Validation failure | `ValidationProblem` |
| `409 Conflict` | Email already registered | `Problem` with `detail: "Email already in use."` |

---

## POST /api/auth/login

**Request body**

```json
{
  "email": "jane@example.com",
  "password": "Passw0rd!"
}
```

**Responses**

| Status | Description | Body |
|--------|-------------|------|
| `200 OK` | Credentials valid | `{ "accessToken": "<jwt>", "refreshToken": "<32-hex>", "expiresAt": "<iso8601>" }` |
| `400 Bad Request` | Missing/malformed body | `ValidationProblem` |
| `401 Unauthorized` | Wrong email or password | `Problem` with `detail: "Invalid credentials."` (same response for both cases) |
| `429 Too Many Requests` | Rate limit exceeded | `Problem` with `detail: "Too many failed login attempts. Try again later."` |

---

## POST /api/auth/refresh

**Request body**

```json
{
  "refreshToken": "<32-hex-guid>"
}
```

**Responses**

| Status | Description | Body |
|--------|-------------|------|
| `200 OK` | Token rotated | `{ "accessToken": "<jwt>", "refreshToken": "<new-32-hex>", "expiresAt": "<iso8601>" }` |
| `400 Bad Request` | Missing token | `ValidationProblem` |
| `401 Unauthorized` | Expired, revoked, or consumed token | `Problem` with `detail: "Invalid or expired refresh token."` |

Note: a consumed token (reuse detection) triggers full family invalidation before returning 401 (FR-013).

---

## POST /api/auth/logout

**Authorization**: `Bearer <access_token>` (required)

**Request body**

```json
{
  "refreshToken": "<32-hex-guid>"
}
```

**Responses**

| Status | Description | Body |
|--------|-------------|------|
| `204 No Content` | Refresh token revoked | — |
| `400 Bad Request` | Missing token | `ValidationProblem` |
| `401 Unauthorized` | Missing/invalid access token or invalid refresh token | `Problem` |

---

## Common Error Shape (RFC 7807)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Invalid credentials."
}
```
