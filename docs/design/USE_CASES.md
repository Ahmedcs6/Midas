# USE CASES

Derived from `docs/requirements/REQUIREMENTS.md`. Statuses: `[EX] explicit · [CU] current · [EX-2026-10-07] approved 2026-10-07 (target where gap noted)`. Account privacy and post privacy are independent axes: account privacy decides Accepted vs Pending formation; post privacy decides visibility given the accepted set; Private posts are never visible to followers.

## UC-A1 Register → confirm → login [EX/CU]
- Actor: Anonymous. Pre: email/username unused.
- Input: `RegisterRequest` (see API design).
- Main: create user → assign `User` role (transactional) → queue confirm link → 201 "please confirm". User confirms (`userId+token`) → login with Client → session + token pair.
- Alt: duplicate → 409; resend-confirm unknown/done → success-noop.
- Fail: unconfirmed login/forgot → 403; bad credentials → 401 generic; lockout → 423.
- Post: confirmed user with ≥1 active session.

## UC-A2 Refresh rotation [CU]
- Actor: Authenticated. Pre: active refresh token.
- Main: present token → verify hash + session not revoked + not expired → atomically revoke old (`RevokedAt==null` guard) → issue new pair in same session.
- Alt/fail: malformed → 400; expired/unknown → 400/validation; reuse of revoked → revoke whole session + security-alert email + 401.
- Post: exactly one active token per session chain.

## UC-A3 Forgot → reset (revoke-all) [CU]
- Actor: Anonymous. Pre: confirmed email.
- Main: forgot queues reset link (noop-success if unknown) → reset with `id+token+newPassword` → new password + revoke ALL sessions/tokens transactionally.
- Post: all prior sessions revoked; user logs in again.

## UC-P1 View profile [CU]
- Actor: Anonymous/Authenticated. Input: `userName`. Main: return public fields + follower/following counts. Fail: unknown → 404.

## UC-P2 Edit profile [EX/CU + EX-2026-10-07]
- Actor: Owner. Pre: authenticated. Input: partial `EditUserRequest` (no UserName/Gender/Email — never applied via this route [EX-D7]; [CU]: DTO omits those fields).
- Main: omitted = keep; explicit null clears whole Address / nullable field, 400 on required First/Last [EX-2026-10-07]; validate lengths/dates/address → `UserManager.UpdateAsync`. No-op → 200.
- Post: profile updated; no identity fields changed.

## UC-P3 Edit avatar [CU + EX-2026-10-07]
- Actor: Owner. Input: multipart image (required, ≤5 MB, content/signature/decoding check [EX-2026-10-07]; [CU] gap: ext/MIME only).
- Main: save new → update user → delete old. Fail: missing/invalid → 400.
- Post: `ImageUrl` points to new file; old file removed.

## UC-P4 Toggle account privacy [EX-D6/EX-2026-10-07, pending impl]
- Actor: Owner. Input: `{isPrivate}`.
- Main: set flag; if false (going public) bulk-accept pending (`ExecuteUpdate`); accepted rows untouched both directions.
- Post: future follows follow new mode; visibility rules switch immediately.

## UC-F1 Follow public account [CU → EX-2026-10-07 target]
- Actor: Follower. Pre: target public, not self, no active row.
- Main: insert `Accepted` row → 200 `{status:accepted}`.
- Fail: self → 400; unknown → 404; duplicate active → 409.

## UC-F2 Request private account [EX-D2/EX-2026-10-07, pending impl]
- Actor: Follower. Pre: target private, not self, no row.
- Main: insert `Pending` row → 200 `{status:pending}` (no visibility granted).
- Fail: same as F1; duplicate pending → 409 "already pending".

## UC-F3 Cancel / unfollow [CU + EX-2026-10-07]
- Actor: Follower. Input: `{userName}`. Main: delete caller's row regardless of status → 204. Fail: no row/unknown → 404. Ownership [EX-2026-10-07]: target accepts/declines, requester cancels, accepted unfollows, self forbidden.

## UC-F4 Moderate inbound requests [EX-D2/EX-2026-10-07, pending impl]
- Actor: Followed (owner). Pre: private account with pending rows.
- Main: `GET /me/follow-requests` → accept (`Pending→Accepted`, 200) or decline (delete, 204). Guards: only target owner; accept is idempotent-safe (re-accept → 409).
- Post: accepted follower gains `Friends` visibility.

## UC-T1 Create post [CU + EX-D1/EX-2026-10-07]
- Actor: Owner. Input: multipart `{content?, privacy!, image?}` with ≥1 of non-blank text/image (empty → 400); image ≤5 MB + content check.
- Main: validate → save file (if any) → insert row → 201.
- Fail: empty → 400; bad image → 400.

## UC-T2 Edit post [CU + EX-D5/EX-2026-10-07]
- Actor: Owner. Pre: post exists and owned.
- Input: `{content?, image?, removeImage, privacy?}` — independently editable; privacy-only valid.
- Main: apply partials → validate result non-empty (else 400) → swap files safely → 200 (incl. no-op).
- Fail: missing → 404; foreign → 403 (edit) — note delete uses 404 for same case (preserved inconsistency documented in API design).

## UC-T3 Delete post [CU]
- Actor: Owner. Main: owner-scoped delete → remove row → delete file → 204. Fail: missing/foreign → 404.

## UC-T4 List user posts [CU + EX-2026-10-07]
- Actor: Anyone. Input: `{userName, limit 1–50, cursor?}`.
- Main: resolve user → compute permission set (owner: all; accepted-follower: Public+Friends, never Private; else Public; anon: Public) → counts accepted-only → cursor query `Id<cursor`, `Id desc`, `Take(limit+1)` → `{items, nextCursor}`.
- Fail: bad limit/cursor → 400; unknown user → 404.

## UC-S1/S2/S3 Session management [EX-D3, implemented 2026-10-04 via `SessionService`]
- Actor: Owner. S1: list active sessions with `isCurrent` (from `session_id` claim via `ICurrentUser.SessionId`). S2: revoke one (own only, incl. self; strong revoke = session row + active tokens). S3: revoke all except current. Scope [EX-2026-10-07]: revocation blocks refresh; JWTs expire normally. Fail: foreign id → 404. Reset-password reuses `SessionService.RevokeAllAsync` inside its transaction.

## UC-E1/E2 Change-email [EX-D7/EX-2026-10-07, pending impl]
- Actor: Owner + System. E1: `POST /me/email/change {newEmail}` → queue link to NEW address (always 200 unless invalid format). E2: confirm `userId+token` (1h, single-use; newer request invalidates previous) → uniqueness re-check → swap `Email`, `EmailConfirmed=true`, refresh `SecurityStamp` → 200. Fail: invalid/expired/used → 400; unknown → 404; taken-at-confirm → 409.
- Post: old email works until swap; sessions unaffected except the swap itself.
