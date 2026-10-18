# Midas Requirements

## 1. Project Overview

Midas is a social-media backend built with ASP.NET Core Web API (.NET 10), Entity Framework Core, SQL Server, ASP.NET Core Identity, and JWT.

The project focuses on secure authentication, account management, social interactions, and clear separation of responsibilities without unnecessary architectural complexity.

## 2. Authentication and Account Management

### 2.1 Registration

- Users MUST be able to register using an email address and password or Google authentication.
- Every new Midas account MUST have a unique username, regardless of the registration method.
- Users registering through Google MUST select a username during the registration-completion flow.
- The account MUST NOT be fully registered until the selected username passes the application's validation and uniqueness rules.
- Email/password registration MUST require email confirmation before normal sign-in is allowed.
- Google registration MUST validate the Google ID Token on the server and MUST NOT trust identity claims supplied independently by the client.
- Google registration MUST use Google's stable subject identifier (`sub`) to identify the external account.
- The server MUST validate the token's signature, issuer, audience, expiration, and other required claims.
- Google registration MAY skip Midas email confirmation only when the validated Google identity confirms that the email is verified (`email_verified = true`).
- An unverified Google email MUST NOT be treated as a verified email address.
- A Google account MUST NOT be automatically linked to an existing Midas account solely because their email addresses match.
- Usernames and email addresses MUST satisfy the application's validation and uniqueness rules.

### 2.2 Login

Users MUST be able to authenticate using any of the following methods:

- Email address and password.
- Username and password.
- Google authentication.

Password-based authentication MUST validate credentials using ASP.NET Core Identity.

Google authentication MUST use a supported Google identity flow and validate the returned identity token on the server.

All authentication methods MUST follow the same account-status checks and MFA decision process before issuing a fully authenticated session.

When Google authentication is used for the first time, the server MUST distinguish between registration of a new Midas account and authentication of an existing account.

### 2.3 Email Confirmation and Password Recovery

- Users MUST be able to confirm their email addresses.
- Email/password accounts MUST complete email confirmation before normal sign-in.
- Google accounts with a validated, verified Google email MAY be considered email-confirmed without a separate Midas confirmation step.
- Changing an account's email address MUST require re-authentication and verification of the new email address.
- Completing an email address change MUST revoke all existing sessions, including the current session, requiring the user to authenticate again.
- Users MUST be able to request password recovery and reset their passwords securely.
- Password recovery and email-related endpoints MUST avoid unnecessarily revealing whether an account exists.
- Password reset MUST revoke the user's existing sessions and refresh tokens according to the session-management requirements.
- Password reset MUST NOT silently disable MFA.
- Password recovery MUST NOT be treated as sufficient proof to bypass MFA.

### 2.4 Multi-Factor Authentication (MFA)

#### General Requirements

- MFA MUST be optional and configurable per Midas account.
- TOTP through compatible authenticator applications MUST be supported.
- Users MUST verify a valid TOTP code before MFA enrollment is considered complete.
- When MFA is enabled, successful password authentication or Google authentication MUST NOT immediately grant a fully authenticated session.
- Users MUST complete TOTP verification before receiving normal access and refresh tokens.
- When MFA is disabled, successful primary authentication MAY proceed directly to session issuance, subject to other account restrictions.
- MFA challenges MUST be temporary, single-purpose, and restricted to completing authentication.
- MFA challenges MUST NOT grant access to protected API resources or function as normal access or refresh tokens.
- MFA challenge attempts MUST be rate-limited and expire after a short, defined period.

#### TOTP Security

- TOTP secrets MUST be encrypted at rest using an appropriate key-management approach.
- TOTP secrets MUST NOT be exposed through API responses or application logs.
- TOTP verification MUST enforce a limited time window and attempt limits.
- The system MUST prevent the same accepted TOTP code from being successfully reused within its validity window.
- MFA enrollment, verification, and recovery flows MUST be protected against brute-force attacks and unauthorized changes.
- The implementation SHOULD use established ASP.NET Core Identity authenticator functionality where appropriate instead of implementing the TOTP algorithm from scratch.

