---
session: "2026-05-08 — STEG 10a: Audit-log retention via PG native partitioning + Hangfire-job (ADR 0024 D1+D2)"
datum: 2026-05-08
slug: steg10a-audit-retention
status: KLAR
commits:
  - sha: (pending)
    msg: "feat(auditing): STEG 10a — audit-log retention partitioning + Hangfire-job (ADR 0024 D1+D2)"
  - sha: (pending)
    msg: "docs: STEG 10a docs-sync (current-work + steg-tracker + tech-debt + session-logg)"
---

## Mål för sessionen

STEG 10 — TD-16-implementation. Stänger del 1 av två (audit-retention; Art. 17-cascade kommer i STEG 10b). Brett scope valdes initialt för att täcka hela TD-16 + DELETE /me-flödet, men STEG 10a leverar audit-retention som självstående unit. STEG 10b kvar.

## Vad som genomfördes

### Plan-design + ADR via webb-Claude

Klas designade ADR 0024 i webb-Claude efter att CC levererat discovery-paket (audit-state, soft-delete-yta, DELETE /me-saknad, Hangfire-pattern från STEG 9). ADR 0024 har 6 delbeslut:

1. Audit-retention via PG native partitioning per dag (vald över pg_partman/daily DELETE)
2. Migration via rename + reinsert (atomic, en transaction)
3. `IAuditTrailEraser`-port för Art. 17-cascade (10b)
4. `DeleteAccountCommand` som samlat Mediator-command (10b)
5. 30-dagars restore-fönster utan Identity-tabell-migration (10b)
6. `HardDeleteAccountsJob` med Steg 0 orphan-cleanup (10b)

STEG 10a implementerar D1 + D2. Tre justeringar applicerades på ADR 0024 efter granskning:
- D3 atomicitet-paragraf (`HardDeleteAccountsJob` ansvarar för transaction)
- D6 algoritm-omarbetning till Steg 0/1/2 + orphan-cleanup-loop  
- Konsekvens-bullet ändrad till "cross-context-gränsen" + ingen TD-genereras

Plus: D2 bootstrap-text uppdaterades från "senaste 7 dagar" till "idag + 6 framåt" efter min implementation och db-migration-writer-review.

### STEG 10.2 — Komposit-PK i AuditLogEntryConfiguration

`builder.HasKey(a => new { a.Id, a.OccurredAt })` — krav från native partitioning (partition-key måste ingå i PK). Ändring från ADR 0022-spec som implicit kompletteras.

Senare upptäckt: krävs explicit `.ValueGeneratedNever()` även på OccurredAt för att EF Core 10:s `PendingModelChangesWarning` inte ska kasta vid `Database.MigrateAsync()` i Testcontainers-bootstrap. Känd EF-quirk för komposit-PK med blandad value-generation-config (Id har ValueGeneratedNever via HasConversion, OccurredAt fick det inte automatiskt).

### STEG 10.3 — Partitions-migration

`20260508152351_AddAuditLogPartitioning.cs`:

Algoritm (Up):
1. Rename `audit_log` → `audit_log_legacy` + rename PK-constraint + index
2. Skapa partitionerad parent-tabell med komposit-PK
3. Skapa 7 bootstrap-partitions (idag + 6 framåt) **före** default-partition (PG-quirk-skydd)
4. Skapa default-partition
5. INSERT-SELECT med explicit kolumnlista (B1 från review)
6. DROP legacy + dess index
7. Återskapa index på parent (propageras till partitions automatiskt)

Down: symmetrisk reversering till icke-partitionerad tabell.

**db-migration-writer-review:** Block — 2 blockers (B1 SELECT *, M1 ADR-text-mismatch) + 3 viktiga. Alla blockers fixade. M1 löstes via ADR-textuppdatering (bootstrap-orientering förtydligad). M2 (default-first ordning) + m2/m3 (defensive IF EXISTS) också fixade.

**Två run-time-fynd vid första apply:**
1. PK-constraint-namn-kollision: `pk_audit_log` följde inte med tabell-rename, krockade med ny tabell. Fix: explicit `ALTER TABLE … RENAME CONSTRAINT pk_audit_log TO pk_audit_log_legacy`. Transaction rollbackades rent.
2. EF Core 10 PendingModelChangesWarning vid Migrate() trots model-snapshot-konsistens (se 10.2).

Schema verifierat mot dev-DB: 8 partitions (7 range + 1 default), komposit-PK, index propagerat.

