# Current work — JobbPilot

**Status:** **TD-7 + TD-53a + TD-53b STÄNGDA OCH PUSHED 2026-05-11 — ADR 0030 frontend-migration KLAR.** Hela frontend-API-ytan på `ApiResult<T>`-kind-union (7 endpoints, 5+ konsumenter). Dubbel ACL etablerad: ADR 0020 shape-validering (Zod) + ADR 0030 outcome-semantik (kind-union).
**Senast uppdaterad:** 2026-05-11
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`
**Policy-skift denna session:** CLAUDE.md §9.6 — 4h-regel ersatt av fas-regel + CTO-auto-follow-disciplin

---

## Aktivt nu — ADR 0030-migration KLAR, väntar Klas-prioritering för Fas 1-stängning

Stationär-CC-session 2026-05-11 ~16:00–20:00. Fyra arbets-block:

1. **TD-7 STÄNGD** (commit `76f758a`) — Zod-DTO-validering vid HTTP-gränsen + ADR 0020
2. **TD-53a STÄNGD** (commit `7e90b36`) — `ApiResult<T>`-kind-union detail-endpoints + ADR 0030
3. **TD-53b STÄNGD** (commit `aac9b2f`) — `ApiResult<T>`-kind-union list-endpoints + admin.ts
4. **CLAUDE.md §9.6 policy-skift** — 4h-regel ersatt av fas-regel

### TD-53b-leverans

- **4 endpoints refactorade** till `Promise<ApiResult<T>>` med explicit return-type:
  `getPipeline`, `getApplications`, `getResumes`, `getAuditLog`
- **Lokala `AuditLogResponse`-typen raderad** från `admin.ts` (ad-hoc-union ersatt med generisk `ApiResult`)
- **3 konsumenter:** `ansokningar/page.tsx`, `cv/page.tsx`, `admin/granskning/page.tsx`
- **`responseToResult` används unconditionally** — grep `parseResponse` i `lib/api/` = 0 träffar
- **Reviews:**
  - senior-cto-advisor: Variant A för `getApplications` (refactora ändå trots inga konsumenter, ADR-trohet + CCP per Martin 2017), Variant Y för test-scope (helper redan testad, DRY i test-kod, tsc + assertNever statisk exhaustiveness per Fowler 2012)
  - code-reviewer: 0 Blocker/Major, 4 Minor (informativa), 1 Nit
  - design-reviewer: 1 Major (`role="alert"`-borttagning admin ErrorBlock, konsekvens med TD-53a-policy) + 1 Minor (kommentar om dead notFound-case) fixade in-block

### Policy-skift denna session

Klas-direktiv 2026-05-11: TD-bloat = problem. Fixa allt som hör till nuvarande fas innan Fas 2.

**CLAUDE.md §9.6 uppdaterad:**

- **Ingen tidsbegränsning per touch.** 4h-regeln borttagen — utlöste TD-bloat genom tidströskel-utlyftningar
- **Fas-regel:** TD lyfts ENDAST om fyndet hör till annan fas eller saknad funktion-dependency
- **CTO-auto-follow:** CC går direkt till implementation efter CTO-beslut. Klas-STOPP endast vid strategiska frågor (fas-skifte, ADR-amendment, deploy)
- **CC rekommenderar inte vid multi-approach** — Variant A/B/C-val går till senior-cto-advisor

### Tester (full svit grön)

| Suite | Antal | Diff |
|-------|-------|------|
| Frontend vitest | **217** | oförändrat (TD-53b Variant Y) |
| Domain.UnitTests | 163 | oförändrat |
| Application.UnitTests | 204 | oförändrat |
| Architecture.Tests | 32 | oförändrat |

### Pushed commits denna session

| Commit | Scope |
|--------|-------|
| `76f758a` | `feat(web): TD-7 — Zod-DTO-validering vid HTTP-gränsen + ADR 0020` |
| `7e90b36` | `feat(web): TD-53a — ApiResult<T>-kind-union för detail-endpoints + ADR 0030` |
| `aac9b2f` | `feat(web): TD-53b — ApiResult<T>-kind-union för list-endpoints + admin.ts` |

---

## När nästa session startar

ADR 0030-migration KLAR. Inget specifikt TD-block väntar — sessionen behöver
Klas-prioritering för nästa fokus.

### Föreslagna nästa-steg

**Alternativ A: TD-genomgång inför Fas 2-stängning**

Per nya CLAUDE.md §9.6 fas-regel: alla Fas 1-TDs ska fixas innan Fas 2.
Aktiva TDs som inte blockerar nu: TD-39, TD-41, TD-51, TD-52, TD-56, TD-57,
TD-58, TD-59, TD-62. Behöver kategoriseras (Fas 1 vs Fas 2+) och de som
hör till Fas 1 ska prioriteras för stängning.

**Alternativ B: STEG-progression**

`docs/steg-tracker.md` är single source of truth — nästa STEG enligt
fas-plan i BUILD.md.

**Alternativ C: Specifik feature-touch**

Klas pekar på BUILD.md-feature som ska byggas.

---

## Föregående session-summary (referens)

**2026-05-11 ~16:00–19:00:** TD-7 + TD-53a stängda och pushade. ADR 0020 (Zod-DTO-validering vid HTTP-gränsen) + ADR 0030 (ApiResult kind-union) etablerade. 11 unchecked `as Dto`-casts borta. 3 detail-endpoints på kind-union.

**2026-05-11 ~19:00:** TD-61 audit-trail-evidence-test för `IdempotentAdminRoleSeeder` stängd. Commit `47f8deb`.

---

## Pre-existing infra (oförändrat)

| Resurs | Identifier |
|---------|-----------|
| Public URL | `https://dev.jobbpilot.se/api/ready` |
| API task-def | `jobbpilot-dev-api` (post-TD-38 apply) |
| Worker task-def | `jobbpilot-dev-worker` (post-TD-38 apply) |
| Tag (senaste) | `v0.1.2-dev` på SHA `7cde3c7` |

---

## Workflow-disciplin (uppdaterad denna session)

Per CLAUDE.md §9.2 + §9.6 (uppdaterad 2026-05-11):

1. Discovery först (denna session: kartlagt 4 endpoints + 3 konsumenter)
2. Multi-approach-val → senior-cto-advisor auto-invokeras (denna session:
   TD-53b Variant A på `getApplications`, Variant Y på test-scope)
3. **CC går direkt till implementation efter CTO-beslut** — ingen extra
   Klas-GO om motiveringen är entydig mot principer
4. Agent-reviews parallellt vid relevant scope (denna session: code-reviewer +
   design-reviewer)
5. **In-block-fix-default per fas-regel** — TD lyfts ENDAST om fyndet hör
   till annan fas eller kräver saknad funktion-dependency (4h-regel borttagen)
6. Commit + push efter Klas-diff-granskning (direct-push till main per ADR 0019)
