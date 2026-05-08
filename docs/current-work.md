# Current work — JobbPilot

**Status:** STEG 9 KLAR. ADR 0023 skriven. Pipeline-bug fångad och fixad. TD-17, TD-18, TD-19 nya. Nästa: STEG 10 — kräver beslut.
**Senast uppdaterad:** 2026-05-08
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu

**STEG 9 klar.** Worker-pipeline-aktivering + Hangfire-infrastruktur (ADR 0023) implementerad och redo att pushas.

### STEG 9 — Worker-pipeline-aktivering + Hangfire-infrastruktur

**Strategi (ADR 0023):** Aktivera Worker-shell (ADR 0010), splittra `AddInfrastructure` i moduler för CleanArch-renhet, registrera Worker-stubs av audit-portarna (per ADR 0022), lägga till Hangfire 1.8.23 + Hangfire.PostgreSql 1.21.1, första schemalagda jobb (`detect-ghosted` daily 03:00 UTC).

**Domain (Fas 9.1):**
- `Application`-aggregatet utökat med `LastStatusChangeAt` (DateTimeOffset, NOT NULL) + `GhostedThresholdDays` (int, NOT NULL, default 21) — per BUILD.md §schema rad 715–727 (per Application, INTE per JobSeeker som steg-tracker tidigare sade)
- STRIKT-flippning av `LastStatusChangeAt` i `Create()`, `TransitionTo()`, `MarkGhosted()` — INTE i `AddFollowUp`/`AddNote`/`SoftDelete`
- Ingen `SetGhostedThresholdDays`-metod i Fas 1 (deferred till Fas 3 när per-app-override-feature får UI)