### STEG 10.4 — IAuditPartitionMaintainer-port + impl + orchestrator

Tre nya filer:

- `src/JobbPilot.Application/Common/Auditing/IAuditPartitionMaintainer.cs` — port med `EnsureNextDayPartitionAsync` + `DropPartitionsOlderThanAsync`
- `src/JobbPilot.Infrastructure/Auditing/AuditPartitionMaintainer.cs` — PG-impl. Tar `AppDbContext` direkt (inte `IAppDbContext`) eftersom `Database`-facaden inte exponeras på interfacet (medvetet — raw SQL utanför Application-lagret).
- `src/JobbPilot.Application/Common/Auditing/Jobs/AuditLogRetention/AuditLogRetentionJob.cs` — orchestrator med `LoggerMessage`-source-gen logging. Constructor: `IAuditPartitionMaintainer + IDateTimeProvider + ILogger`. **Ingen IMediator** (medvetet — partition-DDL är ren ops, inte domain-mutation; ingen audit-rad skrivs av jobbet).

Designval (port-isolering matchar ADR 0024 D3 IAuditTrailEraser-mönstret):
- Lifetime Scoped (följer DbContext-livscykel)
- Registreras i `AddPersistence` (inte `AddHttpAuditing`) så Worker har tillgång
- Audit-bypass-disciplinen är arch-test-låst (10.7)

Idempotency: `CREATE TABLE IF NOT EXISTS … PARTITION OF` (PG18-stöd verifierat mot dev-DB innan implementation), `DROP TABLE IF EXISTS`.

Lexikografisk filtrering på partition-namn (`audit_log_YYYYMMDD`) ger datum-jämförelse via fixed-width-strängar. Default-partitionen filtreras bort av regex `^audit_log_[0-9]{8}$`.

### STEG 10.5 — Hangfire-registrering

`RecurringJobRegistrar.cs` utökad:

```csharp
manager.AddOrUpdate<AuditLogRetentionJob>("audit-log-retention", j => j.RunAsync(...), Cron.Daily(3));
```

Båda jobs (audit-log-retention + detect-ghosted) på 03:00 UTC. Skyddskommentar förklarar att samkörning är säker (atomisk DDL + olika tabeller). Sparar ADR 0023-uppdatering.

### STEG 10.6 — Smoke-tester

`tests/JobbPilot.Worker.IntegrationTests/Auditing/AuditLogRetentionJobIntegrationTests.cs` med 4 tester (alla `[Trait("Category", "SmokeTest")]`):

1. `EnsureNextDayPartition_CreatesPartitionWhenMissing`
2. `EnsureNextDayPartition_IsIdempotentWhenAlreadyExists`
3. `DropPartitionsOlderThan_DropsOldPartitionsSkipsDefaultAndRecent`
4. `RunAsync_EndToEnd_EnsuresNextDayAndDropsOld`

Testerna använder far-future datum (2030-tal) eller far-past (1900-tal) för att inte kollidera med bootstrap-partitions kring real-now.

**Bug fångad av smoke-test:** `SqlQueryRaw` med format-string-syntax tolkade `[0-9]{8}` som `{8}`-placeholder → FormatException. Fix: escape till `{{8}}`. Hade slipit igenom till prod utan smoke-test (kompilerade rent).

### STEG 10.7 — Architecture-tester för bypass-disciplin

`tests/JobbPilot.Architecture.Tests/AuditingLayerTests.cs` med 3 tester:

1. `IAuditPartitionMaintainer_in_Application_should_only_be_referenced_by_AuditLogRetentionJob`
2. `IAuditPartitionMaintainer_in_Infrastructure_should_only_be_referenced_by_impl_or_DI`
3. `IAuditPartitionMaintainer_should_not_be_referenced_directly_by_Worker`

Allow-list-pattern (framför reviewer-mönstrets `HaveNameMatching`) skalar till multipla tillåtna konsumenter och blir explicit i felmeddelandet.

### STEG 10.8 — Runbook

`docs/runbooks/audit-retention.md`:
- Översikt (jobb + default-partition som säkerhetsnät)
- Övervakning (Hangfire dashboard, Seq-loggar, partitions-state-query)
- Failure-scenarier (1 dag, ≥ 1 dag med default-partition-data, ≥ 7 dagar bootstrap-buffer förbrukad)
- Manuell move-procedur via PARTITION OF-skapning som triggar PG re-route
- Disaster recovery + GDPR-noter

## Reviews genomförda

