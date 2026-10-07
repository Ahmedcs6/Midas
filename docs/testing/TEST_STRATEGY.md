# TEST STRATEGY

Mirrors `tests/Midas.IntegrationTests/PostTests.cs` patterns: `ApiTestBase` + `CustomWebApplicationFactory` + `FakeEmailSender`. No real DB/email. Unit tests only for pure logic (validators, guards); behavior via API tests.

## Conventions (existing, keep)
- Auth helpers: `LoginAsMainAsync`, per-test unique users (`EnsureUniqueUserAsync`), `JsonOptions` camelCase + enum converter.
- Multipart for post/avatar edits (`IFormFile` ⇒ `[FromForm]` inferred; JSON PATCH silently no-ops — tests must use multipart).
- Assert status + envelope (`ApiResponse<T>`), then state (list/counts/visibility), not just happy-path codes.

## Per-slice coverage (v1; `[EX-2026-10-07]` required unless noted)
1. **Follows/requests**: follow public → accepted + visible Friends posts; request private → pending, NOT visible, excluded from counts; duplicate active → 409; duplicate pending → 409 distinct message; self → 400; cancel pending → 204; accept → visible; decline → 204 + still invisible; non-owner accept → 403/404.
2. **Privacy toggle**: public→private keeps accepted; private→public auto-accepts pending; toggle same-value no-op; visibility flips immediately; Private posts never visible to followers.
3. **Posts**: empty create → 400; image-only create → 201; hollow edit (clear text + remove image, no replacement) → 400; privacy-only edit → 200; no-op edit → 200; privacy tighten/loosen → 200 + visibility follows; delete foreign → 404; delete missing → 404.
4. **Profile**: omitted = keep; explicit null clears About/BirthDate/whole-Address, 400 on First/Last null; password 8–64 enforced, no complexity.
5. **Images**: oversized → 400; spoofed content (bad signature/undecodable) → 400 despite image extension/MIME.
6. **Sessions**: list shows `isCurrent`; logout-one revokes + refresh fails; logout-all spares current; revoked JWT still validates until expiry while refresh fails; reset-password still revokes all (existing tests keep passing).
7. **Change-email**: request → link queued to NEW address (fake sender assertion); old login still works pre-confirm; confirm swaps + new login works; expired/used → 400; taken-at-confirm → 409; newer request invalidates previous.
8. **Regression**: full `AuthTests`, `UserTests`, `UserEdgeCaseTests`, `PostTests` green; follower counts reflect accepted-only; anon/public rules unchanged.

## Security/failure tests
- Refresh reuse → session revoked + 401; revoked-session refresh → 400; foreign session delete → 404; foreign post edit → 403; unconfirmed login/forgot → 403 (existing).
- Email throttle 429 is `[EX-2026-10-07]` deferred — no throttle tests in v1.

## Debt to record while testing
- Non-generic `ToActionResult` lacks `Locked` arm (throws on 423 path) — cover with a locked-login API test; fix when touching errors.
- Multipart-only edit quirk — pin with a test (JSON PATCH no-op documented) so a future contract change is deliberate.
