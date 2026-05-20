---
session: F6 P4 sök-infrastruktur-fix (perf-bugg + filter-bugg-rotorsak)
datum: 2026-05-20
slug: f6-p4-sok-infrastruktur-fix
status: BACKEND LEVERERAD — pending Klas tag-push + deploy + manuell snapshot-backfill
commits:
  - "<P1: F6P4a migration — pg_trgm + GIN trigram>"
  - "<P2: JobTechHit POCO + sub-types + 6 nya tester>"
  - "<docs(adr): 0061 — sök-perf-strategi (GIN trigram, Approach A)>"
  - "<docs(sessions): F6 P4 sök-infrastruktur-fix>"
adrs:
  - "0061 (Accepted) — Sök-perf-strategi: GIN trigram-index för q-substring-match"
---

# F6 P4 sök-infrastruktur-fix — backend-leverans

**Mål:** Klas-direktiv 2026-05-20 — två problem i samma batch (BLOCKERAR F6 P4b SavedJobAds):

1. **P1 perf-bugg:** `GET /api/v1/job-ads?q=*` 40-52s på dev (52k rader)
2. **P2 filter-bugg-discovery:** ssyk/region/yrken "fungerar inte" — root-cause: BE/data eller FE?

## Discovery (CC, 2026-05-20)

### Perf-baseline verifierad
Autentiserad mot `dev.jobbpilot.se` (session via dev-test-creds):

| Query | Time | Items |
|-------|------|-------|
| `?page=1&pageSize=3` (no filter) | 2.2s | totalCount=51,749 |
| `?q=systemutvecklare&pageSize=5` | **40.4s** | 22KB svar (riktiga träffar) |
| `?ssyk=Z8ts_J5y_4ZJ&pageSize=5` | 0.6s | **0 items** (filter-bugg) |
| `?ssyk=CifL_Rzy_Mku` (Stockholms län-id) | 0.6s | **0 items** |
| `?ssyk=soBq_ia8_xcx` (Administratör) | 0.6s | **0 items** |
| `?ssyk=QQ23_iVQ_Kzw` (AML-Specialist) | 0.6s | **0 items** |

### Filter-bugg-rotorsak FUNNEN — backend
3 picker-conceptIds från `/api/v1/job-ads/taxonomy` testade → alla returnerar 0 träffar. Inspektion av `src/JobbPilot.Infrastructure/JobSources/Platsbanken/JobTechSearchResponse.cs`-POCO:n visade att `JobTechHit` saknade `Occupation`, `OccupationGroup`, `OccupationField`, `WorkplaceAddress` som deserialiserings-properties. `JsonSerializer.Serialize(hit)` i `PlatsbankenJobSource.cs:184` producerade payload utan klassifikations-keys → generated columns (`raw_payload->'occupation'->>'concept_id'` + `raw_payload->'workplace_address'->>'region_concept_id'`) gav NULL för **alla 51,749 rader** → ssyk/region-filter alltid 0 träffar.

Sanitizer-allowlist (`JobTechPayloadSanitizer.cs:42-50`) hade redan alla keys allowlistade — buggen var inte i sanitizer utan upstream i wire-format-POCO:n.

## Multi-approach via senior-cto-advisor (2026-05-20)

CC presenterade A/B/C/D + full discovery, CTO valde:

- **P1 = Approach A (GIN trigram-index på `lower(title)` + `lower(description)`).** Migration-only. INGEN Application-ändring. Bevarar Clean Arch-precedensen från ADR 0042 Beslut D + JobAdSearch.cs rad 19-22-kommentaren ("`EF.Functions.ILike` ligger i Npgsql-extension → Application-Clean-Arch-brott").
- **P2 = JobTechHit POCO-utvidgning** + manuell `SyncPlatsbankenSnapshotJob`-backfill för existerande 51k rader.
- **ADR 0060 Beslut 4 (N+1 YAGNI):** behåll **betingat**. Antagandet "20×<2s = OK" återställs när q-COUNT faller. ADR 0045 fitness function är rätt evolution-trigger om budget fortfarande bryts post-A-deploy.
- **ADR 0061:** NY (sök-perf-strategi). Implementations-dom för ADR 0042 Beslut D skala-trigger.
- **Scope:** P1 + P2 samma batch (båda Fas 1, samma touch, blockerar P4b). **Splittade i tre commits** för granskningstrail (Fowler 2018 atomic commits): P1 migration, P2 POCO+tests, ADR 0061.

Avvisade alternativ (B FTS, C cache-only, D hybrid) motiverade mot principer i CTO-rapport + ADR 0061 Beslut 2.

## dotnet-architect-design (mekanik-verifiering 2026-05-20)

- Migration: skippa `CONCURRENTLY` (EF Core transaktion-wrapping; dev-volym 51k acceptabelt). Functional `gin (lower(col) gin_trgm_ops)` matchar EXAKT `Col.ToLower()`-LIKE i LINQ. Partial-filter `WHERE status='Active' AND deleted_at IS NULL` speglar query-predikatet. Filnamn `F6P4aJobAdTrigramIndexes`. Down dropp:ar bara index (extension behålls).
- POCO: top-level `Occupation`/`OccupationGroup`/`OccupationField`/`WorkplaceAddress` (per JobTech v2-schema). Alla `internal sealed`, snake_case `JsonPropertyName`. Sanitizer-allowlist redan kompatibel.

## Implementation

### P1 — Migration (commit 1)

