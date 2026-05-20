---
session: F6 Prompt 4a BACKEND — RecentJobSearches auto-capture-domän
datum: 2026-05-20
slug: f6-p4a-recent-job-searches-backend
status: levererat
commits:
  - 5bc6eea (feat: RecentJobSearches auto-capture + ADR 0060)
tag: v0.2.49-dev
adr: 0060 (NY, Accepted); amend 0039 + 0055 + 0024
---

# F6 Prompt 4a BACKEND — RecentJobSearches auto-capture-domän

## Mål

Ny aggregate-domän `RecentJobSearches` för Platsbanken-paritet "Senaste sökningar"-affordance per HANDOVER-v3.md §0/§9. Auto-capture vid `GET /api/v1/job-ads` via post-handler-pipeline-behavior. Skild från SavedSearch (ADR 0039 manuell-spara) — separat semantik, cap=20/seeker, delta-räknare (NewCount via re-query).

Möjliggör F6 P4a FRONTEND (separat session efter merge): `/sokningar` lista + hero-chip-dropdown. F6 P4b SavedJobAds (per-annons-bookmark) = separat backend-prompt.

## Vad gjordes

### Discovery + design-rond

- Explore-agent: kartla SavedSearch-domän, ListJobAdsQuery-shape, pipeline-behavior-mönster (FieldEncryptionKeyPrefetchBehavior), text[]-pattern i ResumeConfiguration, ON CONFLICT-pattern i UpsertExternalJobAd, ITaxonomyReadModel-port, JobAdSortBy-enum, frontend DTO-mirror-konvention.
- senior-cto-advisor: 3 multi-approach-val entydigt avgjorda:
  - **FilterHash**: Variant A — SHA-256 av canonical-JSON i Domain. Avvisade B (plaintext) + C (PG generated column — bryter Clean Arch).
  - **Auto-capture**: Variant A — post-handler `IPipelineBehavior` med opt-in `ICapturesRecentSearch`-markör. Avvisade B (FE-driven, trust-flytt), C (inline i handler, SRP-brott), D (domain-event från query, bryter CQRS-konvention).
  - **CurrentCount**: Variant A — re-query per row + cap=20-invariant. Avvisade B (Hangfire-cache, stale-data UX-bugg) + C (single batch-SQL, heterogena filter ≠ GROUP BY).
  - Villkor: Klas-bekräftelse på cap=20 (auto-mode-accepterad enligt CTO-motivering; Klas kan redirect:a).
- dotnet-architect: Domain-shape + EF-konfig-skiss verifierad. 4 korrektioner mot CC-lutning:
  - F1a: Capture tar redan-validerad `SearchCriteria` som parameter (DRY, Evans 2003 kap. 5)
  - F1c: `MaxPerSeeker = 20`-konstant i Domain, enforce-mekanik i Capturer
  - Q2a: shadow `List<string>`-backing-field paritet ResumeConfiguration (Npgsql 10 auto-map för List<string>, inte IReadOnlyList<string>)
  - Q3a: single generic constraint + runtime markör-check (paritet FieldEncryptionKeyPrefetchBehavior-stilen)
  - Q3b: response-side markör `IRecentSearchCaptureResponse { int TotalCount }` på PagedResult
  - Q4c: `JobAdSearch.ApplyCriteria` är Application-static (CC angav fel som Domain), direkt anrop OK

### Implementation

**Domain** (`src/JobbPilot.Domain/RecentJobSearches/`):
- `RecentJobSearch` aggregate root (Capture/Bump, raisar 2 events, MaxPerSeeker=20-konstant)
- `RecentJobSearchId` (readonly record struct)
- `FilterHashCalculator` (SHA-256 over canonical-JSON av q+ssyk+region+sortBy)
- `RecentJobSearchCapturedDomainEvent` + `RecentJobSearchBumpedDomainEvent`

