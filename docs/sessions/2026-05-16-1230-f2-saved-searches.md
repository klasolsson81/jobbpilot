---
session: F2 Saved Searches — end-to-end (sista Fas 2-leverabeln)
datum: 2026-05-16
slug: f2-saved-searches
status: levererad (Fas 2-milstolpen "spara sökningar" funktionellt klar)
commits:
  - b82e7cf docs(adr): ADR 0039 Accepted + index
  - ae7a521 docs(adr): ADR 0040 Proposed + BUILD.md §18-backlog
  - b18074f feat(saved-searches): backend end-to-end
  - 717dbd9 docs(tech-debt): TD-84
  - d602968 feat(web): frontend Spara sökning + /sokningar
---

# F2 Saved Searches — session-retrospektiv

## Mål

Leverera "spara sökningar" end-to-end (BUILD.md §5.1/§9.x/§16/§18) — sista
oimplementerade Fas 2-leverabeln. `SavedSearch` AR + `SearchCriteria` VO,
6 endpoints JobSeeker-scoped, frontend Spara/lista/kör/radera.

## Vad som levererades

**Design INNAN kod (§9.2):** dotnet-architect (aggregat/EF/run-design) →
senior-cto-advisor (4 beslut). ADR 0039 (Accepted, Klas-GO):
- Beslut 1: `JobAdSearch` delad modul — `ApplyCriteria`/`ApplySort`
  extraherade ur `ListJobAdsQueryHandler` (behaviour-preserving, SPOT).
  `JobAdSortBy` flyttad Application→Domain (Clean Arch §2.1).
- Beslut 2: `RunSavedSearch` = query utan skriv-sidoeffekt; `last_run_at`
  kolumn finns men skrivlogik → Fas 5 (notification-cadence).
- Beslut 3: `SortBy` ingår i `SearchCriteria`-VO (reproducerbarhet, Evans).
- Beslut 4: `notification_enabled` lagras (default false), dispatch → Fas 5.

**Klas-input under sessionen:** önskemål om ett framtida "smart" CV-baserat
filter (AI härleder yrkesurval ur CV). senior-cto-advisor-vägning: båda
filtertyper sekventiellt, smart = Fas 4+ ovanpå `SavedSearch`, `SearchCriteria`
låst (ej multi-occupation nu — VO-värde-equality + okänd form). Dokumenterat
som **ADR 0040 (Proposed)** + BUILD.md §18 Fas 4-backlog (Klas-GO på spec-edit).

**Backend (`b18074f`):** domän (AR + VO + 3 events), 6 handlers + validators
(alla `ICurrentUser`→JobSeeker-scopade, `IFailedAccessLogger` cross-tenant
ADR 0031 oskiljbart NotFound), EF-konfig (`criteria` jsonb `.ToJson()`,
soft-delete query filter), migration `20260516092628_F2SavedSearches`
(additiv, snake_case), 6 API-endpoints (auth-gated, run har ListReadPolicy).
113 nya tester. Domain 293 / App 398 / Arch 51 / Integration 268 gröna.

**Frontend (`d602968`):** `SaveSearchButton` på /jobb (fångar filterläge),
`/sokningar` (lista), `/sokningar/[id]` (kör — återanvänder JobAdList +
JobAdPagination), `DeleteSavedSearchDialog` (shadcn Dialog, DESIGN.md §6),
shell-nav. lib dto/api/actions. 21 nya vitest. tsc 0 / lint 0 / 334 vitest.

## Beslut och avstickare

- **Smart CV-filter (Klas-grundtanke):** mid-session strategisk input.
  CTO-vägd, dokumenterad (ADR 0040 Proposed + BUILD-backlog), gatear ej
  pågående kod (`SearchCriteria` låst per ADR 0039 Beslut 3).
- **OBSERVATION 1 (test-writer):** PATCH/DELETE mappar `NotFound`→400.
  CTO-triage: projekt-brett mönster (13 endpoints, 4 filer), ingen
  differentiell ADR 0031-läcka (cross-tenant + okänt-id ger BÅDA samma 400).
  Alt B — commit oförändrad, **TD-84** (Minor/Trigger, cross-cutting, egen
  ADR vid åtgärd). Ej in-block (skulle bryta lager-konsistens/DRY).
- **OBSERVATION 2:** body-enum `sortBy` kräver numeriskt värde (ingen global
  `JsonStringEnumConverter`) — konsistent projektkontrakt, ej defekt.
  Frontend mappar string↔index i dto-lagret.
- **design-reviewer Blocker:** inline tvåstegs-radering bröt DESIGN.md §6
  (kräver bekräftelse-Dialog). Fixat in-block via `DeleteSavedSearchDialog`
  (speglar `delete-resume-dialog.tsx`). Re-review approved.

## Reviews

- dotnet-architect + senior-cto-advisor (×3) INNAN kod.
- code-reviewer: 0 Blockers / 0 Majors (1 Minor in-block-fixad).
- security-auditor: 0 Critical/High/Medium (1 Low accepterad). Cross-tenant
  + ADR 0031-oskiljbarhet + GDPR (criteria ej i logg/audit) verifierat.
- design-reviewer: 1 Blocker + 2 Minors → in-block-fixade → re-review approved.
- db-migration-writer: migration verifierad, snake_case OK, snapshot ren.
- test-writer: 113 backend-tester.

## Pending / nästa session

- **Visuell verifiering auth-gated** (/jobb, /sokningar, /sokningar/[id])
  → pending live-deploy mot dev-backend per runbook (mock-session används ej).
  Deploy = tag-push, kräver Klas-GO.
- **F2 jobb-ingestion-verifiering** (separat spår): snapshot-cron 02:00 UTC,
  verifieras i egen lokal session med AWS SSO (CloudWatch EventId 5402 +
  `job_ads`-count). Opåverkad av denna batch.
- ADR 0040 (smart CV-filter) detaljdesign vid Fas 4-start.
- TD-84 vid opportunistisk touch / OpenAPI-export.

## Fas 2-status

Milstolpen "söka jobb på Platsbanken + spara sökningar" är **funktionellt
klar** modulo ingestion-live-verifiering (separat spår) + auth-gated visuell
verifiering (pending live-deploy).
