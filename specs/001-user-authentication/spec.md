# Feature Specification: User Authentication

**Feature Branch**: `001-user-authentication`  
**Created**: 2026-05-06  
**Status**: Complete  
**Input**: User description: "authentication"

## Clarifications

### Session 2026-05-06

- Q: Should refresh token rotation be implemented when a refresh token is used? → A: Always rotate — each use issues a new refresh token and revokes the old one.
- Q: What algorithm should be used for JWT signing? → A: HS256 — symmetric HMAC-SHA256 with a single secret key in configuration.
- Q: Should the login endpoint be rate-limited to prevent brute-force attacks? → A: Yes — per-email rate limit in application code (10 failed attempts per 15-minute window per email).
- Q: What password hashing algorithm should be used? → A: PBKDF2/HMACSHA512 via ASP.NET Core Identity — built-in, no extra dependency.
- Q: Should presenting a previously consumed (revoked) refresh token trigger a security escalation? → A: Yes — full family invalidation: revoke all active refresh tokens for that user and return 401.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Register a New Account (Priority: P1)

A new user wants to start using Librify. They provide their name, email address, and a password to create an account. On success, they receive confirmation that their account is active and can immediately begin using the system.

**Why this priority**: Registration is the entry point for all other features. Without it, no one can use the application.

**Independent Test**: Can be fully tested by submitting a registration request with valid credentials and verifying the account is created; delivers a working sign-up flow independently of login.

**Acceptance Scenarios**:

1. **Given** a visitor with a valid email address and a strong password, **When** they submit a registration request, **Then** a new account is created and a success response is returned.
2. **Given** a visitor using an email address that already belongs to an existing account, **When** they attempt to register, **Then** the system rejects the request with a clear conflict message.
3. **Given** a visitor submitting invalid data (missing email, weak password), **When** they submit registration, **Then** the system returns validation errors describing each problem.

---

### User Story 2 - Log In to an Existing Account (Priority: P1)

A registered user wants to access the system. They provide their email and password and receive an access token and a refresh token, which they use to authenticate subsequent requests.

**Why this priority**: Login is the gateway to all protected functionality. It must work reliably before any other feature can be used end-to-end.

**Independent Test**: Can be fully tested by registering an account then logging in; a valid token pair is returned and can be used to call a protected endpoint.

**Acceptance Scenarios**:

1. **Given** a registered user with correct credentials, **When** they log in, **Then** they receive a short-lived access token and a longer-lived refresh token.
2. **Given** a registered user with an incorrect password, **When** they attempt to log in, **Then** the system rejects the request without revealing whether the email or the password was wrong.
3. **Given** an email address that does not correspond to any account, **When** login is attempted, **Then** the system returns the same generic invalid-credentials error.

---

### User Story 3 - Refresh an Expired Access Token (Priority: P2)

An authenticated user's access token has expired. They exchange their refresh token for a new access token without needing to re-enter their password, allowing their session to continue seamlessly.

**Why this priority**: Required for a usable long-lived session; without it users must log in repeatedly. Lower priority than core login because the system is functional even with short-lived tokens alone.

**Independent Test**: Can be fully tested by logging in, waiting for the access token to expire (or using a deliberately short expiry in a test environment), then calling the refresh endpoint and verifying a new valid access token is returned.

**Acceptance Scenarios**:

1. **Given** a user holding a valid refresh token, **When** they request a token refresh, **Then** a new access token and a new rotated refresh token are returned, and the old refresh token is revoked.
2. **Given** a user presenting an expired or invalid refresh token, **When** they request a refresh, **Then** the request is rejected and the user must log in again.

---

### User Story 4 - Log Out (Priority: P2)

A user wants to end their session. They submit a logout request, which invalidates their refresh token so it can no longer be used to obtain new access tokens.

**Why this priority**: Essential for security (shared devices, account handoff), but the application remains functional without explicit logout since access tokens expire naturally.

**Independent Test**: Can be fully tested by logging in, logging out, then attempting to use the refresh token and verifying it is rejected.

**Acceptance Scenarios**:

