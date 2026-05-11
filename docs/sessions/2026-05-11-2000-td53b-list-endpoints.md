---
session: TD-53b list-endpoints + admin.ts kind-union-migration
datum: 2026-05-11
slug: td53b-list-endpoints
status: klar
commits:
  - aac9b2f feat(web) TD-53b list-endpoints + admin.ts
policy-ändring:
  - CLAUDE.md §9.6 4h-regel → fas-regel + CTO-auto-follow
---

# Session: TD-53b list-endpoints + policy-skift fas-regel

Stationär-CC-fortsättning ~19:00–20:00 efter TD-7 + TD-53a stängda samma kväll.
Sessionen levererade två saker:

1. **TD-53b stängd** (commit `aac9b2f`) — färdigställer ADR 0030 frontend kind-union
2. **CLAUDE.md §9.6 policy-skift** — 4h-regel borttagen, fas-regel etablerad

## Mål

Stänga TD-53b genom att refactora 4 list/admin-endpoints + 3 konsumenter till
`ApiResult<T>`-pattern per ADR 0030 §Migration. Slutföra ACL-outcome-täckningen
på hela frontend-API-ytan.

## Vad som levererades

### TD-53b refactor

4 endpoints migrerade till `Promise<ApiResult<T>>` med explicit return-type:

- `lib/api/applications.ts` — `getPipeline()` + `getApplications()`
- `lib/api/resumes.ts` — `getResumes()`
- `lib/api/admin.ts` — `getAuditLog()` + radering av lokala `AuditLogResponse`-typen

3 konsumenter använder exhaustive switch + `assertNever`:

- `app/(app)/ansokningar/page.tsx`
- `app/(app)/cv/page.tsx`
- `app/(admin)/admin/granskning/page.tsx` — `ErrorKind` utvidgad med `notFound`

`responseToResult` används unconditionally — `grep parseResponse` i `lib/api/`
returnerar 0 träffar efter touch.

### Policy-skift (CLAUDE.md §9.6)

Klas-direktiv: "Vi har för många TD. När vi jobbar med TD så skapar vi nya TD…
Vi måste fixa alla TD som vi kan innan FAS2."

§9.6 uppdaterad:

- **Ingen tidsbegränsning per touch.** 4h-regeln borttagen — den utlöste
  TD-bloat genom tidströskel-utlyftningar som kom tillbaka i nästa session.
- **Fas-regel etablerad.** TD lyfts ENDAST om fyndet hör till annan fas eller
  kräver saknad funktion-dependency. TDs i nuvarande fas fixas innan fas-stängning.
- **CTO-auto-follow.** CC går direkt till implementation efter CTO-beslut utan
  extra Klas-GO när motiveringen är entydig. Klas-STOPP triggas endast vid
  större strategiska frågor (fas-skifte, ADR-amendment, deploy-beslut).
- **CC rekommenderar inte vid multi-approach.** Variant A/B/C-val går till
  senior-cto-advisor, inte CC-rekommendation.

## Beslut

### CTO-beslut TD-53b (senior-cto-advisor 2026-05-11)

Två öppna beslutspunkter eskalerades till CTO:

**Beslut 1: `getApplications` har inga konsumenter — refactora ändå (Variant A)**

- ADR-spec-trohet: ADR 0030 §Migration listar `getApplications` explicit
  (Ford/Parsons/Kua 2017, fitness functions)
- CCP: list-endpoints ändras av samma anledning → samma kontrakt
  (Martin 2017, kap. 13)
- YAGNI gäller funktionalitet, inte konsistens i pågående refactor
  (Fowler 2018, "Divergent Change")
- Domän-evidens: `/ansokningar`-statusfilter är BUILD.md-listad feature

**Beslut 2: Test-scope — Variant Y (helper + typecheck, 0 nya tester)**

- Test pyramid: status-mapping bor i `responseToResult`, redan testad där
  (Fowler 2012, Cohn 2009)
- DRY i test-kod: 20 endpoint-tester återtester delad helper-logik
  (Hunt/Thomas 1999)
- Variant Y håller endast om tre hård-villkor uppfylls: explicit return-type,
  `responseToResult` unconditionally, `AuditLogResponse`-typen raderad

Alla tre villkor verifierade gröna efter implementation.

## Reviews

**code-reviewer:** 0 Blocker, 0 Major, 4 Minor (alla informativa), 1 Nit.
Approved. Konstaterar att `getApplications` är dead exported code, men ADR
0030-spec motiverar att den står beredd för Fas 2 list-vy.

**design-reviewer:** 0 Blocker, 1 Major + 1 Minor (åtgärds-krävda).

- M1: `role="alert"` på admin/granskning ErrorBlock — inkonsekvent med
  TD-53a-policy. **Fixad in-block.** TD-53a etablerade att server-renderad
  statisk fel-state inte är interrupt — same logik gäller granskning.
- m1: Kommentar saknades om varför `notFound`-case är död kod på
  list-endpoint. **Fixad in-block.**

Re-review behövdes inte (fix mekanisk + risk-fri per design-reviewer).

## Tester

| Suite | Antal | Diff |
|-------|-------|------|
| Frontend vitest | **217** | oförändrat (Variant Y: inga nya tester) |
| Domain.UnitTests | 163 | oförändrat |
| Application.UnitTests | 204 | oförändrat |
| Architecture.Tests | 32 | oförändrat |

`tsc --noEmit` grönt.

## Commits

| Commit | Scope |
|--------|-------|
| `aac9b2f` | `feat(web): TD-53b — ApiResult<T>-kind-union för list-endpoints + admin.ts` |

## Workflow-disciplin-anteckningar

Två disciplinmissar tidigt i sessionen som Klas korrigerade:

1. **CC gav rekommendation vid multi-approach** istället för att invokera
   senior-cto-advisor. Korrigerat via memory + uppdatering av §9.6.
2. **CC bad om Klas-GO efter CTO-beslut** trots att §9.2 redan etablerar
   CTO-auto-follow vid entydig motivering. Korrigerat — §9.6 nu explicit om
   när CC går vidare automatiskt.

Båda nedtecknade i feedback-memory + reglerade i policy.

## Nästa session

TD-53b stänger ADR 0030-migrationen helt. Frontend-API-ytan är konsistent över
detail- och list-endpoints (7 endpoints, 5+ konsumenter).

Aktiva TDs som inte blockerar Fas 1-stängning: TD-39, TD-41, TD-51, TD-52,
TD-56, TD-57, TD-58, TD-59. Per nya §9.6 fas-regel: TDs som hör till Fas 1
ska fixas innan Fas 2 påbörjas.

Föreslagna nästa-steg (väntar Klas-prioritering):

1. **TD-genomgång inför Fas 2** — kategorisera Fas 1 vs Fas 2+ för aktiva TDs
2. **STEG-progression** — se `docs/steg-tracker.md`
