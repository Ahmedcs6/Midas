# ADR-004: Change-email keeps old address working until confirm

Status: Approved design, pending implementation (2026-10-04 option A + finalized 2026-10-07).

## Context
D7: email mutable via confirmed flow. Identity's email-confirm and password-reset token providers already exist in the codebase patterns.

## Alternatives
- **A. Keep-old-working (chosen).** `POST /me/email/change` queues link to NEW address; login/forgot keep using OLD until `confirm` swaps `Email`, sets `EmailConfirmed=true`, refreshes `SecurityStamp`.
- **B. Lock-on-request (rejected).** Old email stops working immediately; account unusable until confirm (or recovery via support).

## Decision
A. Fewer lockouts (lost access to new inbox doesn't brick the account); matches register/confirm mental model; taken-address discovered at confirm time (409) rather than leaking via request-time errors (anti-enumeration preserved).

## Finalized token rules [EX-2026-10-07]
- Token expires after 1 hour, single-use; invalid/expired/used → 400; taken-at-confirm → 409; DB enforces email uniqueness; newer request invalidates the previous pending request; old address usable until swap.

## Consequences
- New `ChangeEmailJob` (confirm-template reuse); confirm re-checks uniqueness; sessions unaffected except the swap itself. Implementation checklist: see `docs/design/CODE_GAPS.md` G-06. `R-DEF-2` throttle stays deferred.
