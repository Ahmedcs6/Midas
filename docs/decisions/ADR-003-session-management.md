# ADR-003: `SessionService` separated from `JwtService`

Status: Approved + Implemented (2026-10-04). Affects: architecture (D3).
Supersedes the earlier proposal (extend `JwtService`).

## Context
D3: list active sessions, logout one, logout all-except-current.
Before: `JwtService` owned refresh rotation, reuse-detection, hashing; session/token revocation was scattered — rotation reuse-path in `JwtService`, login creation + reset revoke-all inline in `AccountService`. No session endpoints, no session DTOs, `ICurrentUser` exposed only `UserId`.

## Alternatives
- **A. Extend `JwtService`.** Add list/revoke methods reusing rotation helpers. (Rejected.)
- **B. Separate `SessionService` + `ISessionService` (chosen).** Session lifecycle (list, revoke one, revoke all-except-current, revoke all) lives in one service; token protocol (create/rotate/validate) stays in `JwtService`.

## Decision
B. Rationale is responsibility separation, not size: session management and JWT/token lifecycle have independent business rules and reasons to change (device-list UX and logout rules vs rotation security and reuse-attack response). Concretely:
- `JwtService`: `CreateJwtTokenAsync`, `GenerateRefreshToken`, `RefreshAsync` (rotation incl. reuse-detect). Keeps `Channel<IEmailJob>`, `UserManager`, `JwtSettings`. Reuse-path revoke stays inline — it is protocol logic, and this keeps the security path free of cross-service coupling.
- `SessionService`: `ListAsync`, `RevokeOneAsync`, `RevokeAllExceptCurrentAsync`, `RevokeAllAsync(userId)`. Needs only `ApplicationDbContext`, `ICurrentUser`, logging.
- Login session creation stays in `AccountService` (part of the login use case; moving it would drag `IClientInfoProvider` + `Client` into `SessionService` for no gain).
- Reset-password revoke-all moved to `SessionService.RevokeAllAsync`, called by `AccountService` inside its existing transaction (shared scoped `DbContext`, no choreography).
- `ICurrentUser += SessionId` (reads `session_id` claim; additive, existing consumers unaffected) to support logout-all-except-current.
- Revoke semantics unified as strong revoke: `Session.RevokedAt` + active tokens. Scope [EX-2026-10-07]: revocation blocks refresh immediately; already-issued JWTs expire normally (no `OnTokenValidated` revocation check — matches current code).

## Consequences
- `UsersController` session routes → `SessionService`; `AuthController` untouched; no migration (zero schema change).
- Dependency direction is one-way: `AccountService` → `SessionService`; `JwtService` → `SessionService` only if reuse-delegation is ever adopted (currently not; reuse stays inline per gate decision 2026-10-04).
- No further abstractions introduced (`ISessionService` is the single justified seam).
