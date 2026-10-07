# REQUIREMENTS

Scope: Midas social-media Web API (.NET 10, EF Core + Identity + JWT).
Sources observed: `Midas.Api/Controllers/*`, `Services/*`, `Validators/*`, `Models/*`, `Configurations/*`, `tests/`.
Decisions referenced: D1–D8 user decisions and gate resolutions 2026-10-04 (Q1–Q6).

Legend (strict):
- **[EX] Explicit** — explicitly required by a user statement or an explicitly approved product decision (Dx / gate Q-decision). Code implementing it does not *create* this status.
- **[CU] Current behavior** — externally observable behavior of the code today. It is NOT automatically required; it may be intentional or accidental.
- **[IN] Inferred** — a reasonable interpretation of code or behavior. Requires confirmation before it becomes `[EX]`.
- **[PR] Proposed** — a new proposal not yet approved. MUST NOT be implemented as if required.
- **[OQ] Open question** — unresolved; blocks or affects implementation. No answer is invented here.

Reading rule (WHAT vs HOW): requirement lines state observable behavior, business rules, constraints, and guarantees. Implementation mechanisms (query shapes, EF APIs, claim types, channels, static-asset helpers, DB defaults, class/test names) are NEVER requirements by themselves; they appear only in `[CU] Implemented as: …` notes or are moved to design docs (see §8).

## 1. Actors & goals

| Actor | Goals |
|---|---|
| Anonymous | Register, confirm email, login, reset password, view public profiles/posts |
| Authenticated user (Follower / Followed / Owner) | Edit own profile/avatar, CRUD own posts, follow/request/unfollow, moderate inbound requests, manage own sessions |
| System | Deliver confirm/reset/change-email links and security alerts without blocking requests |

## 2. Functional requirements

### 2.1 Auth

- **R-AUTH-1 Register contract [CU] + password rule [EX-2026-10-07].** First/Last 1–50, Gender {Male,Female}, UserName 3–32 matching `^[a-zA-Z0-9_.@-]+$`, valid Email ≤256. Duplicate username/email → 409 Conflict.
  - `[CU] Implemented as:` request validator + Identity `CreateAsync`; `User` role assigned; confirmation email queued.
  - `[EX-2026-10-07]` Password MUST be 8–64 characters. No additional complexity requirement.
  - `[CU] Gap:` validators currently enforce only non-empty; Identity defaults apply underneath — length rule not yet enforced.
- **R-AUTH-2 Confirmed-email gate [CU].** Login and forgot-password with an unconfirmed email → 403 with a confirm-email message.
- **R-AUTH-3 Anti-enumeration [CU].** Resend-confirm and forgot-password for unknown or already-completed addresses return success with no observable difference. Confirm/reset with malformed token → 400; unknown user → 404.
- **R-AUTH-4 Login session [CU].** Successful login creates one server-side session bound to `{Web,Mobile,Desktop}` client plus observed OS/browser/device/IP, one refresh token expiring in 30 days, and one short-lived JWT carrying the session identity.
  - `[CU] Implemented as:` SHA256-hashed refresh-token row; JWT `session_id` claim; OS/browser/device from client-info provider.
- **R-AUTH-5 Refresh single-use [CU] + scope [EX-2026-10-07].** A refresh token MUST be single-use. Once consumed, reuse MUST be rejected. Reuse of a consumed token revokes the whole session and queues a security alert. Password reset revokes ALL sessions and active tokens of the user.
  - `[CU] Implemented as:` atomic conditional update on unrevoked rows with affected-row check; reuse path revokes session tokens and queues alert; reset revokes inside its transaction.
  - `[EX-2026-10-07]` Revocation blocks refresh immediately; already-issued JWTs are NOT invalidated and expire normally (see R-SES-3).
- **R-AUTH-6 Lockout [CU].** Repeated bad passwords lock the account temporarily; locked login → 423.
  - `[CU] Implemented as:` Identity lockout (5 failures, 15 min), enforced on password check.
- **R-AUTH-7 Password policy [EX-2026-10-07].** Password MUST be 8–64 characters. No complexity requirement beyond length.
  - `[CU] Gap:` currently only non-empty is validated at the API layer; Identity defaults apply underneath.

