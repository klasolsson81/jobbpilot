---
session: "2026-05-08 — STEG 9: Worker-pipeline + Hangfire-infrastruktur (ADR 0023)"
datum: 2026-05-08
slug: steg9-worker-hangfire
status: KLAR
commits:
  - sha: (pending)
    msg: "feat(worker): STEG 9 — Worker-pipeline + Hangfire + DetectGhostedApplicationsJob (ADR 0023)"
  - sha: (pending)
    msg: "docs: STEG 9 docs-sync (ADR 0023 + tech-debt + steg-tracker + current-work + session-logg)"
---

## Mål för sessionen

STEG 9 — Worker-pipeline-aktivering + Hangfire-infrastruktur. Förskott från Fas 2-3. Klas valde Alt A (av A/B/C-kandidaterna i steg-tracker §6) eftersom Alt A är unblocker för både Alt C (TD-16 retention-jobb) och Fas 2 JobTech-sync + Fas 4 AI-jobb.

## Vad som genomfördes

### Plan-design + ADR via CC + dotnet-architect (utan webb-Claude)

Klas valde plan-design via CC + dotnet-architect-validering. Mönster från STEG 7+8 fungerade här när scope var väl-avgränsad infrastruktur med ADR 0022 som delvis spec.

CC drog initial plan, fick design-validering från dotnet-architect med 2 kritiska + 5 viktiga + 8 frågor besvarade:

**Kritiska (åtgärdade):**
- K1: WorkerCorrelationIdProvider impl ska cacha i instans-fält (inte HTTP-versionens fallback)
- K2: AddHttpAuditing får aldrig laddas i Worker (explicit dokumentation + arch-test)

**Viktiga (åtgärdade):**
- V3: IsStale flyttas från Domain-aggregat till Specification i Application-lagret
- V4: LastStatusChangeAt STRIKT-flippas i Create/TransitionTo/MarkGhosted, INTE AddNote/AddFollowUp/SoftDelete
- V5: Orchestrator AsNoTracking + cancel-token-check + progress-log var 25:e
- V6: Hangfire schema-isolering (`SchemaName="hangfire"`)
- V7: Cron-zon UTC dokumenterad

**Klas:s tre tillägg efter arch-rapport:**
1. Backfill `last_status_change_at = NOW()` istället för `updated_at` (skydd mot mass-batch-ghosting)
2. Snäv `{Submitted, Acknowledged}` för Fas 1 (definition of stale dokumenterad i ADR 0023)
3. Integration smoke-test med Testcontainers istället för manuellt smoke-test — bevisade sitt värde, fångade pipeline-bug

### Domain (Fas 9.1)

`src/JobbPilot.Domain/Applications/Application.cs`:
- Två nya properties: `LastStatusChangeAt` (DateTimeOffset, NOT NULL) + `GhostedThresholdDays` (int, NOT NULL, default 21)
- Per BUILD.md §schema rad 715–727: per Application (INTE per JobSeeker som steg-tracker tidigare sade — spec-drift fångad och rättad)
- STRIKT-flippning i `Create()` (initialiserar till now), `TransitionTo()` (på lyckad övergång), `MarkGhosted()` (innanför if-grenen så idempotent-fall inte rör fältet)
- Ingen `SetGhostedThresholdDays`-metod i Fas 1 (Q2 — deferred till Fas 3 när per-app-override-feature får UI)

**Tester (test-writer agent):** 9 nya unit-tester verifierar invariant + flippning + non-flippning per V4.

### Migration (Fas 9.2 — db-migration-writer agent)

