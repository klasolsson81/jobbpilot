# Current work — JobbPilot

**Status:** **TD-7 + TD-53a STÄNGDA OCH PUSHED 2026-05-11 — TD-53b pending (separat batch).** ADR 0020 (Zod-DTO-validering) + ADR 0030 (ApiResult kind-union) etablerar dubbel ACL-yta: shape vid HTTP-gränsen + outcome-semantik mot UI. 11 unchecked `as Dto`-casts borta. 3 detail-endpoints på kind-union.
**Senast uppdaterad:** 2026-05-11
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu — TD-53b väntar (list-endpoints + admin.ts-migration)

Stationär-CC-session 2026-05-11 ~16:00–19:00. Tre stora arbets-block:

1. **TD-7 STÄNGD** (commit `76f758a`) — Zod-DTO-validering vid HTTP-gränsen + ADR 0020
2. **TD-53a STÄNGD** (commit `7e90b36`) — `ApiResult<T>`-kind-union detail-endpoints + ADR 0030
3. **TD-53b ÖPPEN** — list-endpoints + admin.ts-migration (separat batch per 4h-regeln)

### TD-7-leverans

- **ADR 0020** etablerar Parse-don't-validate + Zod som ACL-verktyg (Alexis King 2019, Evans 2003 kap. 14)
- **`lib/dto/_helpers.ts`** — `parseResponse<T>` + `pagedResult`-factories + `redactIssues` (PII-skydd: `received` redaktas ur Zod-issues)
- **`lib/dto/{me,applications,resumes,admin,auth,common}.ts`** — Zod-schemas per domän, typer härledda via `z.infer`
- **11 call-sites refactorade:** `lib/auth/session.ts`, `app/api/me/route.ts`, `lib/api/*.ts` (4 filer), `lib/auth/actions.ts` (3 sites), `lib/actions/{applications,resumes}.ts`
- **`lib/types/*.ts`** behållna som tunna re-exports (bakåtkompatibla); `lib/types/paged.ts` raderad
- **52 nya unit-tester**; 18 test-filer / 217 tests grönt
- **Reviews:** code-reviewer (1 Major fixad in-block), security-auditor (2 Minor fixade in-block)
- **TD-62 lyft** som framtida supersession-kandidat (OpenAPI-codegen Fas 2+)

### TD-53a-leverans

- **ADR 0030** etablerar `ApiResult<T>`-discriminated-union för API-outcome-semantik (CCP/OCP Martin 2017, value object Evans/Fowler)
- **`lib/dto/_helpers.ts` utökat:** `ApiResult<T>` + `responseToResult` + `assertNever`
- **3 endpoints refactorade:** `getMyProfile`, `getApplicationById`, `getResumeById` (de två sista med `includeNotFound: true`)
- **3 konsumenter uppdaterade:** `mig/page.tsx`, `ansokningar/[id]/page.tsx`, `cv/[id]/page.tsx`
- **11 nya tester**; total 217/217 grönt (oförändrat efter fix-cycler)
- **Reviews:**
  - senior-cto-advisor: Variant A vald över C (hybrid CCP-brott) och B (`{ok, error}`), scope-split TD-53a/b per 4h-regeln
  - code-reviewer: 0 Blocker/Major; 2 Minor + 3 Nit fixade in-block
  - design-reviewer: **2 Blocker + 3 Major + 3 Minor** fixade in-block (focus-ring via Button asChild, role=alert borttaget, specifik fel-rubrik, Button-affordans, notFound-onboarding-copy separerat från fel-copy i mig/page)
  - design-reviewer re-review: Approved
- **`getServerSession()` undantaget** — `null` är legitim auth-pipeline-semantik (ADR 0017)

### Tester (full svit grön)

| Suite | Antal | Diff |
|-------|-------|------|
| Frontend vitest | **217** | +52 (TD-7) + +11 (TD-53a) — total +63 |
| Domain.UnitTests | 163 | oförändrat |
| Application.UnitTests | 204 | oförändrat |
| Architecture.Tests | 32 | oförändrat |

### Pushed commits

