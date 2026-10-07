# ADR-001: Follow requests as `Follow.Status` (single table)

Status: Approved design, pending implementation (2026-10-07; D2 + EX-2026-10-07). Affects: data model, API contract, business behavior. Implementation checklist: see `docs/design/CODE_GAPS.md` G-03/G-04.

## Context
D2 requires Instagram-like follows: public → immediate, private → pending + owner accept/decline.
Current: `Follow` PK `(FollowerId,FollowingId)`, immediate insert, 409 on PK conflict, `CK_Follow_NoSelfFollow`, both FKs `NoAction`.

## Alternatives
- **A. Status column on `Follow` (chosen).** Add `Status {Pending=0, Accepted=1}`, default Accepted backfill; add `User.IsPrivate`; index `(FollowingId,Status)`.
- **B. Separate `FollowRequest` table.** Pending lives apart; accept moves row to `Follow`.
- **C. Separate table + keep history.** B plus audit of declines.

## Decision
A. One row per pair for life; status flips. Reads add one predicate (`Status==Accepted`).

## Reasons / trade-offs
- No dual-write divergence (B risks request-row + follow-row disagreeing on crash; A is a single atomic insert/update).
- PK preserved → existing 409-on-conflict path and self-follow constraint keep working; backfill is trivially correct (all old rows were immediate ⇒ Accepted).
- Cost: `Follow` gains a state machine (transitions in DOMAIN_MODEL); counts/visibility must remember the predicate (covered by tests).
- B/C only win if pending needs different columns — it doesn't (same pair + timestamp).

## Consequences
- 1 migration; `UserService` owns transitions; accept uses conditional update (`WHERE Pending`) for race safety.
