---
session: 2026-05-11 ~20:30
datum: 2026-05-11
slug: td7-td53a-frontend-acl
status: pushed
commits:
  - 76f758a feat(web) TD-7 Zod-DTO-validering + ADR 0020
  - 7e90b36 feat(web) TD-53a ApiResult-kind-union detail-endpoints + ADR 0030
---

# Session: TD-7 + TD-53a — Frontend ACL-arkitektur etablerad

Stationär CC-session 2026-05-11 ~16:00–20:30. Klas valde TDs framåt och
explicit-godkände bägge TD-stängningar in-session. Två ADRs etablerade,
två TDs stängda, en TD lyft som genuin framtida supersession.

## Mål (vad Klas frågade om vid session-start)

> "vilka TD är öppna, hur många, hur många är blockerade till FAS2+ ?"
> ... senare ...
> "Vi börjar med TD7, efter den är klar tar vi fokus på TD-53"
> ... senare ...
> "KÖR direkt"

CC svarade med komplett TD-mapping (34 öppna varav 14 blockerade Fas 2+).
Sedan kör-läge — minimerade STOPP-pauser efter Klas-GO.

## Vad som levererades

### TD-7: Zod-DTO-validering vid HTTP-gränsen (commit `76f758a`)

Stänger Major-latent shape-skew från security-auditor 2026-05-07 Turn 2
(`roles?: string[]` accepterade tyst saknad nyckel som tom array → tom
roles-array i UI). 11 unchecked `as Dto`-casts ersatta med
`parseResponse(res, schema, context)` som validerar via Zod-schemas.

**ADR 0020** dokumenterar Parse-don't-validate-mönstret + Zod som ACL-verktyg.
CTO-triage valde Variant A över Variant B (OpenAPI-codegen — kräver pipeline
saknad) och Variant C (hand-rullade guards — regression).

**Implementation:**
- `lib/dto/_helpers.ts` — `parseResponse<T>` + `pagedResult`-factories + `redactIssues` (PII-skydd)
- 6 Zod-DTO-filer: `me, applications, resumes, admin, auth, common`
- 11 call-sites refactorade: `auth/session.ts`, `app/api/me/route.ts`, `lib/api/*.ts` (4 filer), `lib/auth/actions.ts` (3 sites), `lib/actions/{applications,resumes}.ts`
- `lib/types/*.ts` blir tunna re-exports; `lib/types/paged.ts` raderad
- 52 nya unit-tester

**Reviews:**
- code-reviewer: 1 Major (missad call-site `app/api/me/route.ts`) fixad in-block + 3 Minor/Nit skippade som opportunistic
- security-auditor: 0 Blocker/Major; Minor 2 (PII via Zod `received`) + Minor 9 (non-strict-motivering) fixade in-block; TD-7 Major 1 verifierat stängd via regression-test (`me.test.ts:21`)

**Ny TD lyft:** TD-62 (OpenAPI-codegen-supersession Fas 2+) — välmotiverad
inte-dumpning-ställe; pekar mot framtida pipeline-etablering.

### TD-53a: ApiResult-kind-union detail-endpoints (commit `7e90b36`)

Stänger del 1 av TD-53. Tidigare `T | null`-pattern komprimerade fyra
betydelser (unauthorized/forbidden/notFound/error) till en bit. Detail-pages
kunde inte skilja "ingen behörighet" från "tekniskt fel" — samma generic
404/fel-fallback för helt olika user-actions.

**ADR 0030** etablerar `ApiResult<T>` discriminated union + `responseToResult`-helper +
`assertNever` exhaustiveness-skydd. CTO-triage valde Variant A (full kind-union)
över Variant C (hybrid CCP-brott, Martin 2017 kap. 14) och Variant B (`{ok, error}`
funktionellt-flavored). Scope-split TD-53a/b per 4h-regeln.

**Implementation:**
- `lib/dto/_helpers.ts` utökat med `ApiResult<T>` + `responseToResult` + `assertNever`
- 3 endpoints refactorade: `getMyProfile`, `getApplicationById`, `getResumeById`
- 3 konsumenter uppdaterade: `mig/page.tsx`, `ansokningar/[id]/page.tsx`, `cv/[id]/page.tsx`
- 11 nya unit-tester (status-prioritet, exhaustiveness, redact)

**`getServerSession()` undantaget** — `null` är legitim auth-pipeline-
semantik per ADR 0017, inte fel-komprimering.

