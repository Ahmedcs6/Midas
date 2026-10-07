# API DESIGN

Base `api/[controller]`. Envelopes: success `{success:true,message?,data?}` via `ToActionResult`; errors `{success:false,message}` with mapped status; unhandled → 500 ProblemDetails. Auth: Bearer JWT; identity from `CurrentUser.UserId` + `session_id` claim (never client-supplied ids).

Status-code note: codes below are API-design choices unless marked [EX] (explicitly required by user decisions D1–D8, gate Q-decisions, or 2026-10-07 approvals). Tags `[CU]` = current code behavior (with gaps noted); `[EX-2026-10-07]` = approved 2026-10-07 decisions from REQUIREMENTS. Account privacy and post privacy are independent axes (REQUIREMENTS R-PROF-5/R-POST-1): account privacy decides whether a follow is Accepted or Pending; post privacy decides visibility given the accepted set; Private posts are never visible to followers.

## Users (`/api/users`, `[Authorize]` unless noted)
| Method + route | Req | Success | Errors |
|---|---|---|---|
| `GET /{userName}` `[AllowAnonymous]` | — | 200 `UserResponse` (counts: accepted-only [EX-2026-10-07]; [CU] gap: counts include every row until Status lands) | 404 unknown [EX-Q4] |
| `PATCH /me` | JSON partial `{firstName?,lastName?,birthDate?,about?,address?}`; omitted = keep; explicit `null` = clear whole Address / nullable field, 400 on required First/Last [EX-2026-10-07]; UserName/Gender/Email never applied via this route [EX-D7] ([CU]: DTO omits those fields) | 200 (incl. no-op) | 400 validation / 404 me-missing (defensive; normally impossible) |
| `POST /me/avatar` | multipart `image!` (≤5 MB, content/signature/decoding check [EX-2026-10-07]; [CU] gap: ext/MIME only) | 200 | 400 |
| `PATCH /me/privacy` **[EX-D6/EX-2026-10-07]** | JSON `{isPrivate!}` (not yet implemented) | 200 `{isPrivate}` | 400 |
| `POST /me/email/change` **[EX-2026-10-07]** | JSON `{newEmail!}` valid ≤256, ≠ current (not yet implemented) | 200 always (incl. same→400, invalid→400) | 400 |
| `POST /me/email/confirm` **[EX-2026-10-07]** | query/body `{userId!, token!}` (Base64Url); 1h single-use; newer request invalidates previous; taken-at-confirm → 409 | 200 | 400 invalid/expired/used / 404 user / 409 taken |
| `POST /follow/{userName}` | — ([CU]: immediate insert, 200-no-body; [EX] target: 200 `{status:accepted\|pending}`) | 200 `{status:accepted\|pending}` [EX-D2/EX-2026-10-07] | 400 self / 404 unknown / 409 active ("already following") or pending ("request already pending") [EX-D4] |
| `DELETE /follow/{userName}` | — | 204 (deletes Accepted OR Pending by caller) | 404 no-row/unknown |
| `GET /me/follow-requests` **[EX-D2/EX-2026-10-07]** | — (not yet implemented) | 200 `[{followerName, requestedAt}]` (owner-only by construction) | 401 |
| `POST /me/follow-requests/{followerName}/accept` **[EX-D2/EX-2026-10-07]** | — (not yet implemented) | 200 (Pending→Accepted) | 404 no-pending/unknown / 409 already-accepted race |
| `DELETE /me/follow-requests/{followerName}/decline` **[EX-D2/EX-2026-10-07]** | — (not yet implemented) | 204 (delete Pending) | 404 |
| `GET /{userName}/posts?limit&cursor` `[AllowAnonymous]` | `limit` 1–50 default 10, `cursor` +id? | 200 `{items,nextCursor}` with Accepted-only Friends rule [EX-2026-10-07] ([CU] gap: any-row grants Friends until Status lands) | 400 range / 404 user |
| `GET /me/sessions` | — | 200 `[{id,client,os,browser,device,ip,createdAt,lastActivityAt,isCurrent}]` (implemented 2026-10-04 → `SessionService.ListAsync`) | 401 |
| `DELETE /me/sessions/{id}` | — | 204 (own only; self allowed; strong revoke) | 404 foreign/missing |
| `DELETE /me/sessions` | — | 204 (revoke all except current) | 401 |

## Posts (`/api/posts`, `[Authorize]`)
| Method + route | Req | Success | Errors |
|---|---|---|---|
| `POST /` | multipart `{content?, privacy!, image?}`; require text-nonblank OR image [EX-D1]; password n/a; images ≤5 MB + content check [EX-2026-10-07] | 201 `PostResponse` | 400 empty/invalid |
| `PATCH /{id}` | multipart `{content?, image?, removeImage=false, privacy?}` [EX-D5/EX-2026-10-07: content/image/privacy independent; privacy-only valid]; result must satisfy D1, else 400; no-op → 200 [EX-2026-10-07] ([CU] gap: no privacy field, empty allowed) | 200 | 400 empty/invalid / 403 foreign [CU-quirk, preserved] / 404 missing |
| `DELETE /{id}` | — | 204 | 404 missing OR foreign [CU-quirk: no leak, preserved — differs from PATCH 403 intentionally until ADR decides otherwise] |

Known quirk (documented, not silently changed): `EditPostRequest` contains `IFormFile` → `[FromForm]` inferred; JSON PATCH bodies silently no-op (see `PostTests` comment). New `privacy` field rides the same multipart contract. A JSON-PATCH alternative is deferred (would change contract; needs ADR if pursued).

## Auth (`/api/auth`, anonymous) — unchanged contracts
`register→201/409 (password 8–64, no complexity [EX-2026-10-07])` · `resend-confirm-email→200-noop` · `confirm-email(userId,token)→200/400/404` · `login{email,password,client}→200/401-generic/403-unconfirmed/423-locked` · `refresh{token}→200/400-expired-unknown/401-reuse` (reuse revokes session + alerts; revoked-session refresh → 400) · `forgot-password→200-noop/403-unconfirmed` · `reset-password{id,token,newPassword}→200/400/404` (+ revokes all). Revocation blocks refresh; JWTs expire normally [EX-2026-10-07].

## Validation behavior
FluentValidation + filter → 400 with field errors before services run. Required validators (gap vs [CU] — see CODE_GAPS.md): password 8–64 (`Register`/`ResetPassword`), `CreatePost(at-least-one)`, `EditPost(result-nonempty incl. privacy-only edits)`, `PrivacyRequest`, `ChangeEmailRequest`, content/signature image check. Service-level re-checks kept for D1 (edit-result rule cannot be expressed on request DTO alone).
