---
session: Platsbanken sök-paritet — Fas C1 (query/filter-layer + yrke-nivåbyte)
datum: 2026-06-09
slug: platsbanken-sok-paritet-fas-c1
status: levererad (PR mot main, automerge)
bas-HEAD: 1fd9600
branch: feat/sok-paritet-query-layer-c1
commits:
  - feat(jobads): Platsbanken sök-paritet Fas C1 — query/filter-layer + yrke-nivåbyte
  - docs: Fas C1 docs-sync (current-work + session-log)
---

# Fas C1 — query/filter-layer + yrke-nivåbyte (Variant C)

## Mål
ADR 0067 Beslut 1 + Beslut 7 C1-raden. Gör B1+B2-kolumnerna sökbara + byt yrke-filtrets
nivå occupation-name → ssyk-level-4. Ren read-väg, ingen ny migration.

## Levererat
- **Yrke-nivåbyte (Variant C):** yrke-filtret targetar nu `OccupationGroupConceptId`
  (ssyk-level-4/yrkesgrupp) i stället för `SsykConceptId` (occupation-name). Den explicita
  Ssyk-equality-grenen BORTTAGEN ur `ApplyCriteria`. `SsykConceptId`-kolumnen + synonym-q-vägen
  BEVARAS (recall- + CV-substrat per ADR 0067 Beslut 1).
- **Nya filter-dimensioner:** `OccupationGroup` + `Municipality` i filter-SPOT
  (`JobAdFilterCriteria`) + `ApplyCriteria` (IN(...) via list.Contains mot shadow-prop, sträng-
  listor EJ VO — feedback_ef_strongly_typed_vo_contains) + `ListJobAdsQuery`/Validator/endpoint.
  Named args obligatoriskt (4 listor). Ssyk behålls som deprecerad no-op-param (matas av
  persisterad SearchCriteria.Ssyk/RecentJobSearch.Ssyk = C2-bunden).
- **3 konsumenter** mappar SPOT:en enhetligt: ListJobAds (passthrough alla), RunSavedSearch +
  ListRecentSearches (OccupationGroup/Municipality tomma, Ssyk passthrough). SPOT-disciplin
  (ADR 0039 Beslut 1 / ADR 0062 Beslut 3) bevarad — bytet sker inuti ApplyCriteria.
- **MaxConceptIds 10→400** (Domain single source) — ADR 0042 Beslut B-amendment (Klas-GO).
  400 = ssyk-level-4-universumets storlek → "Välj alla yrkesgrupper" träffar aldrig taket.
  "Markera alla" = tom lista (FE-kontrakt Fas E).
- **ITaxonomyReadModel-DTO additiv kaskad:** `TaxonomyRegionDto += Municipalities`,
  `TaxonomyOccupationFieldDto += OccupationGroups`, occupation-name `Occupations` BEHÅLLS
  (Open-Closed). `LoadAsync` 2 nya GroupBy(ParentConceptId). Exponerar kommun + ssyk-level-4
  (B1-CTO Beslut 2 sköt hit; B1 seedade noderna v30).
- **reverse-lookup-cap ×2→×4** (`ResolveTaxonomyLabelsQueryValidator` = MaxConceptIds×4 = 1600) —
  4 filterbara dimensioner i en chip-render. ADR 0043 implementerings-notat (ej amendment).

## Beslut / detours
- **CTO-domar (decision-maker, `docs/reviews/2026-06-09-sok-paritet-c1-cto.md`):**
  (a) Variant C nivåbyte (renast mot Beslut 1; A=Speculative Generality avvisad).
  (b) MaxConceptIds=400 (Klas erbjöd 200/400, CTO valde 400 = universumstorlek). Klas-STOPP→GO.
  (c) B2-dims (employment_type/worktime_extent) DEFER — NULL tills re-ingest ("falsk klar").
  (d) DTO additiv kaskad, occupation-name behålls.
  Cap-multiplikator ×4 (separat CTO-dom — fix nu, ej fast tal/DRY).
- **architect (`docs/reviews/2026-06-09-sok-paritet-c1-architect.md`):** behåll Ssyk-fält i SPOT
  (persistens-bunden), named args mot positionell drift, kaskad-DTO egna record-typer.
- **Klas-STOPP-flaggor passerade:** (a) FE-yrke-filter blir no-op tills Fas E — Klas GO:ade
  UX-fönstret (mitigeras av synonym/FTS-väg). (b) ADR 0042-amendment — Klas GO.
- **CC-beslut (test-writer-delegering):** ListJobAds handler passthrough Ssyk (ej Ssyk:[]) —
  pure-adapter + ETT no-op-ställe (ApplyCriteria) + symmetri med övriga konsumenter.

## Reviews
- code-reviewer: GO (0 Blocker/0 Major/1 Minor — kommentar-drift ×2→×4, fixad in-block).
- security-auditor: GO (0 Blocker/0 Major/1 Minor — ResolveTaxonomyLabels saknade ConceptIdPattern-
  regex, fixad in-block + test). Ingen DoS-vektor utöver CTO-analysen, ingen PII/injection.

## Tester (Testcontainers, ej InMemory)
Domain 423, Application 659 (+5 charset), Arkitektur 78, Api-integration 407 (+20 nya:
OccupationGroup/Municipality-filter, Ssyk-no-op, DTO-kaskad), Migrate 6, Worker 70. Alla gröna.
Bygg 0 warn/0 err.

## Inga TDs lyfta
B2-dims-defer ryms i ADR 0067-fasplanen (D1-grannskap); Ssyk-full-borttagning = C2 (VO-expansion).
Inga nya migrations, inga nya dependencies.

## Nästa
Fas C2 (SearchCriteria-VO-expansion + reverse-lookup-migration av sparade Ssyk-sökningar →
ssyk-level-4) ELLER Fas E (FE-picker nivåbyte + Län→Kommun/Yrkesområde→Yrkesgrupp-kaskad +
filter-panel — gör nivåbytet synligt; "Markera alla"=tom-lista-kontrakt). Klas-GO för fas-skifte.

## Pending operativt för Klas
- Granska C1-PR-diff post-merge (automerge).
- Re-ingest Klass 2 (B2-avslut, valfri timing): `POST /api/v1/admin/job-ads/backfill-klass2`.
- GO för nästa fas (C2 eller E).