### 2.2 Profile

- **R-PROF-1 Public profile [CU] + [EX-Q4] + counts [EX-2026-10-07].** `GET /{userName}` is public and returns follower/following counts (accepted-only per R-FOL-7). Unknown username → 404.
  - `[EX-Q4]` Unknown usernames return 404 (decided, unchanged).
  - `[CU] Gap:` counts currently include every row (no pending concept yet).
- **R-PROF-2 Partial edit [CU] + null semantics [EX-2026-10-07].** `PATCH /me` is partial: omitted fields keep their values. Explicit `null` means clear where clearing is logically allowed (nullable/optional fields such as About, BirthDate, Address); explicit `null` for a required field (First/Last) MUST be rejected with 400. Length/recency caps apply (First/Last ≤50, BirthDate past and <150y, About ≤500, Address subfields capped).
  - `[CU] Gap:` the service currently merges with null-coalescing, so explicit `null` is indistinguishable from omitted (clearing is not possible); whole-`Address` replacement with no per-subfield merge (null `Address` clears the whole object once implemented; subfields clear only via full replace). No-op profile edit returns 200 (same principle as R-POST-5).
- **R-PROF-3 Immutability [EX-D7].** UserName and Gender MUST NOT change after register. Email MUST NOT change via `PATCH /me`; only via the confirmed change-email flow (§2.6).
  - `[CU] Gap note:` the current DTO simply omits those fields, so “400 if sent” is not an enforced contract — the requirement is that they are never applied via this route. Wording corrected without changing the decision.
- **R-PROF-4 Avatar [CU] + image rule [EX-2026-10-07] + [EX-Q3].** Avatar is replace-only via multipart image ≤5 MB.
  - `[EX-2026-10-07]` Images MUST be validated by actual file content/signature/decoding in addition to metadata, max 5 MB (applies to avatars and post images).
  - `[CU] Gap:` current check is extension OR `image/*` MIME only; no content/signature/decoding validation.
  - `[EX-Q3]` Avatar DELETE is deferred (unchanged).
- **R-PROF-5 Account privacy [EX-D6] + [EX-2026-10-07] (not yet implemented).** Account privacy is SEPARATE from post privacy (see R-POST-1; no contradiction: account privacy governs how follows are formed, post privacy governs who sees each post). Account has a private flag defaulting to public. Public account → follow immediately accepted. Private account → follow starts pending. Toggling account privacy MUST NOT downgrade existing accepted relationships in either direction. Private→public automatically accepts pending requests.
  - `[CU] Gap:` no flag, endpoint, or backfill exists today.

### 2.3 Follows / follow-requests

- **R-FOL-1 Current follow [CU].** `POST /follow/{userName}` inserts immediately; duplicate → 409 “already following”; self-follow → 400. `DELETE` removes the caller’s row or 404.
  - `[CU] Gap vs [EX-D2]/[EX-2026-10-07]:` no `Status`/`IsPrivate` exists today, so counts include every row and any row grants `Friends` visibility; required behavior is R-FOL-2/3/7.
- **R-FOL-2 Instagram-like requests [EX-D2] + [EX-2026-10-07] (target, not yet implemented).** Public account target → immediate `Accepted` follow. Private account target → `Pending` request. Owner accepts/declines; follower cancels via `DELETE /follow/{userName}`.
  - `[CU] Gap:` no `Status`/`IsPrivate` exists today; R-FOL-1 is the current behavior.
- **R-FOL-3 Pending visibility [EX] (unchanged).** Pending requests MUST NOT grant `Friends` visibility.
- **R-FOL-4 Duplicates [EX-D4] (target).** Duplicate active → 409 “already following”; duplicate pending → 409 “request already pending” (same code, distinct message).
- **R-FOL-5 Privacy toggle keeps accepted [EX-D6] + [EX-2026-10-07] (unchanged).** Privacy changes keep accepted follows both ways. Private→public auto-accepts pending.
- **R-FOL-6 Moderation ownership [EX-2026-10-07].** Target (followed user) accepts/declines incoming requests; requester (follower) cancels own pending requests; an accepted follower can unfollow; self-follow is forbidden.
- **R-FOL-7 Counts [EX-2026-10-07].** Only accepted relationships count toward follower/following counts. Pending requests do NOT count.
  - `[CU] Gap:` today counts include every row (no pending concept exists yet).

