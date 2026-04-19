# Current work — JobbPilot

**Status:** Session 8 KOMPLETT — STEG 3 (Auth-stack + JobSeeker) klar. Nästa: session 9 — nästa aggregate eller feature (Application-aggregate förslag).
**Datum:** 2026-04-20

---

## Aktivt nu

**STEG 3 (Auth-stack + JobSeeker) KOMPLETT.** ADRs, Identity + JWT + Redis, JobSeeker-aggregate, Application auth/profile-handlers, API-endpoints och 75 gröna tester.

**Vad som är på plats (Fas 0 bootstrap + STEG 1-3):**
- AWS-foundation, Docker-compose, Claude Code-agenter/skills/hooks, GitHub-integration, docs
- ADR 0001-0014, .NET Solution, 5 src-projekt, 4 test-projekt
- Domain: JobAd aggregate + JobSeeker aggregate (Preferences VO, domain events)
- Infrastructure: AppDbContext, AppIdentityDbContext, JwtTokenGenerator (RS256), RedisRefreshTokenStore, RedisAccessTokenRevocationStore, DateTimeProvider, EF Core migrations
- Application: 4 pipeline-behaviors, auth commands (Register/Login/Refresh/Logout), JobSeeker queries (GetMyProfile) + UpdateMyProfile
- API: `/api/v1/job-ads` (3 endpoints) + `/api/v1/auth` (4 endpoints) + `/api/v1/me` (3 endpoints)
- JwtBearer: explicit DefaultAuthenticateScheme + DefaultChallengeScheme (löser AddIdentity-konflikt)
- Tests: 75 tester (21 domain, 37 application, 6 arch, 11 integration) — alla gröna

**Commits session 8:**

| Commit | Innehåll |
|--------|----------|
| `3a47b30` | feat(domain): JobSeeker-aggregate med Preferences VO |
| `87dddaa` | feat(infra): JobSeekerConfiguration + AddJobSeekerAggregate migration |
| `6b42f45` | feat(application): auth commands (register, login, refresh, logout) |
| `9adcb3d` | feat(application): JobSeeker queries + UpdateMyProfile |
| `d69da44` | test(auth): handler unit tests för auth + JobSeeker (22 nya tester) |
| `6f8ad1d` | feat(api): JwtBearer setup + Auth/Me endpoints |
| `0dc52e0` | test(integration): Auth + Me integration tests + collection fixture |

**Tekniska iakttagelser session 8:**
- `AddIdentity` sätter `DefaultAuthenticateScheme` och `DefaultChallengeScheme` till sin cookie-handler — måste explicit sätta båda till `JwtBearerDefaults.AuthenticationScheme` EFTER `AddInfrastructure`
- `RequireAuthorization()` returnerar 404 (inte 401) om challenge-schemat är Identity-cookie och `/Account/Login` saknas
- `ICollectionFixture<ApiFactory>` via `[CollectionDefinition("Api")]` + `[Collection("Api")]` ger en delad factory — undviker 5x container-instanser
- `IAsyncLifetime.InitializeAsync` i xUnit v3: `ValueTask` (inte `Task`)
- Testcontainers Redis/Postgres: startade i `InitializeAsync` BEFORE factory.Services är tillgänglig

**När nästa session startar:**

1. Kör `git log --oneline -10` — verifiera 7 nya commits + push om ej gjort
2. Läs `docs/sessions/` senaste session-logg (session 8)
3. Verifiera: `dotnet build --configuration Release` — 0 errors/warnings
4. Verifiera: `dotnet test --configuration Release` — 75 tester gröna
5. Bestäm nästa fas: Application-aggregate (ansökningshantering), eller ett nytt domain-aggregate

## Klart senaste sessioner

- Session 1: research
- Session 2: plan
- Session 2.5: Design-research
- Session 3–5: AWS, Docker, agents, skills, hooks, GitHub, docs
- Session 6 ✅: ADR 0008-0011 + .NET Solution STEG 1
- Session 7 ✅: Domain, Infrastructure, Application, API, Tests (35 tester — JobAd)
- Session 8 ✅: STEG 3 — ADRs 0012-0014, Auth-stack, JobSeeker-aggregate, 75 tester

## Kända begränsningar

Se **ADR 0006** för Claude Code-hooks-begränsningar.

**DesignTimeDbContextFactory** använder hårdkodade `postgres/postgres`-credentials för `migrations add`. Ej ett problem i runtime — bara för design-time verktyg.
