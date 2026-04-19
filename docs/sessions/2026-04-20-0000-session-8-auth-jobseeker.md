---
session: 8
datum: 2026-04-20
slug: session-8-auth-jobseeker
status: komplett
commits:
  - 3a47b30
  - 87dddaa
  - 6b42f45
  - 9adcb3d
  - d69da44
  - 6f8ad1d
  - 0dc52e0
---

# Session 8 — STEG 3: Auth-stack + JobSeeker aggregate

## Mål

Slutföra STEG 3 från BUILD.md: ADRs 0012-0014, Identity + JWT + Redis (FAS A-B), JobSeeker-aggregate (FAS C), Application-handlers (FAS D), API-endpoints + integration-tester (FAS E).

## Vad som slutfördes

### FAS A–B: ADRs + packages (från föregående session-kontext)
ADR 0012 (Identity), ADR 0013 (JWT RS256), ADR 0014 (Redis refresh tokens) skrivna. Identity DbContext, JwtSettings, JwtTokenGenerator, RedisRefreshTokenStore, RedisAccessTokenRevocationStore registrerade.

### FAS C: JobSeeker-aggregate
`JobSeeker` entity med `Preferences` value object (JobSearchPreferences VO). EF Core-konfiguration + migration `AddJobSeekerAggregate`. Registrering via `RegisterCommandHandler` skapar en `JobSeeker` för varje ny `ApplicationUser`.

### FAS D: Application-handlers
Auth commands: `RegisterCommand`, `LoginCommand`, `RefreshCommand`, `LogoutCommand`. JobSeeker queries: `GetMyProfileQuery`, `UpdateMyProfileCommand`. 22 unit tests gröna.

**Bugfix under FAS D:** `CreateHandler` i `RegisterCommandHandlerTests` overwriting `db.JobSeekers.Returns(newSeekSet)` även när `db` skickades in från test → fixad genom `if (db is null)` guard.

### FAS E: API-endpoints + integration-tester
`AuthEndpoints`: POST /register, /login, /refresh, /logout med httpOnly refresh cookie (`jobbpilot-refresh`). `MeEndpoints`: GET /, GET /profile, PATCH /profile — alla bakom `RequireAuthorization()`.

**Root cause för 404 på RequireAuthorization:** `AddIdentity` sätter `DefaultAuthenticateScheme` och `DefaultChallengeScheme` till sin cookie-handler. `AddAuthentication(JwtBearerDefaults.AuthenticationScheme)` (string-overload) sätter bara `DefaultScheme`, inte de mer specifika. Challenge gick till `/Account/Login` → 404. **Fix:** byt till lambda-form:
```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
```

**ApiFactory:** RSA keypair skapas i konstruktorn (temp PEM-filer). Env vars sätts i `CreateHost`-override (inte `ConfigureWebHost.ConfigureAppConfiguration` — fungerar inte för `WebApplication.CreateBuilder`-appar).

**Kollisionsproblem löst:** Fem test-klasser med `IClassFixture<ApiFactory>` skapade 5x Postgres + 5x Redis-containrar → sista misslyckades. Löst via `[CollectionDefinition("Api")]` + `ICollectionFixture<ApiFactory>` — en delad factory.

**Data isolation:** `ListJobAdsTests` antog tom databas → bröt när `CreateJobAdTests` körde först. Fixad genom att byta assertion till `json.ValueKind.ShouldBe(JsonValueKind.Array)`.

## Integration tests: 11 gröna
- RegisterTests: 3 (valid register, duplicate email 400, blank displayName 400)
- LoginTests: 2 (valid login, wrong password 401)
- MeTests: 3 (no token 401, valid token 200, profile 200)
- CreateJobAdTests: 1
- ListJobAdsTests: 1

## Nästa session

Nästa aggregate/feature att välja bland:
- Application-aggregate (JobbPilot core — skapa + hantera ansökningar)
- Notifikations-system
- Nästa steg i BUILD.md §2