#### Enabling, Disabling, and Resetting MFA

- Enabling MFA MUST require appropriate re-authentication and successful verification of the authenticator.
- Disabling or resetting MFA MUST require appropriate verification of the user's identity and existing MFA where available.
- Security-sensitive MFA changes MUST be recorded in audit logs and MUST trigger a security notification.
- MFA recovery MUST NOT rely solely on knowledge of the account's email address.

#### MFA Recovery Codes

- Midas MUST generate one-time recovery codes when a user enables MFA.
- Recovery codes MUST be displayed securely when generated and MUST NOT be retrievable in plaintext afterward.
- Recovery codes MUST be stored as secure hashes.
- Each recovery code MUST be invalidated after successful use.
- Regenerating recovery codes MUST invalidate the previous set.
- Recovery attempts MUST be rate-limited and recorded in security audit logs.
- Successful MFA recovery MUST notify the user and provide a secure path to register a replacement authenticator.
- Recovery codes MUST NOT grant general access without completing the remaining required authentication steps.
- If a user loses access to their authenticator and all unused recovery codes, Midas MUST NOT provide an alternative MFA recovery mechanism.
- Email ownership or password recovery MUST NOT be sufficient to bypass MFA or restore access to an account requiring MFA.

### 2.5 Google Account Linking

- Users MUST be able to link a Google account to an existing Midas account.
- Linking MUST require an authenticated Midas session and appropriate re-authentication to verify ownership of the existing Midas account.
- The user MUST authenticate with Google to prove ownership of the Google account being linked.
- The server MUST validate the Google ID Token and identify the external account using Google's stable `sub` identifier.
- A Google account MUST NOT be linked to more than one Midas account.
- The server MUST NOT automatically link accounts solely because their email addresses match.
- Linking MUST fail if the Google account is already linked to another Midas account.
- MFA requirements MUST be enforced when re-authenticating an account with MFA enabled.
- Account linking and unlinking MUST be recorded in security audit logs.
- Users MUST be notified of successful linking or unlinking.
- Users MUST NOT be allowed to remove their last usable authentication method.
- Unlinking Google MUST require appropriate re-authentication and MUST leave the user with another usable authentication method.

### 2.6 Username Management

- Users MUST be able to change their usernames to valid, available usernames.
- Username uniqueness MUST be enforced server-side and at the database level.
- Username changes SHOULD be rate-limited. The initial policy is one change every 30 days.
- Previous usernames SHOULD be reserved for 30 days before becoming available to other users.
- Internal relationships, authorization checks, and resource ownership MUST use immutable user identifiers rather than usernames.
- Username changes MUST require an authenticated session and appropriate additional verification when the operation is considered sensitive.
- Username changes MUST generate a security notification.

The 30-day limits are initial product policies and MAY be adjusted later.

### 2.7 Access Tokens and Sessions

#### Access Tokens

- The API MUST issue access tokens only after all required authentication steps, including MFA when applicable, have succeeded.
- Access tokens MUST have a lifetime of **10 minutes**.
- Each access token MUST contain a session identifier (`sid`) identifying the session associated with the token.
- Protected endpoints MUST validate the access token and verify that its associated session has not been revoked.
- Protected requests MUST be rejected if the associated session has been revoked, even if the access token has not expired.
- If the system cannot verify whether a session has been revoked, protected requests MUST fail closed.
- Authorization MUST be enforced independently of the authentication method.
- Authorization MUST NOT rely solely on client-supplied user identifiers.


#### Refresh Tokens