**Application** (`src/JobbPilot.Application/RecentJobSearches/`):
- `ICapturesRecentSearch` (opt-in markör; XML-doc med auth-invariant per security-auditor Medium-3)
- `IRecentSearchCaptureResponse` (TotalCount-extraktion utan open/closed-brott)
- `IRecentJobSearchCapturer` (port; Infrastructure-implementation)
- `RecentJobSearchCaptureBehavior` (post-handler pipeline; default-browse-guard + try/catch best-effort)
- `RecentJobSearchDto` (Q, Ssyk/Region + labels, SortBy, Label-server-härled, CurrentCount, NewCount, LastViewedAt)
- `ListRecentSearchesQuery` + Handler (avsiktlig N+1 cappad vid 20, label-resolve via ITaxonomyReadModel)
- `DeleteRecentSearchCommand` + Handler (cross-tenant-detection ADR 0031)
- `PagedResult<T>` impl `IRecentSearchCaptureResponse`
- `ListJobAdsQuery` impl `ICapturesRecentSearch`
- `MediatorPipelineBehaviors.InOrder` utökad: `UoW → RecentJobSearchCapture → Audit`
- `IAppDbContext` utökad: `DbSet<RecentJobSearch>`

**Infrastructure** (`src/JobbPilot.Infrastructure/`):
- `RecentJobSearchConfiguration` (text[] shadow-fields _ssyk/_region paritet ResumeConfiguration, UNIQUE(seeker, hash), idx seeker_viewed_at DESC)
- `RecentJobSearchCapturer` (JobSeeker-lookup, bump-existing-vs-Insert, evict äldsta vid cap-overflow, ON CONFLICT race-pattern ADR 0032 §5)
- DI-registrering Scoped
- Migration `20260520190720_AddRecentJobSearches`
- `AccountHardDeleter.HardDeleteAccountAsync` utökad med explicit `RemoveRange` för SavedSearches + RecentJobSearches (GDPR Art. 17-cascade)

**API** (`src/JobbPilot.Api/Endpoints/RecentSearchesEndpoints.cs`):
- `GET /api/v1/me/recent-searches` (auth-gated)
- `DELETE /api/v1/me/recent-searches/{id}` (auth-gated)
- Registrerad i Program.cs

**Frontend** (`web/jobbpilot-web/src/lib/`):
- `dto/recent-searches.ts` (Zod-schema + types; SortBy-ordinal-mapping via SAVED_SEARCH_SORT_ORDER)
- `dto/recent-searches.test.ts` (10 tester)
- `api/recent-searches.ts` (server-only, `getRecentSearches()` + `deleteRecentSearch(id)`, ApiResult-pattern ADR 0030)

**ADRs**:
- ADR 0060 (NY, Accepted) — 6 huvudbeslut + 6 mekanik-noter
- ADR 0039 amend — frontend ej längre konsumerar SavedSearch
- ADR 0055 amend — Senaste-hero-chip BE-stöd levererat
- ADR 0024 amend — cascade-tabell utökad
- `docs/decisions/README.md` index uppdaterad (även 0059 som saknades)

### Reviews

- **code-reviewer**: 0 Block / 0 Major / 8 Minor — Approved for push. Min-1 (rename `ssykComparer` → `stringListComparer`) fixad in-block. Övriga 7 Minor: paritets-frågor och dokumentations-anteckningar, ej blockerande.
- **security-auditor**: GDPR-1 BLOCKER (Art. 17-cascade saknas för recent_job_searches), 2 High (logging hygiene + default-browse-guard), 4 Medium (Art. 13, backup-retention, auth-invariant, audit-modell), 2 Low. Alla in-block-fixade:
  - GDPR-1: explicit `RemoveRange` för SavedSearches + RecentJobSearches i `AccountHardDeleter.HardDeleteAccountAsync` + ADR 0024-amend + integration-test. **Pre-existing SavedSearches-cascade-lucka samtidigt fixad in-block per CLAUDE.md §9.6** (samma fas, samma blast-radius).
  - High-1: `LogCaptureFailed` passerar `ex.GetType().FullName` istället för Exception-objekt (PII-hygien för Q-fritext)
  - High-2: explicit default-browse-guard innan `SearchCriteria.Create` (data-minimering Art. 5(1)(c))
  - Medium-3: `ICapturesRecentSearch` auth-invariant dokumenterad i XML-doc
  - ADR 0060 Mekanik-not 6: Art. 13 information-skyldighet + Klas-uppgift privacy-policy-uppdatering i F6 P4a frontend-batch
  - Q-backup-retention: dokumenterad acceptans i ADR 0060 Mekanik-not 5

### Tester (38 nya, alla gröna)

