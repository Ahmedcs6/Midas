---
name: code-review
description: Review .NET diffs for auth, EF Core, validation and tests. Use when reviewing Midas Api changes, git diff, or running /cr.
---

# Code Review

Report-only. Do not edit code.

## Check
- Auth: `[Authorize]`, owner-only writes, `CurrentUser` + `ClaimTypes.NameIdentifier`. Check `AuthController.cs`, `JwtService.cs`, refresh rotation.
- Data: EF N+1, tracking, migrations in `Migrations/`, Identity consistency.
- API: `Controllers/*` thin, logic in `Services/*`, validation in `Validators/*`, errors via `GlobalExceptionHandlingMiddleware`.
- Obs: Serilog `UserId`, no secrets in `appsettings*.json`, no real DB/email in tests.
- Tests: mirror `tests/Midas.IntegrationTests/PostTests.cs`, use `FakeEmailSender`.

## Output
List issues as `file:line` + severity (blocker/major/minor) + one-line fix. If clean, say so.
