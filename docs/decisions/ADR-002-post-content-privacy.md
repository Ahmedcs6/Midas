# ADR-002: Empty-post rule + editable privacy at API/service layers

Status: Approved design, pending implementation (2026-10-07; D1/D5 + EX-2026-10-07). Affects: validation, business behavior. Implementation checklist: see `docs/design/CODE_GAPS.md` G-02.

## Context
D1: post must have text and/or image. D5: privacy editable on edit.
Current: validators allow empty (`Content` nullable, image optional); `Post.Content` column `NOT NULL` (image-only stores `""`); edit DTO has no privacy field.

## Alternatives
- **A. API+service rules, no column change (chosen).** Create validator: `content non-blank OR image present`. Edit: service computes result (existing content/image after applying partials + privacy) and rejects empty-result 400. Add optional `privacy?` to `EditPostRequest`.
- **B. Column change (`Content` nullable) + DB check constraint.** Pushes invariant to storage.
- **C. Separate rules per endpoint.** Create strict, edit lenient.

## Decision
A. Emptiness is an API invariant (what a "post" means to clients), not a storage shape; `""`-for-image-only is a harmless encoding. Privacy becomes an optional edit field with same enum validation as create.

## Reasons / trade-offs
- Zero-downtime, no backfill, no provider-specific check constraints; keeps SQL Server `IsRequired` mapping untouched.
- Cost: rule lives in two places (create-validator + edit-service-check) — acceptable: edit-result cannot be validated from the DTO alone.
- C rejected: lenient edit would silently allow hollowing a post to empty, contradicting D1.

## Consequences
- New/changed validators; integration tests: empty-create 400, hollow-edit 400, privacy-tighten/loosen 200, image-only create stores and returns correctly.
