# DATA MODEL

Implements `DOMAIN_MODEL.md`. Conventions: SQL Server; `GETUTCDATE()`/`NEWID()` defaults; soft-revoke (set `RevokedAt`), never hard-delete sessions/tokens. Account privacy and post privacy are independent axes (REQUIREMENTS R-PROF-5/R-POST-1): account privacy decides Accepted vs Pending formation; post privacy decides visibility given the accepted set.

## Current tables (verified in `Configurations/*`, `ApplicationDbContext`)
- `AspNetUsers(Guid)` + Identity tables; owned `Address_*`; roles `User`/`Admin` seeded (fixed Guids).
- `Post`: PK `Id int identity`; `Content nvarchar(5000) NOT NULL` (image-only stores `""` — CU, keep); `PublishDate` default now; `ImageUrl nullable`; `Privacy int NOT NULL`; FK `UserId→Users` CASCADE; index `PublishDate`.
- `Follow`: PK `(FollowerId, FollowingId)`; `CreatedAt` default now; `CK_Follow_NoSelfFollow`; both FKs `NoAction`; no status column (today).
- `Session`: PK `Id uniqueidentifier` default `NEWID()`; `Client int NOT NULL`; OS/Browser/Device ≤100 nullable; `IpAddress ≤45` nullable; `CreatedAt/LastActivityAt` default now; `RevokedAt?`; FK `UserId→Users` CASCADE; indexes `(UserId)`, `(UserId,RevokedAt)`.
- `RefreshToken`: PK `Id`; `TokenHash nvarchar(450) NOT NULL` unique index; `ExpiresAt NOT NULL` + index; `RevokedAt?`; FK `SessionId→Session` CASCADE; index `(SessionId,RevokedAt)`.
- `Comment/React/Notification`: tables exist; untouched by v1.

## Approved migration (single, v1) — `AddFollowRequestsAndAccountPrivacy` [EX-D2/D6/EX-2026-10-07, pending implementation]
1. `AspNetUsers`: ADD `IsPrivate bit NOT NULL DEFAULT 0`.
2. `Follow`: ADD `Status int NOT NULL DEFAULT 1 /* Accepted=1, Pending=0 */`; backfill all existing rows `Accepted` (they were created under immediate-follow semantics).
3. Index: `CREATE INDEX IX_Follow_Following_Status ON Follow(FollowingId, Status)` (inbound pending listing + counts); keep PK as-is (one row per pair preserved).
4. Email uniqueness for change-email confirm race: rely on the existing unique email index (confirm re-checks uniqueness; taken → 409) [EX-2026-10-07]. Change-token storage (1h single-use, newer-invalidates-previous) is an app-layer record — no new uniqueness column beyond the user email index.
5. No password-rule column (8–64 enforced at API/Identity layer, not storage).
6. No data loss; downgrade drops index → column → column (documented in migration).

## Nullability / delete behavior (post-migration)
- `Follow.Status` NOT NULL; `CreatedAt` NOT NULL (keep DB default; app may omit).
- Follow FKs stay `NoAction` (deleting a user with follows fails fast — matches CU; do NOT silently cascade follows; user deletion itself is out of scope v1).
- Post→User CASCADE (keep); Session→User CASCADE (keep); RefreshToken→Session CASCADE (keep).
- Avatar/Post `ImageUrl` nullable; file rows have no FK (filesystem, orphan-cleanup by app patterns).

## Concurrency
- Refresh rotation: `ExecuteUpdate … WHERE Id AND RevokedAt IS NULL`, check row-count (existing pattern, keep for new session-revoke paths).
- Accept race (double-accept / accept-vs-toggle): conditional update `WHERE FollowerId+FollowingId AND Status=Pending`; row-count 0 → 409.
- Follow insert race: rely on PK violation → 409 (existing `DbUpdateException` pattern, extend message by status).
- Toggle private→public auto-accept: single `ExecuteUpdate Pending→Accepted WHERE FollowingId=me` (atomic; concurrent new requests land `Accepted` if they read post-toggle flag, `Pending` if pre-toggle — both converge correctly on retry since toggle is idempotent).

## What is NOT changing
- No new tables (rejected `FollowRequest` — see ADR-001). No `Post.Content` nullability change. No session hard-delete. No v2 tables/columns for comments/reacts/notifications.