| Suite | Före | Efter | Delta |
|---|---|---|---|
| Domain.UnitTests | 378 | 399 | +21 (FilterHashCalculator 10, RecentJobSearch 11) |
| Application.UnitTests | 515 | 526 | +11 (Delete 4, List 7) |
| Architecture.Tests | 70 | 70 | (pipeline-order + taxonomy consumer-allowlist uppdaterade) |
| Worker.IntegrationTests | 68 | 69 | +1 cascade-test för HardDelete |
| Api.IntegrationTests | 351 | 356 | +5 auto-capture-flow + cross-tenant |
| Frontend vitest | n/a | 10 | +10 Zod-tester (pnpm tsc clean) |
| **Total .NET** | **~1382** | **~1420** | **+38** |

## Commits

| SHA | Typ | Beskrivning | Tag |
|---|---|---|---|
| 5bc6eea | feat | F6 P4a BACKEND — RecentJobSearches auto-capture-domän + ADR 0060 | v0.2.49-dev |

## Beslut + open questions

### Beslutade i denna session

1. **CTO-dom 3 multi-approach** (FilterHash/capture-mekanik/CurrentCount) — entydiga vinnare per Clean Arch + DDD + CQRS + YAGNI
2. **Cap=20/seeker** — Klas-bekräftelse efter merge (auto-mode-accepterad enligt CTO-motivering)
3. **In-block fix för pre-existing SavedSearches-cascade-lucka** — CLAUDE.md §9.6 (samma fas, samma blast-radius som RecentJobSearches-introduktionen)
4. **ADR 0060 verbatim av CC** — Klas-explicit-direktiv i startprompten (memory `feedback_klas_can_override_adr_verbatim_source`)

### Öppna för Klas

1. **F6 P4a FRONTEND** återupptas i Klas's existerande chat-session efter merge:
   - `/sokningar` route refactor: SavedSearchList → RecentSearchList (auto-fångade)
   - `<HeroChip>` Senaste-sökningar-dropdown i /jobb-hero (med "(N nya)"-format)
   - DELETE från RecentSearchRow + optimistic update
   - **Privacy-policy-uppdatering** (GDPR Art. 13 per ADR 0060 Mekanik-not 6) i samma FE-batch
   - Vitest-tester (RecentSearchList, HeroChip, DELETE-flow)
2. **Cap=20 bekräftelse** — i auto-mode antaget men Klas kan redirect:a
3. **F6 P4b SavedJobAds BACKEND** — separat prompt EFTER F6 P4a FE levererad
4. **dev-DB-migration** — `AddRecentJobSearches` körs vid tag-deploy `v0.2.49-dev`. Idempotent (additive ny tabell + 2 index).

## Lessons learned

- **CTO-domen löste flera designfrågor parallellt** — 3 multi-approach-rond gav entydiga svar i en agent-invocation utan att jag behövde lyfta varje val separat.
- **dotnet-architect korrigerade min lutning på 4 punkter** — speciellt EF-pattern (shadow text[]) och pipeline-behavior-constraint (runtime markör-check istället för dual generic-constraint). Architect-verifiering är värd ronden även om CTO redan domat.
- **In-block GDPR-fix** för pre-existing SavedSearches-cascade-lucka — security-auditor hittade en latent bugg som existerade innan denna prompt men aktiveras av samma flöde. Per §9.6 fixades in-block (rätt fas, blast-radius matchande). Sparade en TD-lyftning.
- **Frontend Zod-schema** har strict-mode-fallgrop på `SAVED_SEARCH_SORT_ORDER[i]` (`undefined`-möjlighet via `noUncheckedIndexedAccess`). `.find()` returnerar typade strängar utan unwrap-cast. Liten paritets-glidning från `saved-searches.ts` som använder `indexOf` + direct-access.

## Nästa session

**F6 P4a FRONTEND** — återupptas i Klas's chat-session. Behöver:
- HEAD `5bc6eea` (verifierad via `git log --oneline`)
- Tag `v0.2.49-dev` deployad till dev (verifierat via `/api/v1/me/recent-searches` 200)
- Privacy-policy-text (Klas-leverans) för Art. 13-disclosure

**F6 P5+** öppnas EJ förrän F6 P4a FE + F6 P4b BE+FE levererade.