`src/JobbPilot.Infrastructure/Persistence/Migrations/20260508093139_AddApplicationStaleDetectionFields.cs`:
- `last_status_change_at` (timestamptz, NOT NULL) — backfill via `Sql("UPDATE applications SET last_status_change_at = NOW() WHERE last_status_change_at IS NULL")` mellan AddColumn(nullable=true) och AlterColumn(nullable=false)
- `ghosted_threshold_days` (int, NOT NULL, default 21) — direkt med default-värde
- Partial index `ix_applications_stale_detection` ON `(last_status_change_at)` WHERE `status IN ('Submitted', 'Acknowledged') AND deleted_at IS NULL` — minimal index-storlek + index-only scan
- 44/44 befintliga apps backfillade mot dev-DB

`src/JobbPilot.Infrastructure/Persistence/Configurations/ApplicationConfiguration.cs` — uppdaterad med EF-mapping för båda nya properties.

### Infrastructure DI-modulär refaktor (Fas 9.3)

`src/JobbPilot.Infrastructure/DependencyInjection.cs` splittrad i tre extensions:
- `AddPersistence(configuration)` — DbContext, IAppDbContext, IDateTimeProvider (utan HTTP-bagage, utan Identity, utan Redis)
- `AddIdentityAndSessions(configuration)` — Identity, Redis, JWT-rester, ICurrentUser via HTTP, IAuthAuditLogger (HTTP-only)
- `AddHttpAuditing()` — CorrelationIdProvider, RequestContextProvider (HTTP-only — Worker registrerar egna stubs)
- `AddInfrastructure(configuration)` blir composition som anropar alla tre — Api orörd

XML-doc varnar explicit: "HTTP-only. Worker registrerar egna implementationer."

### Application — orchestrator + spec (Fas 9.4)

`src/JobbPilot.Application/Applications/Specifications/StaleApplicationSpecification.cs` (V3 från arch-rapport):
- `CandidateStatusFilter()` — `Expression<Func<Application, bool>>` (Status ∈ {Submitted, Acknowledged}) — EF-översättbart, utnyttjar partial-index
- `IsStaleNow(lastStatusChangeAt, ghostedThresholdDays, now)` — client-side stale-check (per-app-threshold)

**Notera:** Initial Plan A var att uttrycka hela predikatet i en enda `Expression<Func<>>` med `LastStatusChangeAt.AddDays(GhostedThresholdDays) < now`. EF Core 10 / Npgsql 10 översätter inte tillförlitligt `DateTimeOffset.AddDays(int kolumn)`. Plan B (tvådelat predikat) är acceptabel för Fas 1-volym (50–100/dag) — först SQL-snävning via Status-filter över växande tabell, sedan client-side filter över litet kandidat-set.

`src/JobbPilot.Application/Applications/Jobs/GhostedDetection/DetectGhostedApplicationsJob.cs` — orchestrator:
- Konstruktor: IAppDbContext + IMediator + IDateTimeProvider + ILogger
- `RunAsync(ct)`: AsNoTracking-query, client-side filter, per-id `mediator.Send(new MarkGhostedCommand(id))`-loop
- Cancel-token-check + progress-log var 25:e (V5)
- LoggerMessage-source-generator för alla log-anrop (CA1848-konformt)

**Tester:** 12 nya unit-tester med InMemory-DbContext + NSubstitute IMediator. Testar happy-path + 6 negativa fall + per-app-threshold + multiple-stale + cancel-token.

### Worker-aktivering (Fas 9.5)

**Hangfire-paket via Directory.Packages.props:**
- Hangfire.Core 1.8.23
- Hangfire.AspNetCore 1.8.23 (paketnamn vilseledande — krävs för IServiceScopeFactory-baserad JobActivator även i Generic Host)
- Hangfire.PostgreSql 1.21.1 (kompatibel med Npgsql 10.x — verifierat via web-search 2026-02 release-notes)

**CVE-fix:** Newtonsoft.Json 13.0.3 transitiv pinning. Hangfire drar in vulnerable 11.0.1 (CVE-2024-21907 / GHSA-5crp-9r3c-p9vr — DoS via StackOverflow vid djupt nestade JSON). `CentralPackageTransitivePinningEnabled=true` (redan aktivt i Directory.Build.props) propagerar pinningen utan att .csproj-filer rörs.