**Migration (Fas 9.2):**
- `20260508093139_AddApplicationStaleDetectionFields` applicerad mot dev-DB
- Backfill `last_status_change_at = NOW()` (per Klas tillägg #1) — *inte* `updated_at` som hade orsakat mass-batch-ghosting vid första cron
- Partial index `ix_applications_stale_detection` på `(last_status_change_at)` med `WHERE status IN ('Submitted', 'Acknowledged') AND deleted_at IS NULL`

**Infrastructure DI-refaktor (Fas 9.3):**
- `AddInfrastructure` blir composition. Tre nya extensions:
  - `AddPersistence(configuration)` — DbContext, IAppDbContext, IDateTimeProvider
  - `AddIdentityAndSessions(configuration)` — Identity, Redis, JWT, ICurrentUser via HTTP (HTTP-only)
  - `AddHttpAuditing()` — HTTP-baserade audit-portar (HTTP-only)
- Worker laddar bara `AddPersistence` + egna Worker-stubs

**Application (Fas 9.4):**
- `StaleApplicationSpecification` i `Application/Applications/Specifications/` — tvådelat predikat (SQL Status-filter + client-side AddDays-check)
- `DetectGhostedApplicationsJob` orchestrator i `Application/Applications/Jobs/GhostedDetection/` — AsNoTracking + per-id `mediator.Send`-loop + cancel-token-check + progress-log var 25:e
- `MediatorPipelineBehaviors` i `Application/Common/` — delad konstant + `AddMediatorPipelineBehaviors()` extension (pipeline-bug-fix, se nedan)

**Worker-aktivering (Fas 9.5):**
- 3 stub-impls i `JobbPilot.Worker/Auditing/`:
  - `WorkerSystemUser` (Singleton, ICurrentUser): UserId=null, IsAuthenticated=false
  - `WorkerCorrelationIdProvider` (Scoped, ICorrelationIdProvider): instans-fält Guid → en correlation-ID per Hangfire-job-execution (K1-fix från arch-rapport)
  - `WorkerRequestContextProvider` (Scoped, IRequestContextProvider): null IP/UA
- `RecurringJobRegistrar` IHostedService — registrerar `detect-ghosted` cron daily 03:00 UTC
- `Program.cs` full refaktor: AddPersistence + AddApplication + Worker-stubs + AddMediator + AddMediatorPipelineBehaviors + AddHangfire (schema=hangfire, prepareIfNecessary=true) + AddHangfireServer (WorkerCount=4) + AddHostedService<RecurringJobRegistrar>
- `Worker.cs` heartbeat-stub TAS BORT (ersatt av Hangfire-server)
- Hangfire-paket via `Directory.Packages.props`: Hangfire.Core 1.8.23, Hangfire.AspNetCore 1.8.23, Hangfire.PostgreSql 1.21.1
- **CVE-fix:** Newtonsoft.Json 13.0.3 transitiv pinning för CVE-2024-21907 / GHSA-5crp-9r3c-p9vr (Hangfire drar in vulnerable 11.0.1)

**Architecture-tester (Fas 9.6):**
- `WorkerLayerTests.cs` — 5 nya tester: Worker AspNetCore-isolation, job i Application-lagret, Worker-stubs implementerar portar, RecurringJobRegistrar i Worker-assembly, MediatorPipelineBehaviors.InOrder verifierad

**Worker integration smoke-test (Fas 9.7 — Klas tillägg #3):**
- Nytt projekt `JobbPilot.Worker.IntegrationTests` med Testcontainers Postgres
- 6 tester med `[Trait("Category", "SmokeTest")]` — körs via `dotnet test --filter "Category=SmokeTest"`
- Verifierar happy path + 5 negativa fall (Draft/Recent/SoftDeleted/Terminal/PerAppThreshold)
- Verifierar audit-paritet: `entry.UserId.ShouldBeNull()` för system-jobb

### KRITISK pipeline-bug fångad (Fas 9.7)

Min Fas 9.6-refaktor flyttade pipeline-config till delad konstant `MediatorPipelineBehaviors.InOrder` och satte `options.PipelineBehaviors = MediatorPipelineBehaviors.InOrder` i båda composition roots. **Mediator.SourceGenerator 3.0.2 läser INTE `options.PipelineBehaviors` från fält-references vid compile-time** — den behöver inline-array-literal. Resultat: pipeline-behaviors registrerades aldrig i DI. UoW-SaveChanges anropades aldrig. **Audit-rader skrevs inte.**

Hade detta landat utan integration smoke-test hade STEG 8:s audit-funktionalitet tyst brutits för Api också (regression-bug i alla audit-skrivningar).

**Fix:** `AddMediatorPipelineBehaviors()`-extension i `JobbPilot.Application.Common.MediatorPipelineBehaviors`. Registrerar behaviors som **open-generic DI-services** (`services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))`). Mediator runtime hämtar dem via `GetServices<IPipelineBehavior<TRequest, TResponse>>()`. Robust runtime-pattern.

**Regression-skydd dual-coverage:**
- Architecture test `MediatorPipeline_should_have_expected_behaviors_in_order` (kompil-tid)
- Integration smoke-test `RunAsync_StaleSubmittedApplication_TransitionsToGhostedAndWritesAuditEntry` (kör-tid)

### Reviews genomförda

- **dotnet-architect** (innan kod): 2 kritiska + 5 viktiga + 8 frågor besvarade. Alla åtgärdade. Rapport: `docs/reviews/2026-05-08-steg9-dotnet-architect.md`.
- **code-reviewer:** Approved with Minors. 0 Blocker, 0 Major, 7 Minor (alla deferable). Rapport: `docs/reviews/2026-05-08-steg9-code-reviewer.md`.
- **security-auditor:** Approved. 0 Critical, 2 Major (pre-prod-deploy, inte commit), 4 Minor. Rapport: `docs/reviews/2026-05-08-steg9-security-auditor.md`.

### Klas:s tre tillägg (alla implementerade)

1. **Backfill `NOW()`** istället för `updated_at` — befintliga apps får 21-dagars-fönster räknat från migrationsdatum. Skyddar mot mass-batch-ghosting vid första cron.
2. **Snäv `{Submitted, Acknowledged}` för Fas 1.** Definition of stale dokumenterad i ADR 0023. TD-18 spårar utökning till intervju-states.
3. **Integration smoke-test** med Testcontainers istället för manuellt smoke-test. Bevisade sitt värde — fångade pipeline-bug.

### Tech-debt-status efter STEG 9

- ~~**TD-9** stängd i STEG 8~~
- **TD-13** (PII-encryption Fas 2) — kvarstår
- **TD-14** (DeleteResumeVersion VersionInUse-check Fas 4) — kvarstår
- **TD-15** (Resume-formulär aria-invalid per fält Fas 1 a11y-pass) — kvarstår
- **TD-16** (Audit-log retention + Art. 17-anonymisering) — kvarstår, **nu unblockerad** (Hangfire finns)
- **TD-17 ny** — Hangfire prod-härdning (5 punkter, blocker för Fas 1 prod-deploy)
- **TD-18 ny** — Stale-detektering: utökning till intervju-states (vid första rapporterade fall)
- **TD-19 ny** — Worker orchestrator + DI-pattern: defense-in-depth-förbättringar (Fas 2)

## Senaste commits

| SHA | Beskrivning |
|-----|-------------|
| (pending) | docs: STEG 9 docs-sync (ADR 0023 + tech-debt + steg-tracker + current-work + session-logg) |
| (pending) | feat(worker): STEG 9 — Worker-pipeline + Hangfire + DetectGhostedApplicationsJob (ADR 0023) |
| 35efdf2 | docs: STEG 8 docs-sync (current-work + steg-tracker + tech-debt + session-logg) |
| 8df61a9 | feat(auditing): STEG 8 — audit log-infrastruktur (ADR 0022, stänger TD-9) |
| 1cb2926 | docs(claude): förtydliga §1.5 — docs-sync efter varje STEG, inte bara session-end |

## Tester totalt

- **Backend:** 451 (157 Domain + 169 Application + 15 Architecture + 104 Api Integration + 6 Worker SmokeTest) — +32 sedan STEG 8
- **Frontend:** 65 Vitest + 19 Playwright E2E (oförändrat)

## När nästa session startar

1. Kör `git log --oneline -10` — verifiera HEAD
2. Verifiera backend-tester: kör test-exen direkt under `tests/*/bin/Debug/net10.0/` (`dotnet test` på solution-nivå är trasigt)
3. För Worker SmokeTest: `tests/JobbPilot.Worker.IntegrationTests/bin/Debug/net10.0/JobbPilot.Worker.IntegrationTests.exe -trait "Category=SmokeTest"`
4. Läs `docs/steg-tracker.md` §6 för STEG 10-kandidater
5. Läs senaste session-logg (STEG 9) för detaljer
6. Läs ADR 0023 för Worker/Hangfire-arkitektur

## Kända begränsningar / quirks

- **postgres-dev** på port **5435** — `appsettings.Local.json` med rätt port + `.env`-lösenord
- **`dotnet ef`** plockar inte upp `appsettings.Local.json` — använd `export ConnectionStrings__Postgres=...`
- **`dotnet test`** på solution-nivå returnerar "Zero tests ran" (xunit.v3.mtp-v2-issue) — kör test-exen direkt
- **API kräver `ASPNETCORE_ENVIRONMENT=Development`** för Redis-connstring
- **Audit-tabellen växer obegränsat** i dev — TD-16 dokumenterar retention-jobb (nu unblockerad)
- **Hangfire-schema** skapas automatiskt vid Worker-start i dev (`PrepareSchemaIfNecessary=true`) — TD-17 dokumenterar prod-härdning
- **Mediator-pipeline-config:** ALLTID via `AddMediatorPipelineBehaviors()` — `options.PipelineBehaviors`-fält fungerar INTE med Mediator.SourceGenerator 3.0.2 från fält-references
- **Worker integration smoke-test** kräver Docker-Compose uppe + tar ~7-10 sekunder per körning (Testcontainers startar ny Postgres)
- **Middleware-deprecation-varning** i Next.js (kvar från STEG 6)

## Open follow-ups

Per ADR 0023:
- TD-17 (Hangfire prod-härdning) — innan Fas 1 prod-deploy
- TD-18 (intervju-states-utökning) — vid första rapporterade fall
- TD-19 (Worker defense-in-depth) — Fas 2 när Worker-jobb-yta växer

Per CLAUDE.md §1.5: docs-sync efter varje STEG (inte bara session-end). Glöm inte session-logg.