| Commit | Scope |
|--------|-------|
| `76f758a` | `feat(web): TD-7 — Zod-DTO-validering vid HTTP-gränsen + ADR 0020` |
| `7e90b36` | `feat(web): TD-53a — ApiResult<T>-kind-union för detail-endpoints + ADR 0030` |

---

## När nästa session startar — TD-53b

**Scope:** Refactor 3 list-endpoints + migrera `admin.ts:AuditLogResponse` till generic `ApiResult<T>`.

### Endpoints att refactora

| Endpoint | Nuvarande | Mål |
|---|---|---|
| `getPipeline()` | `Promise<PipelineGroupDto[]>` (tom-array-fallback) | `Promise<ApiResult<PipelineGroupDto[]>>` |
| `getApplications(page, pageSize, status?)` | `Promise<GetApplicationsResult \| null>` | `Promise<ApiResult<GetApplicationsResult>>` |
| `getResumes(page, pageSize)` | `Promise<GetResumesResult \| null>` | `Promise<ApiResult<GetResumesResult>>` |
| `getAuditLog(filter)` | `Promise<AuditLogResponse>` (egen union) | `Promise<ApiResult<AuditLogPagedResult>>` |

`includeNotFound: false` (default) på alla — list-endpoints saknar genuin notFound-semantik (tom array är giltigt svar).

### Konsumenter att uppdatera

- `app/(app)/ansokningar/page.tsx` — använder `getPipeline()`
- `app/(app)/cv/page.tsx` — använder `getResumes()`
- `app/(admin)/admin/granskning/page.tsx` + komponenter — använder `getAuditLog()`

### Öppen konvention-fråga (från code-reviewer TD-53a)

`unauthorized`-placering: `mig/page.tsx`-mönster (early redirect före switch) vs detail-pages-mönster (switch-inom). Båda används i TD-53a. För list-endpoint-konsumenter — välj en konvention.

CC-rekommendation utan ny CTO-call: detail-pages-mönster (switch-inom) är default. `mig/page.tsx` är legitim avvikelse där partial-render motiverar tidig early-return. Lyfts till senior-cto-advisor endast om Klas vill ha formell konvention-ADR.

### Scope-bedömning

~4-6h CC-tid (4 endpoints + 4 konsumenter + tester + reviews). Inom 4h-regeln övre gräns.

### TD-status

**Stängda denna session:** TD-7, TD-53a (+ original TD-53 ersatt).
**Lyfta denna session:** TD-62 (OpenAPI-codegen Fas 2+), TD-53a, TD-53b.
**Aktiva som inte blockerar TD-53b:** TD-39, TD-41, TD-51, TD-52, TD-56, TD-57, TD-58, TD-59.

---

## Föregående session-summary (referens) — Väg B TD-61

**2026-05-11 ~19:00:** TD-61 audit-trail-evidence-test för `IdempotentAdminRoleSeeder` stängd via discovery-driven Alt A. XML-doc korrigerad + integration-test mot ILogger (rätt sink). Backend 612 → 615. Commit `47f8deb`.

---

## Pre-existing infra (oförändrat)

| Resurs | Identifier |
|---------|-----------|
| Public URL | `https://dev.jobbpilot.se/api/ready` |
| API task-def | `jobbpilot-dev-api` (post-TD-38 apply) |
| Worker task-def | `jobbpilot-dev-worker` (post-TD-38 apply) |
| Tag (senaste) | `v0.1.2-dev` på SHA `7cde3c7` |

---

## Workflow-disciplin (oförändrad)

Per CLAUDE.md §9.2 + §9.6:

1. Discovery först (denna session: kartlagt frontend-DTO-stack + alla T|null-konsumenter)
2. Multi-approach-val → senior-cto-advisor auto-invokeras (denna session: TD-7 Variant A vald, TD-53 Variant A vald med scope-split)
3. STOPP-rapport till Klas innan implementation om CTO osäker / fas-strategiskt
4. Agent-reviews parallellt vid relevant scope (denna session: code-reviewer + security-auditor på TD-7, code-reviewer + design-reviewer på TD-53a + design-reviewer re-review)
5. In-block-fix-default per 4h-regel (alla fynd fixade in-block, 1 ny TD lyft som genuin supersession-kandidat)
6. Commit + push efter Klas-diff-granskning (direct-push till main per ADR 0019)