- Refresh tokens MUST be managed separately from access tokens.
- Refresh tokens MUST have a sliding expiration of 15 days from issuance.
- Successful refresh-token rotation MUST issue a replacement refresh token with a new 15-day expiration period.
- Refresh tokens MUST support expiration, revocation, and rotation.
- Refresh tokens MUST be stored securely. Raw refresh tokens MUST NOT be logged.
- Refresh-token rotation MUST invalidate the previously used token.
- The system MUST retain sufficient token-family or session relationships to detect reuse of invalidated refresh tokens.
- Reuse of an invalidated refresh token MUST revoke the affected session's active refresh-token family.
- Other independent sessions SHOULD remain active unless additional evidence indicates broader account compromise.
- Refresh-token reuse detection and related revocation events MUST be recorded in security audit logs.
- Revoked refresh tokens MUST NOT be used to issue new access tokens.

#### Session Management

- Users MUST be able to view their active sessions.
- Session information SHOULD include available device or client details, creation time, last activity, and expiration.
- The system MUST identify the current session where possible.
- Users MUST be able to revoke an individual session.
- Users MUST be able to revoke all their sessions.
- Logout MUST revoke the applicable session's refresh-token capability and add its `sid` to the Redis revocation denylist.
- Password changes and password resets MUST revoke all existing sessions, including the current session, requiring the user to authenticate again.
- Session revocation MUST prevent further refresh-token use and reject access tokens associated with the revoked session.
- The system MUST retain each revoked `sid` in Redis until at least **11 minutes after the expiration time of the latest access token issued for that session**. Redis TTL MUST remove the entry automatically afterward.
- All access tokens associated with the same session MUST use the same `sid`, so revoking that session invalidates every access token associated with it.
- Redis MUST be used as a revocation denylist, not as the sole authoritative store for refresh-token revocation.
- Session revocation, token reuse detection, and security-related session changes MUST be recorded in security audit logs.

## 3. Social Features

The application MUST support the following core capabilities:

- User profiles and profile privacy settings.
- Public and private accounts.
- Follow relationships and follow requests.
- Posts and media attachments.
- Comments and reactions.
- Notifications.

Detailed business rules MUST be documented before implementing each feature.

These rules MUST cover relevant authorization checks, ownership, privacy restrictions, validation, and state transitions.

## 4. Security Requirements

- Authentication and authorization MUST be enforced server-side.
- Sensitive credentials, authentication secrets, recovery codes, and tokens MUST NOT be logged or committed to source control.
- User-owned resources MUST be protected against unauthorized access and modification.
- Client-supplied user identifiers MUST NOT be trusted when the current user's identity can be derived from the authenticated context.
- Authentication and session-management operations MUST handle invalid, expired, revoked, and replayed credentials safely.
- Security-sensitive operations MUST be rate-limited where appropriate.
- Security audit logs MUST record relevant security events without storing credentials, tokens, TOTP secrets, or recovery codes.
- Error responses MUST NOT unnecessarily disclose sensitive account information.
- Security-sensitive flows MUST have automated tests covering successful and unsuccessful cases.

## 5. Quality Requirements

- The codebase MUST maintain clear separation of responsibilities.
- Business rules MUST be enforced independently of API controllers.
- Important business rules and security boundaries MUST be covered by automated tests.
- API behavior and local development setup MUST be documented.
- Database constraints MUST enforce critical uniqueness and integrity requirements where applicable.
- The implementation MUST avoid unnecessary architectural complexity while remaining maintainable.
- Existing ASP.NET Core Identity and framework security capabilities SHOULD be reused where appropriate instead of being reimplemented without a clear reason.

## 6. Scope and Future Decisions

The initial implementation prioritizes:

1. Registration and authentication.
2. Email confirmation and password recovery.
3. Google authentication and account linking.
4. Optional TOTP MFA and recovery codes.
5. Access-token and refresh-token security.
6. Session management and revocation.
7. Username management.

Social features will be implemented according to their documented business rules.

Passkeys and additional external identity providers are outside the initial authentication scope unless explicitly added later.

Implementation-specific choices, such as exact token lifetimes, rate limits, recovery-code count, username validation rules, and audit-log retention, MUST be documented in the relevant configuration or technical documentation before implementation.

Security-sensitive defaults MUST be explicit and MUST NOT be left to arbitrary implementation decisions.
