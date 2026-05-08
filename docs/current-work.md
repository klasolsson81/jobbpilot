# Current work — JobbPilot

**Status:** STEG 10a KLAR. ADR 0024 (D1+D2) implementerad. Audit-retention via PG native partitioning + 90-dagars rotation. TD-16 del 1 stängd, TD-20 ny. Nästa: STEG 10b — DELETE /me + Art. 17-cascade (ADR 0024 D3+D4+D5+D6).
**Senast uppdaterad:** 2026-05-08
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu

**STEG 10a klar.** Audit-log retention via PostgreSQL native daily partitioning + Hangfire-jobb (ADR 0024 D1+D2). Stänger del 1 av TD-16 (Art. 5(1)(e) Storage Limitation). TD-16 del 2 (Art. 17-cascade) kvarstår för STEG 10b.

### STEG 10a — Audit-retention via partitioning (ADR 0024 D1+D2)

**Strategi (ADR 0024):** `audit_log` konverterad till partitionerad tabell (`PARTITION BY RANGE (occurred_at)`). Daglig Hangfire-jobb skapar morgondagens partition + droppar partitions > 90 dagar. Idempotent. Default-partition som säkerhetsnät. `IAuditPartitionMaintainer`-port isolerad till orchestrator (arch-test-låst).

**Komposit-PK (Fas 10.2):**
- `AuditLogEntryConfiguration`: `HasKey(a => new { a.Id, a.OccurredAt })` — partition-key krav från PG18 native partitioning
- Explicit `.ValueGeneratedNever()` på OccurredAt (krav för konsistens; saknad triggade EF Core 10 PendingModelChangesWarning vid Migrate)

**Migration (Fas 10.3):**
- `20260508152351_AddAuditLogPartitioning` applicerad mot dev-DB
- Up: rename → CREATE PARTITION BY RANGE → 7 bootstrap-partitions (idag + 6 framåt) → default → INSERT-SELECT (explicit kolumnlista) → DROP legacy → CREATE INDEX
- Down: symmetrisk reversering till icke-partitionerad tabell
- **Lärdomar fångade vid första apply:** PK-constraint följde inte med RENAME (krock — fix: `ALTER TABLE … RENAME CONSTRAINT`). Och EF Core 10-quirk om komposit-PK kräver `ValueGeneratedNever` på alla key-kolumner.

**Port + impl + orchestrator (Fas 10.4):**
- `IAuditPartitionMaintainer` i `Application/Common/Auditing/` — `EnsureNextDayPartitionAsync` + `DropPartitionsOlderThanAsync`
- `AuditPartitionMaintainer` i `Infrastructure/Auditing/` — tar `AppDbContext` direkt (Database-facaden inte exponerad på interface). Idempotent via `CREATE IF NOT EXISTS` + `DROP IF EXISTS`. Lexikografisk filtrering på `audit_log_YYYYMMDD`-namn.
- `AuditLogRetentionJob` i `Application/Common/Auditing/Jobs/AuditLogRetention/` — orchestrator. Constructor: port + clock + logger. **Ingen IMediator** (medvetet — partition-DDL är ren ops, ingen audit-rad skrivs av jobbet, undviker self-referential audit).
- DI-registrering i `AddPersistence` (Scoped, Worker-tillgänglig)

**Hangfire (Fas 10.5):**
- `RecurringJobRegistrar` utökad med `audit-log-retention` cron `Cron.Daily(3)` (03:00 UTC daily)
- Båda jobs (audit-retention + detect-ghosted) på 03:00 UTC. Skyddskommentar: PG hanterar atomisk DDL → ingen kontention.

**Smoke-test (Fas 10.6):**
- 4 nya tester med `[Trait("Category", "SmokeTest")]` mot Testcontainers Postgres
- **Bug fångad:** SqlQueryRaw + format-string-syntax tolkade `[0-9]{8}` som `{8}`-placeholder → FormatException. Fix: escape till `{{8}}`.

**Architecture-test (Fas 10.7):**
- Ny `AuditingLayerTests.cs` med 3 tester som låser `IAuditPartitionMaintainer`-konsumtion
- Allow-list-pattern (Application: orchestrator only; Infrastructure: impl + DI; Worker: ingen direct dep)

**Runbook (Fas 10.8):**
- `docs/runbooks/audit-retention.md` med övervakning, failure-scenarier, manuell move-procedur, GDPR-noter

### Reviews genomförda

- **db-migration-writer** (10.3): Block — 2 blockers + 3 viktiga. Alla fixade. Rapport sparad i agent-output.
- **code-reviewer** (10.4–10.6): Approved with Minors. 0 Blocker, 0 Major, 3 Minor + 3 Nit. M1 + M3 + N1 fixade. M2 defererad som TD-20 + N2/N3 icke-issues.

### Tech-debt-status efter STEG 10a

- ~~**TD-9** stängd i STEG 8~~
- **TD-13** (PII-encryption Fas 2) — kvarstår
- **TD-14** (DeleteResumeVersion VersionInUse-check Fas 4) — kvarstår
- **TD-15** (Resume-formulär aria-invalid Fas 1) — kvarstår
- **TD-16** — **del 1 (audit-retention) stängd via STEG 10a**. **Del 2 (Art. 17-cascade) kvar för STEG 10b.**
- **TD-17** (Hangfire prod-härdning, blocker för Fas 1 prod-deploy) — kvarstår
- **TD-18** (intervju-states-utökning) — kvarstår
- **TD-19** (Worker defense-in-depth Fas 2) — kvarstår
- **TD-20 ny** — `AuditPartitionMaintainer.DropPartitionsOlderThanAsync`: SqlQueryRaw + format-string-escape (defensiv refactor till `SqlQuery<FormattableString>`, defererad)

