---
session: Väg E — TDs-cleanup (TD-40 + TD-49)
datum: 2026-05-11 ~15:30
slug: vag-e-tds-cleanup
status: KLAR
commits:
  - test(web): TD-40 — leaf-path regression-bevakning för resume-schemas refines
  - docs: TD-49 stängd som redan-implementerad pre-TD-skapande
---

# Väg E — TDs-cleanup (TD-40 + TD-49)

## Mål

Test-only-batch ovanpå Väg B-a11y-pass: stänga 2 TDs som låser
befintliga defenses mot regression. Klas-val: Väg E "mer TDs-cleanup"
över Väg A (Fas 2-prereq) eftersom A kräver webb-Claude för strategi.

- TD-40 (path-equality regression-bevakning för `resume-schemas` refines)
- TD-49 (unit-test för `HstsOptions.EnsureSafeForEnvironment`)

## Sammanfattning

2 commits. **Inget produktionskod-touch** (test-only + docs-only).
Backend 594/594 oförändrat + Frontend 150 → 153 grönt (+3 TD-40-tester).
1 agent-review (code-reviewer) APPROVE med Minor in-block-fixad per 4h-regel.

## Discovery-fynd som ändrade scope

**TD-49 var redan löst.** Discovery via grep `EnsureSafeForEnvironment`
över `tests/`-trädet hittade `tests/JobbPilot.Api.IntegrationTests/
Configuration/HstsOptionsTests.cs` (143 rader, skapad vid STEG 13c
2026-05-10) som täcker samtliga 6 TD-49-cases.

dotnet-architect-reviewen som lyfte TD-49 letade efter
`JobbPilot.Api.UnitTests/`-projekt (existerar inte) och missade
befintlig pattern `JobbPilot.Api.IntegrationTests/Configuration/` som
har unit-style tester (samma pattern som `ForwardedHeadersConfigTests`,
`RateLimitingOptionsTests`, `ProductionStartupSmokeTests`,
`UseHttpsRedirectionGateTests`).

**Lärdom:** TD-skapande ska verifiera test-existens via grep + Glob över
ALLA test-projekt, inte anta projekt-namn.

Block B blev därför docs-only-stängning med audit-trail i tech-debt.md.

## Implementation

### Block A — TD-40 regression-bevakning för leaf-path

**Discovery:**
- `resume-schemas.ts` har 2 refines (rad 81 + 101) med explicit
  `path: ["endDate"]` på `experienceSchema` resp `educationSchema`.
- `resume-content-form.tsx` använder `pathToElementId` från
  `resume-path-routing.ts` (TD-46-arvet) för focus-flytt vid
  serverError.
- Risk: framtida `.refine()` på `z.object()` utan explicit leaf-path
  → Zod sätter path till array-rot eller toppnivå → fieldA11y missar
  aria-invalid + pathToElementId returnerar null → ingen focus-flytt.

**Test-tillkomst (commit `6b8f087`):**

Nytt `describe`-blok i `resume-schemas.test.ts`:
- Test 1: experiences refine → path `experiences.0.endDate` +
  `pathToElementId` → `exp-0-endDate`
- Test 2: educations refine → path `educations.0.endDate` +
  `pathToElementId` → `edu-0-endDate`
- Test 3: Icke-0-index → path `experiences.1.endDate` →
  `pathToElementId` → `exp-1-endDate` (regex-grupperings-bug-bevakning
  för pathToElementId)

**Code-reviewer:** APPROVE (0 Blocker, 0 Major, 1 Minor, 1 Nit)
- Minor (in-block-fixad): `findIssueAtPath`-helper som söker via
  path-prefix istället för message-string-match. Path är invarianten
  vi skyddar — copy-tweaks ska inte rödna testet.
- Nit: it-titel "experiences.N.endDate" → "experiences.0.endDate"
  (alltid 0 i de första 2 testen). Kosmetisk konsistens.

Båda fixade in-block (samma commit).

### Block B — TD-49 docs-only-stängning

**Verifierad täckning i `HstsOptionsTests.cs`:**

| TD-49-case | Befintlig test |
|---|---|
| 1. Production MaxAge<365 → throws | `FailsLoud_OnLowMaxAgeDays_OutsideDevTest` (Theory) |
| 2. Production MaxAge=365 → ok | `AcceptsSpecCompliantDefaults_InProduction` |
| 3. Development MaxAge=0 → ok | `AllowsAnyConfig_InDevOrTest` (Theory) |
| 4. Preload+MaxAge<365 → throws | Implicit (case 1-branchen kastar först) |
| 5. Preload+!IncludeSubDomains → throws | `FailsLoud_OnPreloadWithoutIncludeSubDomains` |
| 6. Empty env-name → throws | `ThrowsArgumentException_OnEmptyEnvironmentName` (Theory) |