**Worker-stubs i `JobbPilot.Worker/Auditing/`:**
- `WorkerSystemUser` (Singleton, ICurrentUser): UserId=null, IsAuthenticated=false (system-jobb)
- `WorkerCorrelationIdProvider` (Scoped, ICorrelationIdProvider): instans-fält Guid → en correlation-ID per Hangfire-job-execution. K1-fix: HTTP-versionens `Guid.NewGuid()`-per-anrop-fallback skulle ha brutit korrelation-linkning.
- `WorkerRequestContextProvider` (Scoped, IRequestContextProvider): null IP/UA — system-jobb har inget HTTP-request

**`JobbPilot.Worker/Hosting/RecurringJobRegistrar.cs`** (per Q7 från arch-rapport): IHostedService som registrerar `detect-ghosted` cron daily 03:00 UTC via `AddOrUpdate` (idempotent).

**`Worker/Program.cs`** full refaktor:
```
AddPersistence + AddApplication + Worker-stubs + AddMediator
+ AddMediatorPipelineBehaviors (pipeline-fix)
+ AddHangfire (UseRecommendedSerializerSettings, SchemaName="hangfire", PrepareSchemaIfNecessary=true)
+ AddHangfireServer (WorkerCount=4)
+ AddHostedService<RecurringJobRegistrar>
```

**`Worker.cs` heartbeat-stub TAS BORT** — ersatt av Hangfire-server.

### KRITISK pipeline-bug fångad och fixad (under Fas 9.7)

**Symptom:** Smoke-test `RunAsync_StaleSubmittedApplication_TransitionsToGhostedAndWritesAuditEntry` failade. Status flippade inte. `mediator.Send(new MarkGhostedCommand(...))` returnerade `success=True` men handler kördes aldrig effektivt.

**Diagnostik:**
- Direkt-mediator-test: success=True, ingen status-flip, **0 audit-rader**
- Manuellt handler-anrop (utan Mediator) + manuell SaveChanges: status flippade till Ghosted ✓
- Spec-query returnerade 1 candidate korrekt
- IsStaleNow returnerade true korrekt
- Generated Mediator.g.cs visade att `pipelineBehaviours = mediator.Services.GetServices<IPipelineBehavior<...>>()` — DI-resolved
- DI-query för `IPipelineBehavior<MarkGhostedCommand, Result>` returnerade **0 behaviors**

**Root cause:** Min Fas 9.6-refaktor flyttade pipeline-config till delad konstant `MediatorPipelineBehaviors.InOrder` och satte `options.PipelineBehaviors = MediatorPipelineBehaviors.InOrder` i båda composition roots. **Mediator.SourceGenerator 3.0.2 läser inte `options.PipelineBehaviors` från fält-references vid compile-time** — den behöver inline-array-literal för att kunna analyseras. Behaviors registrerades aldrig i DI. UoW-SaveChanges anropades aldrig. Audit-rader skrevs inte.

Hade detta landat utan integration smoke-test hade STEG 8:s audit-funktionalitet tyst brutits för Api också (regression-bug i alla audit-skrivningar). Klas tillägg #3 (integration smoke-test framför manuellt) bevisade sitt värde.

**Fix:** Ny `AddMediatorPipelineBehaviors()`-extension i `JobbPilot.Application.Common.MediatorPipelineBehaviors`. Registrerar behaviors som **open-generic DI-services** (`services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))`). Mediator runtime hämtar dem via `GetServices<IPipelineBehavior<TRequest, TResponse>>()`. Robust runtime-pattern. Anropas explicit av båda Api/Program.cs och Worker/Program.cs efter `AddMediator(...)`.

**Regression-skydd dual-coverage:**
- Architecture test `MediatorPipeline_should_have_expected_behaviors_in_order` cementerar array-ordningen
- Integration smoke-test `RunAsync_StaleSubmittedApplication_TransitionsToGhostedAndWritesAuditEntry` verifierar att audit-rad faktiskt skrivs

