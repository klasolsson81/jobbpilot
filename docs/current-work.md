# Current work — JobbPilot

**Status:** Session 7 KOMPLETT — Fas E (Application + API + tester) klar. Nästa: session 8 — push + Fas F (nästa aggregate eller feature).
**Datum:** 2026-04-19

---

## Aktivt nu

**FAS E KOMPLETT.** Application-lager, API-endpoints och fullständig testsvit för job-ads är på plats.

**Vad som är på plats (Fas 0 bootstrap + STEG 1-7):**
- AWS-foundation, Docker-compose, Claude Code-agenter/skills/hooks, GitHub-integration, docs
- ADR 0001-0011, .NET Solution, 5 src-projekt, 4 test-projekt
- Domain: JobAd aggregate (Company VO, JobAdStatus, JobSource), domain events
- Infrastructure: AppDbContext, JobAdConfiguration, DateTimeProvider, EF Core migration
- Application: 4 pipeline-behaviors, IAppDbContext, CreateJobAd + ListJobAds + GetJobAd
- API: 3 endpoints under `/api/v1/job-ads` via MapGroup, Mediator Scoped
- Tests: 35 tester (12 domain, 15 application, 5 arch, 3 integration) — alla gröna

**Commits session 7:**

| Commit | Innehåll |
|--------|----------|
| `f6ef80a` | feat(domain): Common building blocks + architecture tests |
| `0ea0c6b` | feat(application): Common building blocks + 4 pipeline behaviors per ADR 0008 |
| `fbe46c9` | feat(application): explicit EF Core dependency + arch test precision |
| `5bd47b8` | feat(domain): JobAd-aggregate med Company VO + JobAdStatus + JobSource |
| `fab6b24` | feat(infra): AppDbContext + JobAdConfiguration + DateTimeProvider + DependencyInjection |
| `8e9d346` | feat(infra): InitialCreate migration (job_ads table) |
| `4a79eb6` | feat(application): CreateJobAd command + ListJobAds + GetJobAd queries |
| `aed8a8b` | feat(api): job-ads endpoints under /api/v1/ via MapGroup + Mediator Scoped |
| `9a095c9` | test(job-ads): handler unit tests + integration tests + ApiFactory race fix |

**När nästa session startar:**

1. Kör `git log --oneline -12` — verifiera 9 nya commits + push om ej gjort
2. Läs `docs/sessions/` senaste session-logg (session 7)
3. Verifiera: `dotnet build --configuration Release` — 0 errors/warnings
4. Verifiera: `dotnet test --project tests/JobbPilot.Domain.UnitTests/... && ...Application.UnitTests/... && ...Architecture.Tests/...` — alla gröna
5. Bestäm nästa fas: nästa aggregate (JobSeeker?) eller feature

**Tekniska iakttagelser från session 7:**
- `Mediator.SourceGenerator` defaultar till `ServiceLifetime.Singleton` — måste explicit sätta `Scoped` annars captive dependency mot IAppDbContext
- `WebApplicationFactory` + eager connection string read: löses via `CreateHost` override som sätter env var FÖRE `base.CreateHost`
- Parallella `IClassFixture<ApiFactory>` overskrider varandras process-globala env var — löst med `static Lock` i `CreateHost`
- `xUnit v3 IAsyncLifetime` kräver `ValueTask` (inte `Task`) för `InitializeAsync`/`DisposeAsync`
- EF Core InMemory + `OwnsOne` fungerar korrekt i unit tests

## Klart senaste sessioner

- Session 1: research
- Session 2: plan
- Session 2.5: Design-research
- Session 3–5: AWS, Docker, agents, skills, hooks, GitHub, docs
- Session 6 ✅: ADR 0008-0011 + .NET Solution STEG 1
- Session 7 ✅: Domain, Infrastructure, Application, API, Tests (35 tester gröna)

## Kända begränsningar

Se **ADR 0006** för Claude Code-hooks-begränsningar.

**DesignTimeDbContextFactory** använder hårdkodade `postgres/postgres`-credentials för `migrations add`. Ej ett problem i runtime — bara för design-time verktyg. Behöver uppdateras om en CI-pipeline ska köra `database update`.