**Reviews:**
- code-reviewer: 0 Blocker/Major; 2 Minor + 3 Nit fixade in-block
  - CR-M1 (unauthorized-placering inkonsekvens): dokumenterat med kommentar `mig/page.tsx:18-20`
  - CR-M2 (`return assertNever`): fixad i båda detail-pages
  - CR-N1 (`Awaited<ReturnType<...>>` → direkt-import): fixad
- design-reviewer (initial): **2 Blocker + 3 Major + 3 Minor** — alla in-block-fixade:
  - **B1** focus-ring på Tillbaka-länkar → Button asChild variant="outline" (löser även M2)
  - **B2** `role="alert"` dubbel-annonsering → borttaget från alla tre filer
  - **M1** generisk h1 "Tekniskt fel" → specifik "Kunde inte ladda ansökan/CV"
  - **M2** svag tillbaka-affordans → Button asChild
  - **M3** mig/page kollapsade notFound + forbidden + error → separerat med onboarding-copy
  - **Mi1** "Inga roller tilldelade" → "Inga roller"
  - **Mi3** "CV:t" omformulering → indirekt fixad via M1
  - **Mi2** datumformat → pre-existing, skippad
- design-reviewer (re-review): Approved — mergeklar

## Decisions och detours

### TD-7 discovery avslöjade scope-utvidgning (5 extra missed casts)

Initial-plan: 6 call-sites enligt TD-7-inventering. Discovery efter base-leverans
hittade 5 ytterligare `as`-casts i `lib/auth/actions.ts` (3) + `lib/actions/{applications,resumes}.ts` (2).

Code-reviewer flaggade `app/api/me/route.ts` som Major (same CurrentUser-shape som session.ts validerar).
Per 4h-regeln + ADR 0020 acceptance-kriterium ("0 `as Dto` i lib/auth/") fixades alla 6 in-block. Total 11 casts ersatta istället för planerade 6.

### TD-53 split TD-53a/b var explicit CTO-beslut

Original TD-53-scope >4h. CTO-triage motiverade split mot 4h-regeln (CLAUDE.md §9.6
kriterium 3) — inte spara-TD-anti-pattern, utan genuin scope-split där varje del
är dess egen leverabel batch. ADR 0030 §Migration listar TD-53a + TD-53b stegen.

### Design-reviewer fynd vid initial-pass var högre svårighet än väntat

2 Blockers (a11y) + 3 Majors (UX-copy + design-affordans) — väl värda fixarna
men ovanlig påverkan för en arkitektur-refactor-TD. Re-review krävdes innan
merge. Re-review-Approved utan ytterligare iteration. Total ~30 min extra
för UX-polish-block.

### CTO-CR rekommenderade convention-call för TD-53b unauthorized-placering

CR-M1 hänvisade explicit till senior-cto-advisor för konvention-decision INNAN
TD-53b startas. CC valde att dokumentera valet med kommentar i `mig/page.tsx:18-20`
istället för att invokera CTO för 3-rader-konsekvens. Lyfts till nästa session
om Klas vill ha formell konvention-ADR.

## Pushed commits

- `76f758a` — `feat(web): TD-7 — Zod-DTO-validering vid HTTP-gränsen + ADR 0020`
- `7e90b36` — `feat(web): TD-53a — ApiResult<T>-kind-union för detail-endpoints + ADR 0030`

## Tester (full svit grön efter både commits)

| Suite | Antal | Diff |
|-------|-------|------|
| Frontend vitest | 217 | +63 (52 TD-7 + 11 TD-53a) |
| Domain.UnitTests | 163 | oförändrat |
| Application.UnitTests | 204 | oförändrat |
| Architecture.Tests | 32 | oförändrat |

## Nästa session — TD-53b

**Scope:** Refactor 3 list-endpoints (`getPipeline`, `getApplications`, `getResumes`) +
migrera `admin.ts:AuditLogResponse` till generic `ApiResult<T>`. 4 endpoints + 3 konsument-sidor
(`ansokningar/page`, `cv/page`, `admin/granskning/page` + komponenter).

**Scope-estimate:** ~4-6h CC-tid. Inom 4h-regeln övre gräns.

**Konvention-fråga från CR-M1:** unauthorized-placering pattern för list-endpoints —
detail-pages-mönster (switch-inom) är CC-rekommendation utan ny CTO-call.

**TDs som inte blockerar:** TD-39, TD-41, TD-51, TD-52, TD-56, TD-57, TD-58, TD-59.
