---
session: 2026-05-17 autonom natt
datum: 2026-05-17
slug: soktyta-platsbanken-fynd1-2-cron-gron
status: Fynd 1 pushad+grön; Fynd 2 backend lokalt klart (ej pushat, väntar Klas push-GO + ADR Accepted-flip); cron-grön CONFIRMED
commits:
  - 37338db feat(web) Fynd 1 sortering-separation + etiketter (PUSHAD)
  - a4afa40 docs(reviews) Fynd 1 design-reviewer + Fynd 2 CTO-underlag (PUSHAD)
  - 2e8e380 docs(ADR 0043) Proposed (lokalt)
  - c86daca docs(ADR 0043) scope-fork Beslut E (lokalt)
  - 0f46dad chore(infra) F2TaxonomySnapshot migration (lokalt)
  - 67121d4 test(jobads) taxonomi-ACL testsvit (lokalt)
  - ac9e8da test(jobads) post-triage grön svit + ProdBubble (lokalt)
  - 75f0510 feat(jobads) ADR 0043 taxonomi-ACL backend (lokalt)
  - + docs(ADR 0043 triage + reviews-trail) (lokalt)
---

# Autonom natt-session — sök-yta Platsbanken-anpassning + cron-grön

## Utgångspunkt
Klas-prompt: cron-grön async-followup (uppdrag A). Klas live-jämförde /jobb mot
Platsbanken under sessionen → 2 design-fynd. Klas-GO "för båda" + autonomt
non-stop ("göra klart det mesta", fråga CTO vid oklarhet, kolla 02:00 UTC,
push-GO när Klas vaknar).

## Fynd 1 — sortering-separation + etiketter (LEVERERAD, PUSHAD)
Sortering låg gömd i Filter-disclosuren (Batch 6). CTO Fråga 2 = Approach A
(copy-only, behåll 5-enum, ingen Beslut D-ändring). Flyttad till egen
alltid-synlig kontroll; "Sist/Tidigast sista ansökningsdag" → "Stänger
senare/snart" (jobbpilot-design-copy). design-reviewer APPROVED 0/0/0,
vitest 358, tsc/lint rena. Pushad `37338db`+`a4afa40` (Klas-GO).

## Fynd 2 — Taxonomi-ACL (BACKEND KLART, LOKALT, EJ PUSHAT)
Klas: concept-id `MVqp_eS8_kDZ` + "OR-bevakning" obegripligt. CTO Approach A
(Evans ACL kap.14): lokal taxonomi-snapshot, svenska namn-väljare, concept-id
ur UI. Klas valde ny **ADR 0043** + fullt autonomt.

**Beslutskedja:** dotnet-architect (Clean Arch-design) → senior-cto-advisor
MAP-1 (Variant A seedad snapshot) / MAP-2 (ITaxonomyReadModel-port, IAppDbContext
växer ej) / MAP-3 (TaxonomyReadPolicy 20/60s + ETag + cap=MaxConceptIds×2) →
scope-fork (Variant A: Län + Yrkesområde→Yrke; kommun = payload-verifierings-
trigger, `municipality_concept_id` finns ej som shadow-kolumn) → adr-keeper
ADR 0043 Proposed.

**Implementation:** Application-port + 2 queries/handlers/validator + DTOs;
Infrastructure TaxonomyConcept/-Meta-entiteter + EF-config + embedded
`taxonomy-snapshot.json` (genererad via JobTech GraphQL, off-search-path) +
idempotent version-medveten TaxonomySnapshotSeeder (advisory-lock, 42P01-grace
Dev/Test) + singleton TaxonomyReadModel; Api GET /job-ads/taxonomy(+/labels)
+ TaxonomyReadPolicy. SearchCriteria/JobAdSearch/shadow-props ORÖRDA.

**db-migration-writer:** F2TaxonomySnapshot (2 fristående tabeller, ingen FK,
Testcontainers-verifierad, ingen seed i migration).

**test-writer TDD-Red → 3 prod-defekter:** #1 taxonomin är graf ej träd
(occupation-name multi-field → PK-krock); #2 validator NRE på null; #3 seeder
bryter 9 cold-start-fixturer. CTO defekt-triage: #1 = Variant C i generatorn
(kanoniskt dedupliserad snapshot, deterministisk primär-fält-regel, noll
kodändring); #2 = `.Cascade(CascadeMode.Stop)` in-block; #3 = delad
`RemoveStartupSeeders()`-extension speglar IdempotentAdminRoleSeeder.
Post-fix svit **1130 grön, 0 failed** + ny TaxonomySnapshotSeederProdBubbleTests.

**security-auditor: GO** 0 Crit/High/GDPR. 1 Minor (faulted-Lazy<Task>
permanent-fail) in-block-fixad → lås-fri Volatile-publicering (endast lyckad
laddning cachas; fault → retry). Rate-limit 20/60s verifierat. FE-flagga:
rendera reverse-lookup-label som text (nextjs-ui-engineer-scope).

