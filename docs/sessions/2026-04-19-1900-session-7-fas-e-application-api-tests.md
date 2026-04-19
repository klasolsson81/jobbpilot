---
session: 7
datum: 2026-04-19
slug: session-7-fas-e-application-api-tests
status: komplett
commits:
  - f6ef80a
  - 0ea0c6b
  - fbe46c9
  - 5bd47b8
  - fab6b24
  - 8e9d346
  - 4a79eb6
  - aed8a8b
  - 9a095c9
---

## Mål

Slutföra Fas D (Domain + Infrastructure + migration) och Fas E (Application commands/queries, API-endpoints, tester).

## Vad som gjordes

### Fas D — Domain, Infrastructure, Migration

Fyra commits (f6ef80a–8e9d346) som lades i början av sessionen (Fas D var pending efter context compaction i föregående session):

- **Domain common building blocks**: `AggregateRoot<TId>`, `IDateTimeProvider`, `DomainError`, `Result<T>`, `IDomainEvent` + architecture tests
- **Application common**: 4 pipeline-behaviors (Logging → Validation → Authorization → UnitOfWork), `IAppDbContext`, `AssemblyMarker`
- **JobAd aggregate**: `JobAd`, `JobAdId` (strongly-typed record struct per ADR 0011), `Company` value object, `JobAdStatus`, `JobSource`, domain events
- **Infrastructure**: `AppDbContext`, `JobAdConfiguration` (EF Core + OwnsOne Company), `DateTimeProvider`, `DependencyInjection.AddInfrastructure`
- **Migration**: `20260419145850_InitialCreate` (job_ads-tabell, snake_case via EFCore.NamingConventions)

### Fas E — Application, API, Tests

Klas identifierade tre gap innan commit: fel endpoint-sökväg (saknade `/api/v1/`), `CreateJobAdCommand` ej implementerat, bara 1 nytt test. Detaljerad 4-fix-spec gavs — commit blockerades tills alla verifierades.

**FIX 1 — CreateJobAd:**
- `CreateJobAdCommand` (record med nullable fält + `ICommand<Result<Guid>>`)
- `CreateJobAdCommandValidator` (FluentValidation: NotEmpty, MaxLength, URL-format)
- `CreateJobAdCommandHandler` (skapar Company + JobSource + JobAd, returnerar `Result<Guid>`)

**FIX 2 — API-prefix:**
- `MapGroup("/api/v1/job-ads")` i `Program.cs` per BUILD.md §6.1
- `Mediator.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped)` — kritisk fix för captive dependency-buggen (Mediator defaultar Singleton)

**FIX 3 — Handler unit tests (7 nya):**
- `CreateJobAdCommandHandlerTests`: happy path (NSubstitute mock), InvalidDates-failure, `Add Received(1)`
- `ListJobAdsQueryHandlerTests`: returnerar DTOs sorterade desc, tom lista (InMemory AppDbContext)
- `GetJobAdQueryHandlerTests`: hittar DTO med rätt id, returnerar null för okänt id
- Hjälpklasser: `FakeDateTimeProvider`, `TestAppDbContextFactory`

**FIX 4 — Integration tests:**
- `ListJobAdsTests.cs`: URL fixad till `/api/v1/job-ads`
- `CreateJobAdTests.cs`: POST → 201 → GET /{id} → 200 + `status:"Active"`

### Buggfixar under vägen

**ApiFactory race condition** — Parallella `IClassFixture<ApiFactory>`-instanser anropade `CreateHost` samtidigt och överskrev varandras `ConnectionStrings__Postgres` env var (process-global). Symptom: empty-list-testet såg data skapat av POST-testet. Fix: `static Lock _createHostLock` i `CreateHost`.

**Mediator Scoped** — Handlers injicerar `IAppDbContext` (Scoped) och behaviors injicerar `ICurrentUser` (Scoped). Mediator måste köra i Scoped kontext.

## Beslut

Inga nya ADRs. `DesignTimeDbContextFactory` lämnas med hårdkodade credentials för nu — acceptabelt för solo-dev där migration körs manuellt.

## Testresultat session-slut

```
Domain.UnitTests:       12/12
Application.UnitTests:  15/15
Architecture.Tests:       5/5
Api.IntegrationTests:     3/3
Totalt:                  35/35
```

## Curl smoke test

```
GET  /health                    → 200 {"status":"healthy","service":"JobbPilot.Api"}
GET  /api/v1/job-ads            → 200 []
POST /api/v1/job-ads            → 201 {"id":"fa8f222a-..."}
GET  /api/v1/job-ads/{id}       → 200 {...,"status":"Active"}
GET  /api/v1/job-ads            → 200 [{...}]
```

## Nästa session

Bestäm nästa aggregate/feature. Kandidater: JobSeeker aggregate, eller Feature-flagga mot nästa milestone (Fas 1 MILSTOLPE: manuellt skapa CV, submit "fake" ansökan, se i admin-audit).
