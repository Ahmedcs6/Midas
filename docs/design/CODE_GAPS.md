# CODE GAPS (requirement → code, docs-only backlog — no code changed here)

Each item: approved requirement, current code behavior with `file:line`, and what must change. Ordered per ARCHITECTURE execution slices.

## G-01 Password 8–64, no complexity [EX-2026-10-07 → R-AUTH-1/7]
- Current: `Midas.Api/Validators/RegisterRequestValidator.cs:33-34`, `ResetPasswordRequestValidator.cs:15-16`, `LoginRequestValidator.cs:14-15` enforce `NotEmpty` only; `Midas.Api/Extensions/IdentityAuthExtensions.cs:12-19` sets no `Password` options (Identity defaults apply).
- Change: length 8–64 validators on register/reset (+ login pass-through not needed); remove reliance on Identity-default complexity; tests: too-short/too-long → 400.

## G-02 Post nonempty + privacy edit + no-op [EX-D1/D5/EX-2026-10-07 → R-POST-2/3/5]
- Current: `Midas.Api/Validators/CreatePostRequestValidator.cs:9-11` length-only, no at-least-one rule; `EditPostRequest.cs:5-7` has no `Privacy`; `PostService.cs:31-53` no result-nonempty check (no-op → 200 already correct).
- Change: create validator at-least-one; add optional `privacy?` to edit DTO + enum validation; service computes post-edit result and rejects empty → 400; privacy-only valid; tests per TEST_STRATEGY slice 3.

## G-03 Follow Status + IsPrivate + moderation [EX-D2/D6/EX-2026-10-07 → R-FOL-2/4/6, R-PROF-5]
- Current: `Midas.Api/Models/Follow.cs:3-12`, `ApplicationUser.cs:4-36`, `FollowConfiguration.cs:9-22` have no `Status`/`IsPrivate`; `UserService.cs:74-155` only immediate insert + delete; no accept/decline/list, no toggle.
- Change: approved migration in DATA_MODEL (Status + IsPrivate + index + Accepted backfill); status-aware insert; target-accept/decline, requester-cancel; duplicate messages per R-FOL-4; toggle with accepted-kept + auto-accept.

## G-04 Accepted-only counts + visibility [EX-2026-10-07 → R-FOL-7, R-POST-1, R-PROF-1]
- Current: `UserService.cs:172-173` counts all rows; `PostService.cs:84-98` any-row grants Friends (Private correctly owner-only).
- Change: add `Status==Accepted` predicate to counts + Friends filter; Private never to followers; tests: pending invisible + uncounted.

## G-05 Null-clear semantics [EX-2026-10-07 → R-PROF-2]
- Current: `UserService.cs:17-21` null-coalescing (null = keep); validators gated `.When(not null)` (`EditUserRequestValidator.cs:9-30`).
- Change: omitted = keep; explicit null clears whole Address / nullable About/BirthDate, 400 on First/Last null; whole-Address object semantics (no per-subfield null clearing); tests per TEST_STRATEGY slice 4.

## G-06 Change-email flow [EX-D7/EX-2026-10-07 → R-EML-3]
- Current: no endpoint/token flow in `Midas.Api` (only `ConfirmEmailJob`, `PasswordResetJob`, `SecurityAlertJob` exist).
- Change: request (queue link to NEW, always success) + confirm (`userId+token`, 1h single-use, newer-invalidates-previous, invalid/expired/used → 400, taken → 409, DB uniqueness, swap + SecurityStamp); tests per TEST_STRATEGY slice 7.

## G-07 Image content validation [EX-2026-10-07 → R-PROF-4, NFR files]
- Current: `ImageFileRules.cs:6-16` ext OR `image/*` MIME, `null→true`, 5 MB; enforced in create/edit/avatar validators; `LocalFileStorage.cs:21-32` no check.
- Change: add signature/magic-byte + decode validation in addition to metadata; keep 5 MB; spoofed content → 400; tests per TEST_STRATEGY slice 5.

## Already implemented (no gap)
- Sessions list/revoke-all-except-current + strong revoke (`SessionService`, `UsersController` session routes); scope confirmed: refresh blocked, JWTs expire normally.
- Anti-enumeration noops, lockout 423 path, reset-revokes-all, 404 unknown usernames, PATCH-403/DELETE-404 asymmetry, multipart-only quirk (preserve or change only via ADR).
- Deferred (no work): avatar DELETE (R-DEF-1), email throttle 429 (R-DEF-2).