- **db-migration-writer** (10.3): Block — 2 blockers + 3 viktiga. Alla fixade. Approved efter fix.
- **code-reviewer** (10.4–10.6 paketet): Approved with Minors. 0 Blocker, 0 Major, 3 Minor + 3 Nit. M1 + M3 + N1 fixade. **M2 defererad** som TD-20 (`SqlQuery<FormattableString>`-refactor) — försök gjort, bröt 2 tester pga EF projection-issue mot pg_class.relname-typ. N2 + N3 defererade som icke-issues för Fas 1.

## Klas:s tre tillägg

1. **Brett scope-val** — STEG 10 omfattar hela TD-16 + DELETE /me. STEG 10a leverar audit-retention; STEG 10b kvarstår.
2. **Webb-Claude för plan-design** — ADR 0024 designades i webb-Claude före implementation. CC implementerade mot färdig ADR (matchar STEG 8-mall).
3. **TD-20 dokumenterat** istället för att bara nämna det i session-logg — explicit spårning för defensiv refactor.

## Tech-debt-status efter STEG 10a

- ~~**TD-9** stängd i STEG 8~~
- **TD-13** (PII-encryption Fas 2) — kvarstår
- **TD-14** (DeleteResumeVersion Fas 4) — kvarstår
- **TD-15** (Resume-formulär a11y Fas 1) — kvarstår
- **TD-16** — **del 1 stängd via STEG 10a** (audit-retention via partitioning + 90-dagars rotation). **Del 2 (Art. 17-cascade) kvarstår för STEG 10b.**
- **TD-17** (Hangfire prod-härdning) — kvarstår
- **TD-18** (intervju-states-utökning) — kvarstår
- **TD-19** (Worker defense-in-depth Fas 2) — kvarstår
- **TD-20 ny** — `AuditPartitionMaintainer.DropPartitionsOlderThanAsync`: defensiv refactor till `SqlQuery<FormattableString>` (M2 från review, defererad)

## Tester totalt efter STEG 10a

- **Backend:** 458 (157 Domain + 169 Application + 18 Architecture + 104 Api Integration + 10 Worker SmokeTest) — +7 sedan STEG 9 (4 retention smoke + 3 auditing arch)
- **Frontend:** 65 Vitest + 19 Playwright E2E (oförändrat)

## Filer ändrade

**Nya:**
- `docs/decisions/0024-audit-retention-and-art17-cascade.md`
- `docs/runbooks/audit-retention.md`
- `src/JobbPilot.Application/Common/Auditing/IAuditPartitionMaintainer.cs`
- `src/JobbPilot.Application/Common/Auditing/Jobs/AuditLogRetention/AuditLogRetentionJob.cs`
- `src/JobbPilot.Infrastructure/Auditing/AuditPartitionMaintainer.cs`
- `src/JobbPilot.Infrastructure/Persistence/Migrations/20260508152351_AddAuditLogPartitioning.cs`
- `src/JobbPilot.Infrastructure/Persistence/Migrations/20260508152351_AddAuditLogPartitioning.Designer.cs`
- `tests/JobbPilot.Architecture.Tests/AuditingLayerTests.cs`
- `tests/JobbPilot.Worker.IntegrationTests/Auditing/AuditLogRetentionJobIntegrationTests.cs`

**Modifierade:**
- `src/JobbPilot.Infrastructure/DependencyInjection.cs` (port-registrering)
- `src/JobbPilot.Infrastructure/Persistence/Configurations/AuditLogEntryConfiguration.cs` (komposit-PK + ValueGeneratedNever på OccurredAt)
- `src/JobbPilot.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs` (auto)
- `src/JobbPilot.Worker/Hosting/RecurringJobRegistrar.cs` (Hangfire-registrering)

## Nästa session

STEG 10b — DELETE /me + Art. 17-cascade. Förutsättningar:

- ADR 0024 D3, D4, D5, D6 är designade
- Två öppna design-frågor som CC ska besluta:
  - `ISessionStore.InvalidateAllForUserAsync`-strategi (rek: secondary Redis-set)
  - `LoginCommandHandler`-blockering via JobSeeker.DeletedAt (rek: ny IAppDbContext-injektion)
- Ny port `IAuditTrailEraser` + ny migration eventuellt, ny endpoint, ny `HardDeleteAccountsJob`
- Stänger del 2 av TD-16

Uppskattning: ~10 filer (orchestrator + impl + handler + endpoint + arch-test + 2 smoke-tests + tester) — analog scope med STEG 10a men utan migration.