### 2.4 Posts

- **R-POST-1 Shapes [CU] + visibility [EX-2026-10-07].** Create takes nullable content ≤5000, required Privacy enum, optional image ≤5 MB. Edit is owner-only partial (`content`/`image`/`removeImage`/`privacy`). Delete of a missing row or a foreign row → 404 (no existence leak). List takes `limit` 1–50 default 10 and positive `cursor` post-id, newest-first with `nextCursor`.
  - `[EX-2026-10-07]` Post visibility: `Public` → everyone; `Friends` → owner + accepted followers; `Private` → owner only. Accepted followers NEVER see `Private` posts. Account privacy (R-PROF-5) and post privacy are independent axes: account privacy decides whether a follow is `Accepted` or `Pending`; post privacy decides visibility given the accepted set. No contradiction.
  - `[CU] Implemented as:` cursor-paged single projection query (`limit+1` lookahead); delete scoped by owner id. `[CU] Gap:` current filter uses any-follow-row for `Friends` (no accepted/pending distinction yet); anonymous/owner/other branches otherwise match.
- **R-POST-2 Non-empty post [EX-D1] (target; gap vs [CU]).** A post MUST contain non-blank text and/or an image. Empty create or an edit resulting in no text and no image MUST be rejected with 400.
  - `[CU] Gap:` validators and service allow empty today; image-only rows store `""`. API-layer rule only; no column change decided.
- **R-POST-3 Post PATCH fields [EX-D5] + [EX-2026-10-07] (target; gap vs [CU]).** `content`, `image`, and `privacy` are independently editable on `PATCH /{id}`; privacy-only updates are valid. Tightening takes effect immediately; the endpoint is already owner-only.
  - `[CU] Gap:` edit DTO has no privacy field today.
- **R-POST-4 File lifecycle [CU].** Delete removes the row; edit swaps the file after save.
  - `[CU] Note:` orphan file on crash is current behavior; cleanup policy belongs to design, not requirements.
- **R-POST-5 No-op post PATCH [EX-2026-10-07].** A no-op PATCH (nothing changes, final post still satisfies R-POST-2) returns 200.
  - `[CU] Note:` service already returns success on no-op; consistent with the requirement.

### 2.5 Sessions

- **R-SES-1 Session model [CU].** Sessions are soft-revoked, never hard-deleted. Refresh is valid only when the session is not revoked.
- **R-SES-2 Session endpoints [EX-D3] + [CU].** `GET /me/sessions` lists active sessions with `isCurrent`; `DELETE /me/sessions/{id}` revokes one own session (self-revoke allowed); `DELETE /me/sessions` revokes all except current (current derived from the JWT session identity, never a client-supplied id).
  - `[CU] Implemented as:` dedicated session service; `isCurrent` from the `session_id` claim; strong revoke (session row + active tokens).
- **R-SES-3 Revocation scope [EX-2026-10-07].** Revoking a session blocks refresh immediately. Already-issued JWTs are NOT invalidated; they expire normally.
  - `[CU] Note:` matches current behavior (no token-validation revocation check; JWTs valid until short expiry, refresh blocked).

### 2.6 Change-email

- **R-EML-1 Confirmed flow only [EX-D7] (unchanged).** Email changes only through a confirmed flow, never via profile PATCH.
- **R-EML-2 Keep-old-working [EX-Q1] (unchanged).** The old address keeps working until confirmation swaps to the new address.
- **R-EML-3 Change-email flow [EX-2026-10-07] (target; not yet implemented).** Request queues a confirm link to the NEW address and always returns success (no “taken” signal at request time). Token expires after 1 hour and is single-use. Confirm with invalid/expired/used token → 400; address already confirmed to another user → 409. The database enforces email uniqueness. A newer change request invalidates the previous pending request. The old address remains usable until confirmation swaps `Email`, marks confirmed, and refreshes the security stamp.
  - `[CU] Gap:` no change-email flow exists in code today.