## cron-grön (uppdrag A) — CONFIRMED GRÖN
Post-v0.2.9/10-dev 02:00 UTC-snapshot: `[5401] startad` (02:00:07) →
`[5004]` trunkerad attempt=1 (parsade 8340) → `[5004]` attempt=2 (21960) →
**`[5005]` bounded retry uttömd efter 3 försök (36570/36570 konverterade),
avslutar gracefully** (02:16:33). JsonException fångad vid enumeration-boundary
— NOLL ofångad Hangfire-storm (kontrast: gammal 02:00 = 56-storm). Korpus
(auth-API): 5 380 (red-verify) → 5 477 (00:55) → **19 816** (efter graceful
run). ADR 0032-amendment gate-def (storm-borta + korpus-trajektoria + 5005)
**HELT UPPFYLLD** — hybrid-konvergensen bekräftad i prod.

## Beslut & avvägningar
- §9.2 sessionsbyte/fas-reopening: Klas explicit GO "för båda" uppfyllde kravet.
- ADR 0043 förblir **Proposed** — Accepted-flip = Klas-GO (sessionens förbud).
- Allt Fynd 2 hålls **opushat** — Klas push-GO när han vaknar (hans uttryckliga val).
- Multi-approach → senior-cto-advisor (×4), aldrig CC-egen rek (memory
  feedback_cto_decides_multi_approach). Defekter → CTO (#1/#3), in-block (#2).
- DI samma commit som port-impl (memory feedback_di_with_handlers).

## Nästa session
- **Klas push-GO** för Fynd 2 lokala commits → backend deploy (tag) +
  ADR 0043 Proposed→Accepted-flip + frontend-batch.
- **Frontend (scopat, ej påbörjat):** nextjs-ui-engineer ersätter
  JobAdMultiSelect concept-id-input med hierarkiska Län + Yrkesområde→Yrke
  namn-väljare som konsumerar /api/v1/job-ads/taxonomy; behåll ADR 0042
  Beslut A-disclosure + Beslut B URL-multi (onChange emitterar fortsatt
  concept-id); FE-flagga (label som text). design-reviewer VETO +
  visual-verify post-deploy + Klas skärmbilds-approve.
- docs-keeper: ADR-index 0043-rad + cross-ref-verifiering.
- TD-19/23/24/62/63/74/82 "Fas 2"→Trigger etikett-städning (kvarstår).

---

## Forts. — Klas "GO allt enligt rek" → Fynd 2 fullt deployad (2026-05-17 fm)

- **ADR 0043 Proposed→Accepted** (adr-keeper `8c7e582`, docs-keeper index `5075439`, Klas review-GO). Endast statusfält — brödtext immutable.
- **Backend deploy:** tagg `v0.2.11-dev` → GH `deploy-dev` run `25983313208` **success**. Verifierat: `/api/ready` 200, `GET /api/v1/job-ads/taxonomy` 200 + `Cache-Control: private` + ETag, träd 21 län/21 yrkesområden/2323 yrken (migration F2TaxonomySnapshot + TaxonomySnapshotSeeder körda korrekt — funktionellt bevis via live-endpoint).
- **Frontend:** nextjs-ui-engineer (`c79aace`) — `region-picker`/`occupation-picker`/`taxonomy-chip-list` + server-side träd/reverse-lookup i page.tsx, Beslut B URL-multi oförändrat, FE-säkerhetsflagga följd (label som text). vitest 387→ (efter död-kod-rm) job-ads 70/70, tsc 0, lint 0. Död `JobAdMultiSelect` borttagen (`1fc3b1b`). design-reviewer kod-review APPROVED 0/0/0.
- **Push:** `782414d` på origin/main (Vercel auto-deploy). pre-commit/pre-push gröna.
- **visual-verify:** 56 shots live mot https://www.jobbpilot.se (`C:\tmp\jobbpilot-visual\20260517-0849`). design-reviewer post-deploy skärmbilds-granskning **APPROVED 0/0/2** — Klas kan slutgodkänna.

### Pending Klas / uppföljning
1. **Klas slutgodkänn skärmbilderna** (Batch 6-grind — inneboende Klas-steg).
2. **saved-search-list `criteriaSummary` rå concept-id** + Spara-sökning "SSYK-kod"-copy → separat förhandlad batch (bulk-namnuppslag överskrider ADR 0043 Beslut D-cap; §9.6 saknad funktion-dependency, CTO/Klas-triage). EJ autonomt.
3. **visual-verify `jobb-chip-filled` stale** → `selectOption` istället för `.fill()` (harness-uppföljning, nextjs-ui-engineer/CC).

Fynd 2 är funktionellt komplett och deployad på dev; återstår Klas skärmbilds-approve + de två uppföljningarna ovan (ej blockerande mot leveransen).
