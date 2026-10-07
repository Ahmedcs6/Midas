# DOMAIN MODEL

Follows use cases in `USE_CASES.md`. Only concepts with a concrete reason are introduced — no speculative abstractions.

## Entities
- **ApplicationUser** (aggregate root for profile/identity): FirstName, LastName (required ≤50; explicit null → 400), Gender {Male,Female} immutable [EX-D7/D8], UserName immutable [EX-D7], Email mutable via confirmed flow [EX-D7/R-EML-3], EmailConfirmed, BirthDate?, About ≤500, Address (owned value object), ImageUrl?, **IsPrivate bool default false [EX-D6/EX-2026-10-07]** ([CU] gap: no flag in code yet).
  - Owns: Posts, Sessions. Participates: Follows (both sides), Comments, Reacts (v2, no behavior), Notifications (v2, no behavior).
- **Post** (aggregate root): Id int, Content (empty-string allowed when image-only; API enforces non-empty-result rule [EX-D1/EX-2026-10-07]), PublishDate (DB default), ImageUrl? (≤5 MB, content/signature/decoding check [EX-2026-10-07]), Privacy {Public,Friends,Private}, UserId (owner).
  - Invariants: I-T1 owner-only mutation; I-T2 result must have text or image, privacy-only edits valid, no-op → 200 [EX-D1/EX-2026-10-07]; I-T3 privacy edit allowed, independent of content/image [EX-D5/EX-2026-10-07]. Visibility: Public → everyone; Friends → owner + accepted followers; Private → owner only, never followers [EX-2026-10-07].
- **Follow** (relationship entity with state [EX-D2/EX-2026-10-07]): (FollowerId, FollowingId), CreatedAt, **Status {Pending, Accepted} [EX-2026-10-07]** ([CU] gap: no column in code yet).
  - Invariants: I-F1 no self-follow (app guard + `CK_Follow_NoSelfFollow`); I-F2 one row per pair (composite PK); I-F3 only Accepted grants Friends visibility + counts [EX-2026-10-07]; I-F4 state transitions: ∅→Pending (private) | ∅→Accepted (public) | Pending→Accepted (owner accept / auto-accept on going public) | Pending→∅ (decline/cancel) | Accepted→∅ (unfollow). Ownership: target accepts/declines, requester cancels, accepted unfollows [EX-2026-10-07].
- **Session** (auth aggregate with RefreshTokens): Id, UserId, Client {Web,Mobile,Desktop}, OS/Browser/Device/IP (observational), CreatedAt/LastActivityAt, RevokedAt? (soft-revoke, never hard-delete).
  - Invariants: I-S1 refresh valid only if session not revoked; I-S2 reset-password revokes all user sessions; I-S3 logout-all spares current; I-S4 revocation blocks refresh, JWTs expire normally [EX-2026-10-07].
- **RefreshToken**: Id, TokenHash (SHA256 Base64, unique), ExpiresAt (30d), RevokedAt?, SessionId. Derived: `IsExpired`, `IsActive`. Invariant I-R1: single-use rotation; reuse ⇒ session-wide revoke + alert.
- **Comment / React / Notification**: exist as tables, NO v1 behavior. React is like-only (no type). Listed here only to prevent accidental coupling — v1 code must not reference them in new paths.

## Value objects
- **Address** (owned by user): Country/State/City ≤100, Street ≤200, all nullable. No identity; replaced wholesale on edit; explicit null clears the whole object (no per-subfield null clearing) [EX-2026-10-07] ([CU]: `request.Address ?? user.Address` — clearing not possible yet; preserved shape).
- **ClientInfo** (non-persisted): OS/Browser/Device families + IP, captured at login.

## Ownership & responsibilities
- User owns profile fields + avatar file reference + privacy flag.
- Post owned by exactly one user; file (`Posts/…`) lifecycle follows row (create→save-then-insert; edit→save-then-swap-then-delete-old; delete→remove-then-delete-file).
- Follow row co-owned by pair for reads; writes split: follower creates/cancels, followed accepts/declines.
- Session owned by user; JWT `session_id` claim is the runtime identity of the session (surfaced via `ICurrentUser.SessionId`).
- Service ownership: `SessionService` owns session-lifecycle transitions (I-S2/I-S3 + list/revoke); `JwtService` owns token protocol (I-R1 + rotation); `AccountService` owns login-time session creation and calls `SessionService.RevokeAllAsync` on reset.

## Intentional non-design (rejected)
- No `Friendship` mutual entity (one-way accepted follower = friend per D2; mutual would contradict requirement).
- No separate `FollowRequest` table (single `Follow` row with Status avoids dual-write divergence; see ADR-001).
- No `Post.Privacy` history table (tightening/loosening takes effect immediately, no audit required in v1).
- No further service splits: `ISessionService` is the single justified new seam (ADR-003); login creation stays in `AccountService`, reuse-revoke stays in `JwtService`.