Plus 3 bonus-cases (defaults-spec-validering, config-bindning,
`AcceptsValidPreloadConfig`).

Tech-debt.md TD-49 markerad som STÄNGD med audit-trail-länk till
HstsOptionsTests.cs + STEG 13c-historik (commit `954fe1e`).

## Reviews-rapporter

- `docs/reviews/2026-05-11-td40-code-reviewer.md`

## Tester

| Suite | Före session | Efter session | Tillkomst |
|-------|--------------|---------------|-----------|
| Backend totalt | 594 | 594 | — (ingen backend-touch) |
| Frontend Vitest | 150 | 153 | +3 TD-40 regression-tester |

## Lärdomar

- **TD-skapande ska verifiera test-existens via grep, inte anta
  projekt-namn.** TD-49-fall-typ: review-fynd antog `Api.UnitTests`-
  projekt och lyfte TD utan att verifiera om befintligt pattern i
  IntegrationTests/Configuration/ täckte fyndet. 5-min Discovery hade
  sparat hela TD-skapandet.
- **Discovery → scope-ändring är OK midstream.** Klas instruktion
  "GO more TD cleanups" var entydigt val av Väg E, men scope-ändring
  från "2 implementations" till "1 implementation + 1 docs-stängning"
  är rätt call när discovery visar att TD redan är löst. Rapporterat
  pre-impl så Klas hade möjlighet att redirect:a.
- **Path-equality regression-bevakning är "kontrakts-test" mellan
  moduler.** TD-40-testet låser kontraktet `resume-schemas.refines`
  ↔ `resume-path-routing.pathToElementId`. Båda kan ändras separat,
  men inte i sätt som bryter kompatibiliteten. Naturlig hemvist:
  bredvid schemat (testar schemats output), import från routing
  istället för flytt av testet.
- **Path-prefix-match > message-string-match i regression-tester.**
  Initial implementation matchade `i.message === "Slutdatum..."`
  vilket binder testet till copy-strängen. Code-reviewer-Minor
  påpekade att copy-tweaks då rödnar testet utan att invarianten
  (path = leaf) är regressed. `findIssueAtPath`-helpern fokuserar
  testet på det vi faktiskt skyddar.

## Aktiva TDs efter denna session (7)

- TD-39 (error-summary-mönster)
- TD-41 (Select-komponent-konvention)
- TD-51 (admin-läs audit-logging Fas 6)
- TD-52 (admin-endpoint rate-limit Fas 6)
- TD-53 (frontend kind-union standardisering >4h)
- TD-56 (ListJobAds Fas 2-paginering)
- TD-57 (native form-controls divergerar)

## Stängda TDs (kumulativt)

TD-15, TD-31, TD-38, TD-40, TD-42, TD-43, TD-44, TD-45, TD-46, TD-47,
TD-48, **TD-49 (denna session)**, TD-50, TD-54, TD-55.

Plus de tre i denna session: **TD-40 (Block A) + TD-49 (Block B)** —
totalt 15 TDs stängda i Fas 1-stängning-svit (2026-05-10 → 2026-05-11).

## Pre-existing infra (oförändrat)

| Resurs | Identifier |
|---------|-----------|
| Public URL | `https://dev.jobbpilot.se/api/ready` |
| API task-def | `jobbpilot-dev-api` |
| Worker task-def | `jobbpilot-dev-worker` |
| Tag (senaste) | `v0.1.2-dev` på SHA `7cde3c7` |

## Nästa session

Klas väljer vägen framåt:

- **Väg A — Fas 2-prereq:** ADR 0005 (go-to-market) + kostnadsskydd
  (kräver webb-Claude för strategiska val) — fortfarande aktuell
- **Väg E forts. — Resterande TDs-cleanup:** Återstående 7 TDs är
  mer komplexa eller fas-deferrade:
  - TD-39 (error-summary) — Fas 2+ "vid faktisk användarsignal"
  - TD-41 (Select-konvention) — kräver design-beslut
  - TD-51/TD-52 (Fas 6-deferrade)
  - TD-53 (kind-union >4h, kräver ADR)
  - TD-56 (Fas 2 JobTech)
  - TD-57 (native form-controls 3 varianter, kräver design-beslut)

**Min rek:** Väg A med webb-Claude. TDs-listan består nu nästan
uteslutande av deferrade eller design-beslut-krävande items —
Fas 2-uppstart är högsta värde-aktion.

Startprompt sparas separat eller embedded i nästa session.

## Cost

Oförändrat ~$79.65/mån (ingen infra-touch).