`src/JobbPilot.Infrastructure/Persistence/Migrations/20260520212725_F6P4aJobAdTrigramIndexes.cs`:

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX ix_job_ads_title_lower_trgm
  ON job_ads USING gin (lower(title) gin_trgm_ops)
  WHERE status = 'Active' AND deleted_at IS NULL;

CREATE INDEX ix_job_ads_description_lower_trgm
  ON job_ads USING gin (lower(description) gin_trgm_ops)
  WHERE status = 'Active' AND deleted_at IS NULL;
```

Down dropp:ar bara index (`pg_trgm`-extension idempotent additive). Migration genererad via db-migration-writer; `AppDbContextModelSnapshot.cs` oförändrad (raw SQL, ingen model-DSL).

### P2 — JobTechHit POCO (commit 2)

`src/JobbPilot.Infrastructure/JobSources/Platsbanken/JobTechSearchResponse.cs` utökad:

- 4 nya properties på `JobTechHit`: `Occupation`, `OccupationGroup`, `OccupationField`, `WorkplaceAddress`
- 4 nya `internal sealed class`-sub-typer (`JobTechOccupation`, `JobTechOccupationGroup`, `JobTechOccupationField`, `JobTechWorkplaceAddress`) med snake_case `JsonPropertyName`-attribut

`tests/JobbPilot.Application.UnitTests/JobAds/Infrastructure/JobTechHitDeserializationTests.cs` — 6 nya tester:
1. `Deserialize_PopulatesOccupationConceptId`
2. `Deserialize_PopulatesWorkplaceAddressRegionConceptId`
3. `Deserialize_PopulatesOccupationGroupAndField`
4. `Deserialize_GracefullyHandlesMissingClassification`
5. `RoundTripSerialize_PreservesClassificationJsonPaths` — verifierar att `JsonSerializer.Serialize(hit)` producerar exakt de JSON-paths som generated columns konsumerar
6. `RoundTripThroughSanitizer_PreservesClassificationForGeneratedColumns` — end-to-end via `JobTechPayloadSanitizer.SanitizeForStorage`

### Commit 3 — ADR 0061

`docs/decisions/0061-job-ad-search-perf-strategy.md` + README.md-rad. Skriven av adr-keeper på CC-utkast. Status Accepted (Klas-direktiv i startprompten).

## Reviews (alla gröna)

- **code-reviewer:** 0 Block / 0 Major / 0 Minor. "Mergeklar."
- **security-auditor:** 0 Block / 0 Critical / 0 High / 0 Medium / 0 Low. PII-neutral, GDPR Art. 5(1)(c) data-minimering uppfylld, partial-filter respekterar Art. 17 erasure-flöde, ingen ny logging-yta.

## Tester (alla gröna)

| Svit | Total | Status |
|------|-------|--------|
| Domain.UnitTests | 399 | ✓ |
| Application.UnitTests | 532 (526+6 nya) | ✓ |
| Architecture.Tests | 70 | ✓ |
| Api.IntegrationTests / ListJobAdsFilterTests | 13 | ✓ |
| Api.IntegrationTests / ListJobAdsMultiFilterTests | 6 | ✓ |

Architecture-grindar (`JobSourceLayerTests.JobTech_wire_types_are_internal_to_Infrastructure`) gröna — alla nya sub-POCOs är `internal sealed`.

## Pending Klas-aktion (post-leverans)

1. **Tag-push `v0.2.51-dev`** → GO?
2. **Dev-deploy:** migration applieras automatiskt via Migrate task. Verifiera `/api/ready` 200 + spot-check `EXPLAIN ANALYZE` på q-search (förvänta Bitmap Index Scan).
3. **Manuell `SyncPlatsbankenSnapshotJob`-trigger** via dev-Hangfire-UI för backfill av existerande 51k rader (ny POCO populerar `raw_payload`-klassifikation → generated columns fylls).
4. **Post-deploy verifierings-checklist:**
   - `GET /api/v1/job-ads?q=systemutvecklare&pageSize=5` → förvänta <2s (mål <200ms, från 40s)
   - `GET /api/v1/job-ads?ssyk=fg7B_yov_smw&pageSize=5` (Systemutvecklare/Programmerare) → förvänta totalCount>0 efter backfill
   - `GET /api/v1/job-ads?region=CifL_Rzy_Mku` (Stockholms län) → förvänta totalCount>0 efter backfill
   - `GET /api/v1/me/recent-searches` med q-rader → förvänta <10s (från 57s/504)

## F6 P4b SavedJobAds — status

**Avblockerad** efter Klas-GO för denna prompts deploy + verifiering. F6 P4b körs som separat backend-prompt enligt ursprunglig Klas-plan.

## Disciplin-noter

- senior-cto-advisor invokerades INNAN CC presenterade egen rekommendation (memory `feedback_cto_decides_multi_approach`).
- dotnet-architect invokerades INNAN kod (CLAUDE.md §9.2).
- code-reviewer + security-auditor INNAN commit (samma).
- Inga TDs lyfta — alla fynd in-block per §9.6 fas-regeln.
- ADR-prosa CC-skriven via adr-keeper på explicit Klas-direktiv i startprompten (override av §9.4 webb-Claude-verbatim per memory `feedback_klas_can_override_adr_verbatim_source`).
- Ingen FE-implementation (CLAUDE.md-förbud i prompt) — filter-bugg visade sig BE-fixbar.
- Pre-existing oparsade ändringar (`.claude/settings.json`, `docs/jobbpilot-v3-bundle/`, `docs/reviews/2026-05-17-agent-roster-gap-cto.md`) RÖRDA EJ.