### 2.7 Deferred to v2

- **R-V2-1 [EX] (unchanged).** Comments, likes (reacts), and notifications are deferred to v2. Suggested order: comments → likes → notifications. No v1 endpoints.
- **R-DEF-1 Avatar deletion [EX-2026-10-07] (deferred).** Avatar deletion remains deferred; replace-only suffices.
- **R-DEF-2 Email throttling [EX-2026-10-07] (deferred).** Email throttling (429 path) remains deferred to an implementation slice.

## 3. Non-functional requirements

- **Security [EX/CU].** Owner-only writes; never trust client-supplied user ids; confirmed-email gate; anti-enumeration success-noop on resend/forgot and change-email request (taken signal only at confirm → 409); password 8–64, no complexity `[EX-2026-10-07]`; single-use refresh with reuse revocation + alert; revocation blocks refresh, JWTs expire normally; lockout on repeated failures; reset revokes all sessions; images ≤5 MB with content/signature/decoding validation `[EX-2026-10-07]`; email-change token 1h single-use with newer-invalidates-previous; DB-enforced email uniqueness; no secrets committed (UserSecrets/env). Identity defaults and JWT lifetimes are `[CU]` facts, not invented policies.
- **Performance [CU].** The API MUST avoid unbounded result sets (paged post lists, bounded counts via single queries). No measurable latency/throughput target is currently defined — targets are explicitly undefined, not zero.
  - `[CU] Strategy (not NFR):` no-N+1 projections, conditional updates. Moved detail: see design docs.
- **Reliability [CU].** Requests MUST NOT block on SMTP (queued email). Unhandled errors → 500 ProblemDetails; request logs carry user + client IP.
- **Data / infrastructure constraints [CU].** SQL Server; roles `User`/`Admin` seeded with only `User` assigned at register; `Post.PublishDate` is DB-generated and not client-settable; self-follow impossible; cascades define deletion semantics (posts/sessions/tokens cascade; follows do not).
- **Maintainability / testability [EX-process].** Auth/authz and core flows MUST have automated integration coverage. Harness choice is strategy only (see §8).
- **File storage [CU] + image rule [EX-2026-10-07].** Images ≤5 MB and MUST pass content/signature/decoding validation in addition to metadata; local storage layout and orphan handling are implementation strategy (see design docs).
  - `[CU] Gap:` current check is filename/MIME only.
- **Email delivery [CU].** Links use the configured base URL; delivery is best-effort background send with logged failures.
- **Secrets [EX-process].** Never commit real secrets in `appsettings*.json`; connection strings, JWT key, and email credentials come from UserSecrets/env.

## 4. Constraints & assumptions

- SQL Server is the assumed provider; provider-specific defaults and cascade choices are constraints only insofar as they affect observable behavior above.
- Post edits are multipart-only today (a JSON body with a file field silently no-ops). This is a `[CU]` quirk to preserve or fix explicitly — not a requirement that edits stay multipart.
- Pagination uses opaque cursor ids; clients MUST treat `nextCursor` as opaque.

## 5. Open questions (registry)

No remaining open questions. All OQs from the prior revision (`OQ-PATCH-1/2/3`, `OQ-FOLLOW-1`, `OQ-SESSION-1`, `OQ-EMAIL-1..5`, `OQ-PWD-1`, `OQ-IMG-1`) are resolved by the `[EX-2026-10-07]` decisions below. Deferred items (avatar DELETE, email throttle 429) are tracked as `[EX-2026-10-07]` deferred requirements R-DEF-1/R-DEF-2, not OQs.

## 6. Security review (requirements lens)