### Architecture-tester (Fas 9.6)

`tests/JobbPilot.Architecture.Tests/WorkerLayerTests.cs` — 5 nya tester:
1. `Worker_should_not_depend_on_AspNetCore_Http_or_Identity` (K2)
2. `DetectGhostedApplicationsJob_should_reside_in_Application_layer`
3. `Worker_audit_stubs_should_implement_application_ports`
4. `RecurringJobRegistrar_should_reside_in_Worker_assembly`
5. `MediatorPipeline_should_have_expected_behaviors_in_order` (regression-skydd för pipeline-bug)

### Worker integration smoke-test (Fas 9.7)

`tests/JobbPilot.Worker.IntegrationTests/` — nytt projekt med Testcontainers Postgres:
- `WorkerTestFixture.cs` — speglar Worker/Program.cs DI-yta (utan Hangfire-server)
- `DetectGhostedApplicationsJobIntegrationTests.cs` — 6 tester med `[Trait("Category", "SmokeTest")]`
- Verifierar happy-path + Draft/Recent/SoftDeleted/Terminal/PerAppThreshold-negativa fall
- Verifierar audit-paritet: `entry.UserId.ShouldBeNull()`, `entry.EventType == "Application.MarkedGhosted"`

Körkommando: `dotnet test --filter "Category=SmokeTest"` (eller test-exe `-trait "Category=SmokeTest"`).

### Reviews

**code-reviewer:** Approved with Minors. 0 Blocker, 0 Major, 7 Minor (alla deferable). Notable: regression-skydd för pipeline-bug rekommenderas (M1 + M5 + M6 i TD-19).

**security-auditor:** Approved. 0 Critical, 2 Major (pre-prod-deploy), 4 Minor.

Major-fynd som spåras till TD-17:
- MAJ-1: `PrepareSchemaIfNecessary=true` ska flippas till false i prod + schema-runbook
- MAJ-2: Hangfire dashboard får aldrig exponeras utan custom IDashboardAuthorizationFilter

Minor (alla TD-17/TD-19):
- MIN-1: Architecture-test utöka med fler AspNetCore.*-namespaces
- MIN-2: Worker-stub-utbyggnad när fler jobb läggs (Fas 2)
- MIN-3: Defensiv runbook-anteckning för "kalibrerings-fas" första 21 dagar
- MIN-4: Splittra ConnectionStrings för least-privilege Hangfire-DB-user

### ADR 0023

Skriven via adr-keeper-agent: `docs/decisions/0023-worker-pipeline-hangfire.md`. README-index uppdaterat. Stänger ADR 0010:s "aktiveras Fas 1"-stub. Kompletterar ADR 0008 + ADR 0022 med pipeline-bug-fix.

### Test-status efter STEG 9

| Suite | Resultat | Diff från STEG 8 |
|-------|----------|------------------|
| Domain.UnitTests | 157/157 ✓ | +9 |
| Application.UnitTests | 169/169 ✓ | +12 |
| Architecture.Tests | 15/15 ✓ | +5 |
| Worker.IntegrationTests (SmokeTest) | 6/6 ✓ | +6 (ny) |
| Api.IntegrationTests | 104/104 ✓ | 0 (regression-skydd) |
| **Total backend** | **451** | **+32** |

Build: 9 projekt, 0 warnings, 0 errors.

### Tech-debt-status efter STEG 9

