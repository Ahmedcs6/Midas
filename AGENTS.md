# Midas - Social media Web API (.NET 10, EF Core + Identity + JWT)
Sln: `Midas.slnx` (SDK 10.0.x, `Midas.Api` targets `net10.0`). API: `Midas.Api/Program.cs`. Tests: `tests/`.

## Commands
- `dotnet build Midas.slnx`
- `dotnet test Midas.slnx`
- `dotnet run --project Midas.Api`
- `dotnet ef --project Midas.Api migrations add <Name>`

## Rules
- Layers: `Program.cs` DI + `Controllers/*` thin, logic in `Services/*` (+ `Interfaces/*`), validation in `Validators/*`, errors via `Middlewares/GlobalExceptionHandlingMiddleware`. Keep Serilog, `MapControllers()`, `MapStaticAssets()`.
- Auth: owner-only writes, `[Authorize]`, use `CurrentUser` + `ClaimTypes.NameIdentifier`. Refresh rotation + lockout live in `AuthController` / `JwtService` / `AccountService`.
- Data: EF Core + Identity consistency, avoid N+1 / tracking issues, migrations in `Midas.Api/Migrations/` via EF, no data loss.
- Secrets: never commit secrets in `appsettings*.json`; use UserSecrets (`Midas.Api` has `UserSecretsId`) / env vars.
- Tests: mirror `tests/Midas.IntegrationTests/PostTests.cs` (`ApiTestBase`, `CustomWebApplicationFactory`, `FakeEmailSender`). No real DB/email.
- Verify (tiered, skip when trivial): Tier 0 docs/comments/config-only (`*.md`, `SKILL.md`) -> skip build+test. Tier 1 trivial code (<=5 lines, 1 file, e.g. exit code / log text, no signature / DI / EF / migration change) -> skip both by default. Tier 2 real change (endpoint/service/validator, DI, EF model/migration, auth, shared code) -> `build` (+ targeted `test`; full `test` only if shared/auth/persistence changed and DB available).

## Agents / Skills
- Agents (`.opencode/agents/*`, all `mode: subagent`): `auth-reviewer` (auth hardening), `dotnet-api-dev` (endpoints), `persistence-expert` (EF/Identity/migrations), `test-engineer` (xUnit). Scopes + bash limits in each agent frontmatter.
- Skills: `code-review` (report-only `.NET` diff review, output `file:line` + severity). Command `/cr` runs it on `git diff`.
