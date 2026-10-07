# ARCHITECTURE

Follows `AGENTS.md` layering: `Program.cs` DI + thin `Controllers/*` → `Services/*` (`Interfaces/*`) + `Validators/*`, errors via `GlobalExceptionHandlingMiddleware`. Serilog, `MapControllers()`, `MapStaticAssets()` preserved. Simplest design that satisfies the use cases — no new layers, no speculative patterns.

## Placement of new behavior
- **Controllers** (thin, mapping only): `UsersController` gains `privacy`, `email/change`, `email/confirm`, `follow-requests/*`, `sessions/*` (`GET me/sessions`, `DELETE me/sessions/{id:guid}`, `DELETE me/sessions` → `SessionService`). `PostsController` gains `privacy?` on edit (no new routes). `AuthController` untouched (refresh stays on `JwtService`).
- **Application services** (all business rules live here, enforced via `CurrentUser`):
  - `SessionService` (new, `ISessionService`): session lifecycle — `ListAsync` (active + `isCurrent` from `session_id` claim), `RevokeOneAsync` (own only, strong revoke: session row + active tokens), `RevokeAllExceptCurrentAsync`, `RevokeAllAsync(userId)` (used by reset-password inside its transaction; shared scoped `DbContext`, no choreography). Needs only context + `ICurrentUser` + logging.
  - `JwtService`: token protocol only — `CreateJwtTokenAsync`, `GenerateRefreshToken`, `RefreshAsync` (rotation incl. reuse-detect, which stays inline as protocol logic). Keeps channel/`UserManager`/settings.
  - Login session creation stays in `AccountService` (login use-case ownership; avoids dragging `IClientInfoProvider` into `SessionService`).
  - `UserService` += privacy toggle, change-email orchestration (1h single-use token, 400/409 mapping, newer-invalidates-previous, queue link to NEW address, confirm swap + `SecurityStamp`) [EX-2026-10-07], follow state-machine (status-aware insert, accept/decline/list, toggle backfill with accepted-kept + auto-accept). Stays behind existing `IUserService` (+ method additions; no new interface unless it exceeds ~10 methods — then split `IFollowService`, deferred).
  - `PostService` += D1 result-nonempty check (create + edit; privacy-only valid; no-op → 200) [EX-D1/EX-2026-10-07], D5 privacy edit, Accepted-only visibility filter (join `Follows` with `Status=Accepted`; Private never to followers; anonymous path unchanged shape).
  - `AccountService` += password 8–64 validation (no complexity) [EX-2026-10-07] plus change-email confirm swap (uniqueness re-check + `SecurityStamp`). No new Identity tables.
- **Infrastructure (unchanged patterns)**: `IFileStorage` local (`Avatars/`, `Posts/`); image content/signature/decoding check in `ImageFileRules` + validators [EX-2026-10-07] ([CU] gap: ext/MIME only); `Channel<IEmailJob>` + `EmailWorker` (+ `ChangeEmailJob` reusing confirm template); `IClientInfoProvider` at login only.
- **Persistence**: one EF migration (§DATA_MODEL); `FollowConfiguration` += Status + index; `ApplicationUserConfiguration` += IsPrivate. Queries: pending excluded via `Status==Accepted` predicate in counts + post-visibility (single predicate, no new includes → no N+1).
- **Cross-cutting**: validators listed in API design; `ResultExtension.ToActionResult` reused (note tech debt below); Serilog enrich UserId+IP unchanged.

## Significant decisions (summary; details in `docs/decisions/*`)
- **Single-table `Follow.Status`** over separate `FollowRequest` (ADR-001): avoids dual-write divergence; PK preserved; one backfill.
- **Post-rule at API+service layers, no column change** (ADR-002): `Content NOT NULL` stays (`""` for image-only); emptiness is an API invariant, not a storage one.
- **Separate `SessionService` from day one** (ADR-003): distinct responsibilities/reasons to change (session lifecycle vs token protocol); one-way deps (`AccountService` → `SessionService`; reuse-path stays inline in `JwtService`).
- **Change-email keeps old working until confirm** (ADR-004, decided 2026-10-04 + finalized 2026-10-07: 1h single-use, 400/409, DB uniqueness, newer-invalidates-previous).

## Current vs intended vs implemented (existing-project §7)
- Follow immediate-insert → **approved, pending impl** (D2 + 2026-10-07; migration backfills Accepted so current data keeps meaning).
- Post empty-allowed → **approved, pending impl** (D1 + 2026-10-07; validator-only change).
- Post privacy immutable → **approved, pending impl** (D5 + 2026-10-07; additive optional field).
- Session list/logout → **implemented** (D3; revoke pattern reuses reset-tested path; scope: refresh blocked, JWTs expire normally).
- Email effectively immutable → **approved, pending impl** (D7 + 2026-10-07).
- Password non-empty-only → **approved rule pending impl** (8–64, no complexity).
- Preserved quirks (not redesigned for cleanliness): PATCH-403 vs DELETE-404 asymmetry; multipart-only post edits; whole-address replace; non-generic `ToActionResult` missing 423 (logged as debt: add `Locked` arm to the non-generic overload when touching errors).

## Execution order (slices, each independently testable)
1. Migration + `Follow.Status`/`IsPrivate` → 2. follow-requests + moderation (ownership: target accepts/declines, requester cancels) → 3. privacy toggle + backfill (accepted-kept, auto-accept) + accepted-only counts/visibility → 4. post D1/D5 (nonempty, privacy field, no-op 200) + password 8–64 + image content check → 5. change-email (token rules) → 6. validators/tests per slice (sessions already done; regression keeps green).