- TD-13, TD-14, TD-15 kvarstår (oförändrat)
- TD-16 (audit-retention) — kvarstår, **nu unblockerad** (Hangfire finns)
- **TD-17 ny** — Hangfire prod-härdning (MAJ-1, MAJ-2, MIN-3, MIN-4 + konfig-overlay). Blocker för Fas 1 prod-deploy.
- **TD-18 ny** — Stale-detektering: utökning till intervju-states (Klas tillägg #2 deferral). Trigger: första rapporterade fall.
- **TD-19 ny** — Worker orchestrator + DI defense-in-depth (M1, M5, M6, MIN-1). Adresseras vid Fas 2.

## Tekniska beslut

- **Plan-design via CC + dotnet-architect (utan webb-Claude)** — fungerade för väl-avgränsad infrastruktur när ADR 0022-spec fanns delvis färdig.
- **DI-modulär refaktor framför override-stubs** — SRP på composition-nivå. Worker drar inte HTTP-bagage. Sista-vinner-DI är arkitekturell lukt.
- **Worker-stub-impls i Worker-projektet** (inte Infrastructure) — HTTP-baserade impls hör till HTTP-context, Worker har eget kontext.
- **`AddMediatorPipelineBehaviors`-pattern** — open-generic DI-registrering är pålitligt runtime-pattern. `options.PipelineBehaviors`-fält fungerar bara med inline-array-literal i source generator-analys.
- **Per-id orchestrator-loop framför batch-command** — audit-paritet 1:1 med ADR 0022, isolering vid handler-failure.
- **Tvådelat StaleApplicationSpecification** — SQL-snävning via Status-filter (utnyttjar partial-index), client-side AddDays-check (Npgsql 10 inte tillförlitlig). Acceptabel för Fas 1-volym.
- **Backfill `NOW()` framför `updated_at`** (Klas tillägg #1) — skyddar mot mass-batch-ghosting vid första cron. Befintliga apps får 21-dagars-fönster räknat från migrationsdatum.
- **Definition of stale: snäv {Submitted, Acknowledged}** (Klas tillägg #2) — intervju-states är active oavsett kalendertid. TD-18 spårar utökning vid första fall.
- **Integration smoke-test framför manuellt** (Klas tillägg #3) — bevisade sitt värde, fångade pipeline-bug.
- **Hangfire schema-isolering (`hangfire`)** — ingen konflikt med JobbPilot-tabeller. PrepareSchemaIfNecessary defereras till prod-konfig (TD-17).
- **Newtonsoft.Json 13.0.3 transitiv pinning** — CVE-2024-21907-skydd via Central Package Management.

## Lärdomar

- **Plan-design-modell:** dotnet-architect agent fångade 7 punkter innan kod skrevs. Pipeline-bug fångades först under integration-test — arch-validering kan inte ersätta runtime-test för subtila source-generator-quirks.
- **Mediator.SourceGenerator API-quirks:** `options.PipelineBehaviors` är inte tillförlitlig från fält-references. Dokumenterat i ADR 0023 + XML-doc.
- **Spec-drift fångas tidigt:** "ghosted_threshold_days per JobSeeker" i steg-tracker rättat till "per Application" per BUILD.md §schema. Discovery (CLAUDE.md §9.4) fångade det.
- **Klas tillägg #3 var rätt instinkt:** integration smoke-test över manuellt fångade pipeline-bug. Mönster: alla nya orchestrator-/Worker-jobb ska ha integration smoke-test med `[Trait("Category", "SmokeTest")]`.

## Nästa session

STEG 10 kräver beslut. Fyra kandidater i `docs/steg-tracker.md` §6:

- **Alt B — Fas 0-stängning** (deploy till dev.jobbpilot.se, GitHub Actions, IAM-cleanup)
- **Alt C — TD-16-implementation** (audit-retention + Art. 17, nu unblockerad efter STEG 9)
- **Alt D — TD-17-implementation** (Hangfire prod-härdning, 5 punkter)
- **Alt E — Fortsätt features** (Fas 2 JobTech blockerad av ADR 0005, andra Fas 1-features)

Rekommendation: Alt C eller Alt D — båda stänger Fas 1 prod-deploy-blockare.

Förväntad HEAD efter STEG 9: nya commits för feature + docs.