## Senaste commits

| SHA | Beskrivning |
|-----|-------------|
| (pending) | docs: STEG 10a docs-sync (current-work + steg-tracker + tech-debt + session-logg) |
| (pending) | feat(auditing): STEG 10a — audit-log retention partitioning + Hangfire-job (ADR 0024 D1+D2) |
| 8982213 | docs: STEG 9 docs-sync (ADR 0023 + tech-debt + steg-tracker + current-work + session-logg) |
| 152f047 | feat(worker): STEG 9 — Worker-pipeline + Hangfire + DetectGhostedApplicationsJob (ADR 0023) |
| 35efdf2 | docs: STEG 8 docs-sync (current-work + steg-tracker + tech-debt + session-logg) |
| 8df61a9 | feat(auditing): STEG 8 — audit log-infrastruktur (ADR 0022, stänger TD-9) |

## Tester totalt

- **Backend:** 458 (157 Domain + 169 Application + 18 Architecture + 104 Api Integration + 10 Worker SmokeTest) — +7 sedan STEG 9 (4 retention smoke + 3 auditing arch)
- **Frontend:** 65 Vitest + 19 Playwright E2E (oförändrat)

## När nästa session startar

1. Kör `git log --oneline -10` — verifiera HEAD
2. Verifiera backend-tester: kör test-exen direkt under `tests/*/bin/Debug/net10.0/` (`dotnet test` på solution-nivå är trasigt)
3. För Worker SmokeTest: `tests/JobbPilot.Worker.IntegrationTests/bin/Debug/net10.0/JobbPilot.Worker.IntegrationTests.exe -trait "Category=SmokeTest"`
4. Läs `docs/steg-tracker.md` §6 för STEG 10b-plan
5. Läs senaste session-logg (STEG 10a) för detaljer
6. Läs ADR 0024 — fokus på D3+D4+D5+D6 för STEG 10b

## Kända begränsningar / quirks

- **postgres-dev** på port **5435** — `appsettings.Local.json` med rätt port + `.env`-lösenord
- **`dotnet ef`** plockar inte upp `appsettings.Local.json` — använd `export ConnectionStrings__Postgres=...`
- **`dotnet test`** på solution-nivå returnerar "Zero tests ran" (xunit.v3.mtp-v2-issue) — kör test-exen direkt
- **API kräver `ASPNETCORE_ENVIRONMENT=Development`** för Redis-connstring
- **`audit_log` är nu partitionerad** — bootstrap-fönstret är 7 dagar framåt (idag + 6); retention-jobb skapar morgondagens partition dagligen 03:00 UTC. Default-partitionen fångar overflow.
- **Hangfire-schema** skapas automatiskt vid Worker-start i dev (`PrepareSchemaIfNecessary=true`) — TD-17 dokumenterar prod-härdning
- **Mediator-pipeline-config:** ALLTID via `AddMediatorPipelineBehaviors()` — `options.PipelineBehaviors`-fält fungerar INTE med Mediator.SourceGenerator 3.0.2 från fält-references
- **Komposit-PK på audit_log:** `(id, occurred_at)` per ADR 0024 D2. Komplett kompletterar ADR 0022:s schema-spec.
- **EF Core 10 PendingModelChangesWarning** kräver `.ValueGeneratedNever()` på alla komposit-PK-kolumner med blandad value-generation-config.
- **Worker integration smoke-test** kräver Docker-Compose uppe + tar ~7-10 sekunder per körning (Testcontainers startar ny Postgres)
- **Middleware-deprecation-varning** i Next.js (kvar från STEG 6)

## Open follow-ups

Per ADR 0024:
- TD-16 del 2 (Art. 17-cascade) — STEG 10b
- TD-17 (Hangfire prod-härdning) — innan Fas 1 prod-deploy
- TD-18 (intervju-states-utökning) — vid första rapporterade fall
- TD-19 (Worker defense-in-depth) — Fas 2 när Worker-jobb-yta växer
- TD-20 (SqlQuery<FormattableString>-refactor) — opportunistiskt vid touch på AuditPartitionMaintainer

Per CLAUDE.md §1.5: docs-sync efter varje STEG (inte bara session-end). Glöm inte session-logg.

## STEG 10b — design klar, implementation kvar

ADR 0024 D3 + D4 + D5 + D6 specar:
- `IAuditTrailEraser`-port + Infrastructure-impl (audit-bypass-pattern, direct SQL UPDATE)
- `DeleteAccountCommand` som samlat Mediator-command (cascade soft-delete)
- `DELETE /me`-endpoint i `MeEndpoints.cs`
- `LoginCommandHandler`-blockering vid `JobSeeker.DeletedAt is not null`
- `HardDeleteAccountsJob` med Steg 0 orphan-cleanup + Steg 1 hämta + Steg 2 hard-delete + Identity-DELETE separat boundary

Två öppna design-frågor som CC ska besluta inom ADR-ramen:
1. `ISessionStore.InvalidateAllForUserAsync` — bygg secondary Redis-set (rek) eller SCAN-fallback
2. `LoginCommandHandler`-blockering — ny `IAppDbContext`-injektion (rek) i auth-flödet
