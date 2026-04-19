# Current work — JobbPilot

**Status:** Fas 0 kod-scaffolding STEG 1 KOMPLETT. Nästa: STEG 2 — Mediator-pipeline + IAppDbContext + första aggregate.
**Datum:** 2026-04-19

---

## Aktivt nu

**FAS 0 KOD-SCAFFOLDING STEG 1 KOMPLETT.** .NET Solution + 5 src-projekt + 4 test-projekt
scaffoldade. Build grön, 4 Smoke-tester gröna, Husky pre-commit test-gates aktiva.

**Vad som är på plats (Fas 0 bootstrap + STEG 1):**
- AWS-foundation (Terraform: budgets, KMS, Bedrock model access, secrets manager)
- Docker-compose dev/test (Postgres 18 + Redis 8.6 + Seq 2025.2)
- 11 Claude Code-agenter (7 Opus 4.7 + 4 Sonnet 4.6 per ADR 0002)
- 5 design-skills (DESIGN.md som index per ADR 0003)
- 7 Claude Code-hooks + 2 Husky-hooks (med kända begränsningar i ADR 0006)
- GitHub-integration (templates, CODEOWNERS, Dependabot, branch protection B-nivå)
- Komplett docs-struktur (10 ADRs med index, runbooks, session-loggar)
- ADR 0008 (pipeline-order), ADR 0009 (no-repository), ADR 0010 (worker comp root)
- `JobbPilot.sln` + `Directory.Build.props` + `Directory.Packages.props` (CPM)
- `global.json` (SDK 10.0.200 latestPatch)
- 5 src-projekt: Domain, Application, Infrastructure, Api, Worker (per ADR 0010)
- 4 test-projekt: Domain.UnitTests, Application.UnitTests, Api.IntegrationTests, Architecture.Tests
- xUnit v3 MTP v2 (`xunit.v3.mtp-v2 3.2.2`), Shouldly, NSubstitute, NetArchTest.Rules
- tests/Directory.Build.props (parent-import + CA1707-suppression för underscore-testnamn)
- Husky pre-commit: dotnet format + 3 test-gates aktiva (Domain, Application, Architecture)

**När nästa session startar (STEG 2):**

1. Kör `git log --oneline -8` — verifiera STEG 1-commits överst
2. Läs `docs/sessions/` senaste session-logg (session 6)
3. Verifiera: `dotnet build --configuration Release` — ska ge 0 errors/warnings
4. Verifiera: `dotnet test --configuration Release` — ska ge 4/4 gröna
5. Kör `bash .husky/pre-commit` — ska passera alla gates

**STEG 2 — vad som ska göras:**
- `IAppDbContext`-interface i JobbPilot.Application (DbSet per aggregate root + SaveChangesAsync)
- `IUnitOfWork`-interface i JobbPilot.Application
- `AppDbContext : DbContext, IAppDbContext` i JobbPilot.Infrastructure
- Mediator-pipeline-behaviors (Logging → Validation → Authorization → UnitOfWork) per ADR 0008
- Första aggregate: `JobSeeker` med invarianter + domain events
- Initial EF Core-migration

**Aktiva skyddslager på main:**
- Pre-push gitleaks-scan med 3-stegs fallback-lookup
- Branch protection B-nivå (no force push, no deletion)
- Claude Code-hooks (alla 7 fungerande)
- Husky pre-commit: dotnet format + 3 test-gates (Domain, Application, Architecture)
- Husky pre-push: gitleaks-scan

## Klart senaste session

- Session 1: research (`docs/research/SESSION-1-*.md`).
- Session 2: plan godkänd (`docs/research/SESSION-2-PLAN.md`).
- Session 2.5: Claude Design-research + skill-arkitektur.
- Session 3–4: AWS, Docker, agents, skills, hooks, GitHub-integration, docs-struktur.
- Session 5: CLAUDE.md uppdaterat, guard-spec-files fix, Bootstrap IAM-user raderad.
- Session 6 ✅: ADR 0008-0010 + .NET Solution STEG 1 (se commithistorik nedan).

## Committat i session 6

| Commit | Innehåll |
|--------|----------|
| `87e870d` | docs(decisions): ADR 0008-0010 — pipeline order, no repo, worker comp root |
| *(pending)* | feat(solution): JobbPilot.sln + Directory.Build.props + global.json (CPM) |
| *(pending)* | feat(src): scaffold 5 .NET projekt per ADR 0010 (Domain/App/Infra/Api/Worker) |
| *(pending)* | feat(tests): scaffold 4 xUnit v3 testprojekt med Shouldly |
| *(pending)* | feat(husky): aktivera test-gates i pre-commit |
| *(pending)* | docs(session): session 6 — Fas 0 kod-scaffolding STEG 1 |

## Committat i session 3-5 (referens)

| Commit | Innehåll |
|--------|----------|
| `1e98ce4` | docs(session): fix session 5 log — fill in STEG 12 commit hash |
| `4e8128a` | docs(session): STEG 12 — Fas 0 bootstrap KOMPLETT |
| `6c37a1c` | docs(decisions): ADR 0006 add 4th limitation — silent dependency failures |
| `1879b4b` | fix(hooks): bash-native parsing in guard-spec-files (drop jq dependency) |
| `bda9f72` | docs(claude): STEG 10 — Session Protocol + Docs structure + spec-drift fix |

---

## Nästa (STEG 2+)

**STEG 2 — Domain + Infrastructure grund:**
- IAppDbContext + IUnitOfWork interfaces i Application
- AppDbContext i Infrastructure
- Mediator-pipeline-behaviors per ADR 0008
- Första aggregate (JobSeeker)
- Initial EF Core-migration

**STEG 3+ — Application layer:**
- Commands + Queries för JobSeeker, Resume, Application
- API-endpoints
- MILSTOLPE Fas 1: Manuellt skapa CV, submit "fake" ansökan, se i admin-audit

---

## Kända begränsningar

Se **ADR 0006** för Claude Code-hooks-begränsningar.

**Nya tekniska iakttagelser från STEG 1:**
- `.NET 10 SDK` skapar `.slnx` som default — NuGet + solution-folders ger "Invalid framework identifier". Workaround: `dotnet new sln --format sln` + manuell rensning av solution-folder entries. Dokumenterat i session 6-loggen.
- `dotnet test <dir>` kräver nu `--project`-flagga i SDK 10.0.202 (breaking change vs tidigare SDK).
- `tests/Directory.Build.props` måste explicit importera root-filen via MSBuild `GetDirectoryNameOfFileAbove`-funktion — annars blockeras TargetFramework-arv.