- Account enumeration: `[CU]` noop on resend/forgot and change-email request + take-at-confirm 409 preserves it. Throttle 429 is `[EX-2026-10-07]` deferred, not assumed.
- Refresh rotation/reuse: `[CU]` single-use + session-wide revoke + alert is the required observable guarantee; storage mechanism is not the requirement.
- Session revocation: `[EX-2026-10-07]` blocks refresh immediately; JWTs expire normally — do not assume early JWT kill.
- Password reset: `[CU]` revokes all sessions; password rule is `[EX-2026-10-07]` 8–64, no complexity.
- Email confirmation/change: token 1h single-use, invalid/expired/used → 400, taken → 409, DB uniqueness, newer-invalidates-previous, old usable until swap `[EX-2026-10-07]`.
- Ownership: owner-only writes `[CU]`; target accepts/declines, requester cancels, accepted unfollows, self-follow forbidden `[EX-2026-10-07]`; client-supplied ids MUST be ignored (sessions derive current from JWT).
- Uploads: content/signature/decoding validation required `[EX-2026-10-07]`; current ext-or-MIME check is a `[CU]` gap.
- Secrets/concurrency/lifetime: no secrets in repo `[EX-process]`; username/email uniqueness enforced with conflict mapping `[CU]` (+ DB email uniqueness `[EX-2026-10-07]`); token lifetimes are `[CU]` facts (refresh 30d, JWT short-lived, email-change 1h) with no invented numbers beyond what is decided/observed.

## 7. Traceability (decisions unchanged)

- D1 non-empty post → R-POST-2 `[EX]`; gap noted; ADR-002 is the proposal for how.
- D2 follow requests → R-FOL-2 `[EX]`; gap noted; ADR-001 is the proposal for how.
- D3 sessions → R-SES-2 `[EX]`; implemented.
- D4 duplicate messages → R-FOL-4 `[EX]`; gap noted (no pending state yet).
- D5 editable post privacy → R-POST-3 `[EX]`; gap noted.
- D6 toggle keeps accepted + auto-accept → R-PROF-5 + R-FOL-5 `[EX]`; gap noted.
- D7 email flow + username/gender immutable → R-EML-1 + R-PROF-3 `[EX]`; flow details R-EML-3 `[EX-2026-10-07]`.
- Q1 keep-old-working → R-EML-2 `[EX]`; Q2 auto-accept → R-FOL-5 `[EX]`; Q3 avatar DELETE deferred → R-PROF-4 + R-DEF-1 `[EX]`; Q4 404 unknown → R-PROF-1 `[EX]`; Q5 throttle → R-DEF-2 `[EX-2026-10-07]` deferred; Q6 v2 order → R-V2-1 `[EX]`.
- 2026-10-07 approvals (all `[EX]`): password 8–64 → R-AUTH-1/7; PATCH null semantics → R-PROF-2; post PATCH independence + privacy-only valid + no-op 200 → R-POST-3/5; accepted-only counts → R-FOL-7; follow ownership → R-FOL-6; post visibility Public/Friends/Private → R-POST-1 (+ R-FOL-3); account-vs-post privacy separation → R-PROF-5/R-FOL-2/5; revoke-blocks-refresh/JWT-expires → R-AUTH-5/R-SES-3; change-email token rules → R-EML-3; content image validation → R-PROF-4/NFR files; avatar-delete + throttle deferred → R-DEF-1/2.
- Preserved verbatim: old-email-works, auto-accept, 404 unknown, avatar DELETE deferred, v2 order comments→likes→notifications, accepted-kept, pending≠Friends.

## 8. Design pointers (implementation detail lives here, not above)

- Queries/mutations: `ARCHITECTURE.md`, `DATA_MODEL.md` (conditional updates, `limit+1` lookahead, projections, soft-revoke, cascades, defaults, indexes).
- Identity/auth plumbing: `ARCHITECTURE.md` (claim types, `CurrentUser`, thin controllers → services, validators, error envelope, Serilog enrichment).
- Files/email infra: `ARCHITECTURE.md` (local storage layout, queued jobs, base-URL links).
- Contracts/quirks: `API_DESIGN.md` (envelopes, status codes, multipart-only quirk, 423-debt on non-generic path).
- State machines/invariants: `DOMAIN_MODEL.md`, `USE_CASES.md`.
- Tests: `TEST_STRATEGY.md` (harness, fakes, no real DB/email, coverage expectations).
