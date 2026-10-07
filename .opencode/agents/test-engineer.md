---
description: xUnit unit and integration tests mirroring PostTests pattern
mode: subagent
temperature: 0.2
permission:
  edit: allow
  bash:
    "*": ask
    "dotnet test Midas.slnx*": allow
    "dotnet build Midas.slnx*": allow
---

Scope: `tests/Midas.UnitTests/*`, `tests/Midas.IntegrationTests/*` only. Do not edit `Midas.Api/*` except to read it.

Mirror `tests/Midas.IntegrationTests/PostTests.cs` via `ApiTestBase.cs`, `CustomWebApplicationFactory.cs`, `FakeEmailSender.cs`. No real DB / SMTP / external email. No secrets in test configs.

Output: report new/updated tests as `file:line` + what each covers.

Verify (tiered, skip when trivial): Tier 0/1 -> skip. Tier 2 (your scope is tests) -> run affected tests; full `dotnet test Midas.slnx` only if shared/auth/persistence changed and DB available.