1. **Given** a user with a valid refresh token, **When** they log out, **Then** the refresh token is invalidated and the system returns a success response.
2. **Given** a user who has already logged out and attempts to reuse the same refresh token, **When** they call the refresh endpoint, **Then** the request is rejected.

---

### Edge Cases

- What happens when registration is attempted with an email address that differs only in letter case (e.g., `User@Example.com` vs `user@example.com`)?
- Presenting a consumed refresh token triggers full family invalidation (all sessions revoked, 401 returned). See FR-013.
- What happens when a login request is missing required fields entirely (null/empty body)?
- How does the system handle concurrent refresh requests with the same refresh token?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a new user to create an account by providing a display name, a unique email address, and a password.
- **FR-002**: The system MUST reject registration if the email address is already associated with an existing account.
- **FR-003**: The system MUST validate that passwords meet a minimum strength requirement (at least 8 characters, containing at least one uppercase letter, one lowercase letter, and one digit).
- **FR-004**: The system MUST store passwords hashed using PBKDF2/HMACSHA512 (ASP.NET Core Identity default hasher). Plain-text passwords MUST never be persisted or logged.
- **FR-005**: The system MUST allow a registered user to log in with their email and password and, on success, return both a short-lived access token and a longer-lived refresh token.
- **FR-006**: The system MUST treat login failures (wrong password or unknown email) identically in the response, revealing no information about which field was incorrect.
- **FR-007**: The system MUST allow a caller holding a valid refresh token to obtain a new access token without re-authenticating. On each successful refresh, the old refresh token MUST be revoked and a new refresh token MUST be issued (always-rotate strategy).
- **FR-008**: The system MUST invalidate a refresh token when it is used for logout, preventing future use.
- **FR-009**: Access tokens MUST be JWTs signed with HS256 (HMAC-SHA256). They MUST be self-contained and verifiable without a database lookup (stateless verification). The signing secret MUST be sourced from application configuration, never hardcoded.
- **FR-010**: All endpoints that operate on user-specific data MUST require a valid access token; requests without one MUST be rejected with an authentication error.
- **FR-011**: Email addresses MUST be treated case-insensitively during lookup (registration and login).
- **FR-012**: The login endpoint MUST enforce a per-email rate limit of 10 failed attempts within any 15-minute window. Once exceeded, further login attempts for that email MUST be rejected with a 429 response until the window resets. Successful logins reset the counter.
- **FR-013**: If a refresh token that has already been consumed (revoked by rotation) is presented, the system MUST treat it as a token theft signal, immediately revoke ALL active refresh tokens for the associated user (full family invalidation), and return a 401 response.

### Key Entities

- **User**: Represents a registered account. Attributes: unique identifier, display name, email address (normalised), hashed password, account creation timestamp.
- **Refresh Token**: Represents an active session credential. Attributes: unique token value, owning user, expiry timestamp, revocation flag, creation timestamp.
- **Access Token**: A short-lived, self-contained credential issued at login or token refresh. Not persisted; carries the user's identity and expiry claim.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new user can complete registration in under 30 seconds from a standing start (no prior knowledge of the API required beyond reading the documentation).
- **SC-002**: A registered user can log in and receive usable credentials in a single request-response cycle.
- **SC-003**: 100% of login failure responses return the same response shape regardless of whether the email or password was incorrect, preventing enumeration attacks.
- **SC-004**: Access token verification adds no observable latency on protected endpoints (stateless check only — no additional network call required).
- **SC-005**: Logging out reliably prevents the invalidated refresh token from being accepted on any subsequent call.

## Assumptions

- The application currently has no authentication mechanism; this feature introduces the first identity layer.
- Only email/password credential-based authentication is in scope; OAuth2 social login (Google, GitHub, etc.) is deferred to a future feature.
- A single user role exists for now (no admin or premium tiers); role-based access control is out of scope for this feature.
- Access tokens expire after 15 minutes; refresh tokens expire after 7 days. These values should be configurable via application settings.
- Email verification (confirming a user owns the address they registered with) is out of scope for this initial version.
- Password reset / forgot-password flows are out of scope for this feature and will be addressed separately.
- The API is consumed by machine clients (other services or a future frontend), not directly by browsers, so cookie-based session management is not required.
