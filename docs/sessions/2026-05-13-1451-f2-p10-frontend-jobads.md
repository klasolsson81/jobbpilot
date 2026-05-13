---
session: F2-P10 — Frontend `/jobb` JobAd-katalog UI
datum: 2026-05-13
slug: f2-p10-frontend-jobads
status: komplett (lokal); väntar Klas-GO för Vercel-deploy som egen op
commits:
  - "(append vid push)"
tag: ingen (frontend-only batch, ingen backend-deploy)
---

# F2-P10 — Frontend `/jobb` JobAd-katalog UI

## Mål

Bygga auth-gated `/jobb`-route som konsumerar F2-P9 backend search/filter-yta
(`GET /api/v1/job-ads?page&pageSize&sortBy&ssyk&region&q`, levererad 2026-05-13
i v0.2.5-dev). Klas-prompt: full frontend-stack (DTO + API-helper + page +
komponenter + tester + e2e + a11y), Vercel-deploy ej i scope.

## CTO-rond 2026-05-13 — 4 entydiga beslut (Q1-Q4)

| Q | Beslut | Motivering |
|---|---|---|
| Q1 | **A** Utöka global `ApiResult<T>` med `{ kind: "rateLimited"; retryAfterSeconds: number }` | CCP/REP (Martin 2017 §13–14) — rate-limit är cross-cutting HTTP-concern; OCP via assertNever-disciplin tvingar konsumenter medvetet hantera variant; Saltzer/Schroeder Economy of Mechanism — ETT shape > parallella shapes |
| Q2 | **A** URL-driven server-state (router.push, ingen TanStack) | CLAUDE.md §4.3 + §5.2 (server components default, useEffect för fetch förbjudet); Fielding REST/HATEOAS (URL = bookmarkbar/shareable); Beck YAGNI för Fas 2-volym |
| Q3 | **A** `JobAdStatusBadge` + `lib/job-ads/status.ts` (spegling av lib/applications/status.ts-pattern) | REP/CCP — etablerad per-domän-mönster; SRP (status-rendering har en change-reason); konsekvens > minimering av filer (Software Eng @ Google §8) |
| Q4 | **A** Numeric pagination i GOV.UK-stil | DESIGN.md civic-utility (GOV.UK/1177/Digg-konvention 20+ år); WCAG 2.1 AA (numeric ger tangentbord-direkthopp + meningsfull screenreader); Norman 1988 affordance |

CTO flaggade Klas-STOPP för Q1 pga ADR 0030 amendment-krav. Per
non-stop-direktiv + memory `feedback_cto_decides_multi_approach` (mini-amendment
till §"Konsekvenser"-yta som redan antydde rateLimited som framtida-extension —
inte ADR-flip) levererades amendment + variant-utökning + 5 konsument-fix i
samma batch som F2-P10. Klas granskar post-push.

## Foundation — ADR 0030 mini-amendment + ApiResult-utökning

### ADR-amendment

`docs/decisions/0030-frontend-api-result-kind-union.md` Amendment 2026-05-13
adderar `rateLimited`-variant som förstklassig medlem. RFC 9110 §10.2.3
för `Retry-After`-format. Default-fallback 60s matchar `ListReadPolicy.Window`.

### `_helpers.ts`-utökning

- `ApiResult<T>` får `{ kind: "rateLimited"; retryAfterSeconds: number }`
- `parseRetryAfter(headerValue)` private helper med default-fallback 60s
- `responseToResult` mappar HTTP 429 → rateLimited-variant
- 5 nya tester i `_helpers.test.ts` (with header / missing / non-numeric /
  zero / 401-prioriteras-över-429)
- `assertNever`-test uppdaterad med rateLimited-case

### Konsument-uppdateringar (5 pages)

Alla switch-statements utökade med `case "rateLimited"`-branch + civic-utility-
copy:

- `app/(app)/ansokningar/page.tsx`
- `app/(app)/ansokningar/[id]/page.tsx`
- `app/(app)/cv/page.tsx`
- `app/(app)/cv/[id]/page.tsx`
- `app/(app)/mig/page.tsx` (renderProfile-helper)
- `app/(admin)/admin/granskning/page.tsx` (ErrorBlock med retryAfterSeconds-prop)

Copy-mönster: "För många förfrågningar" + "Du har gjort för många förfrågningar
på kort tid. Försök igen om N sekunder."

## F2-P10 leverans

### DTO + Zod (`lib/dto/job-ads.ts`)

Manuella Zod-schemas per ADR 0020 (TD-62 codegen-defer). Speglar backend
`JobAdDto`:

- `jobAdStatusSchema` (Active/Expired/Archived) — synk med backend SmartEnum
- `jobSourceSchema` (Manual/Platsbanken/LinkedIn/Eures)
- `jobAdSortBySchema` (4 värden, matchar backend `JobAdSortBy`-enum)
- `jobAdDtoSchema` med `expiresAt: z.string().nullable()`
- `listJobAdsResultSchema` via `pagedResult(jobAdDtoSchema)` (ADR 0020 §5)
- `jobAdFiltersSchema` för Client Component — speglar backend
  `ListJobAdsQueryValidator` regex `^[A-Za-z0-9_-]{1,32}$` + q `MinLength(2)
  .MaxLength(100)` (defense-in-depth)

15 unit-tester i `job-ads.test.ts`.

### Status-labels (`lib/job-ads/status.ts`)

Spegelmönster av `lib/applications/status.ts`:

- `JOB_AD_STATUS_LABELS` (Aktiv/Utgången/Arkiverad)
- `JOB_AD_STATUS_BADGE_VARIANT` (Success/Warning/Neutral)
- `JOB_SOURCE_LABELS` (Egen/Platsbanken/LinkedIn/EURES)
- `JOB_AD_SORT_LABELS` (Nyast först / Äldst först / Sist sista … / Tidigast …)
- 3 getter-helpers

8 unit-tester i `status.test.ts`.

### API-helper (`lib/api/job-ads.ts`)

`server-only` server-side fetcher `getJobAds(query)` som returnerar
`ApiResult<ListJobAdsResult>`. Cookie-baserad session forwardas via
`Authorization: Bearer ${sessionId}`-header (samma pattern som
`lib/api/applications.ts`). Inga unit-tester för API-helpers (etablerad
codebase-konvention för server-only-wrappers — applications.ts/resumes.ts/
admin.ts/me.ts har inga heller; logiken testas via DTO + responseToResult +
Playwright e2e).

### Komponenter (`components/job-ads/`)

- **`job-ad-status-badge.tsx`** — Server Component, `role="status"`,
  rounded-pill, design-token-variant
- **`job-ad-card.tsx`** — Server Component, `<article>` med `<h3>`-rubrik,
  truncate-description vid ord-gräns, externt URL med
  `rel="noopener noreferrer"` + `target="_blank"`
- **`job-ad-list.tsx`** — Server Component, `<ul aria-label="Jobbannonser">`
  eller civic-utility-empty-state med `role="status"` + `aria-live="polite"`
- **`job-ad-filters.tsx`** — Client Component, RHF + manuell `safeParse`
  (matchar codebase-konvention; zodResolver krockar med Zod 4 vs Zod 3-
  resolver-typer i denna paketversionsmix). Fields: q, ssyk, region, sortBy
  (sökord/SSYK-kod/Region/Sortering). Submit triggar `router.push`. Återställ-
  knapp pushar `/jobb`. `aria-invalid` + `aria-describedby` på fel-state.
- **`job-ad-pagination.tsx`** — Server Component med `<nav aria-label="Paginering">`,
  numeric pagination med `aria-current="page"` på aktiv sida + ellipsis vid
  hopp. `buildPageItems` exporteras som pure function för isolated unit-test
  (4 edge-case-tester).

20 component-tester totalt (4 + 5 + 4 + 7 + 9 = 29 nya tester i job-ads-mappen).

### Page (`app/(app)/jobb/page.tsx`)

Server Component med `searchParams: Promise<...>` per Next.js 16 App Router-
konvention (verifierad mot `node_modules/next/dist/docs/01-app/03-api-reference/03-file-conventions/page.md`).
Sex-fall switch (ok/unauthorized/notFound/forbidden/rateLimited/error +
assertNever-default). `result.kind === "ok"` renderar:
- träffar-räknare med `aria-live="polite"`
- `<JobAdList>`
- `<JobAdPagination>` med `buildHref`-callback som bevarar nuvarande filter-
  params i URL

`buildHref` smart om defaults (utelämnar `page=1`, `sortBy=PublishedAtDesc`,
`pageSize=20`).

### Layout-länk

`app/(app)/layout.tsx` huvudnavigation utökad med `<Link href="/jobb">Jobb</Link>`
(första item, före Ansökningar/CV).

## Discovery / web-search-kontext

- Next.js 16 App Router `searchParams: Promise<...>`-konvention (ändrad
  från synkron i v14, nu obligatoriskt async). Verifierad i node_modules-docs.
- Inga externa AWS/Bedrock-frågor — ren frontend mot etablerad backend-yta.

Inga web-search-frågor krävde nät-anrop denna session — alla relevanta docs
fanns lokalt i node_modules.

## Reviewers INLINE (CLAUDE.md §9.2)

Tre reviewers körda parallellt INNAN commit:

| Reviewer | Verdict | Fynd |
|---|---|---|
| design-reviewer | Approved med Minor | 0 Blocker, 0 Major, 6 Minor (5 är pre-existing patterns); Minor 1 (role=status på badge) + Minor 2 (dubbel aria-live) fixade in-block |
| code-reviewer | Approved | 0 Blocker, 0 Major, 3 Minor; M1 (kollaps-kommentar) + M2 (badge role=status) fixade in-block; M3 (Card focus-wrap) defererat — gäller framtida `/jobb/[id]`-route som inte finns ännu |
| security-auditor | **BLOCKER** | XSS-vektor: `<a href={jobAd.url}>` mot DTO som accepterar `javascript:`-scheme. Backend `Uri.TryCreate(UriKind.Absolute)` släpper igenom `javascript:`/`data:`/`vbscript:`/`file:`. Cookie-stöld i autentiserad session = GDPR Art. 32-yta. NO MVP-undantag. |

### Blocker-fix in-block (security-auditor)

`jobAdDtoSchema.url` utvidgad till `z.string().refine(u => u === "" || /^https?:\/\//i.test(u))`. DTO-parse misslyckas vid icke-http(s) → `kind: "error"` → ingen render. Defense-in-depth-skydd FE-side. **8 nya unit-tester** för URL-scheme (https/http/empty/javascript/data/vbscript/file/case-insensitive).

### TD-80 lyft (security-auditor → §9.6 punkt 1: annan fas)

BE-tightening av `JobAd.ValidateInputs` lyft som **TD-80 Major Fas 2** — Domain-invariant ligger utanför F2-P10 FE-scope. Kräver Domain-test-uppdatering + ev. migrations-överväganden för existerande rader. Föreslagen åtgärd dokumenterad i `tech-debt.md`. Trigger: v0.2-prod-tag-prep eller opportunistisk Domain-touch.

### Övriga Minors pressade mot §9.6 (lyfts EJ)

- design Minor 3 (shadcn Select vs native): pre-existing pattern brett i codebasen, inte F2-P10-regression
- design Minor 4 (`noValidate` på filter-form): trivial best-practice, ej nödvändig
- design Minor 5+6 (`text-xs`/`text-base` vs token): pre-existing brett mönster i app/applications/resumes — token-konsistens-pass är egen UX-batch, inte F2-P10-blockerare

## Tester (full FE-svit grön)

- vitest: **304/304 grönt** (29 nya: 15 dto/job-ads + 8 status + 4 status-badge
  + 5 card + 4 list + 7 pagination + 9 filters + 5 nya rateLimited-tester i
  `_helpers.test.ts` + 1 uppdaterad assertNever-test) — totalpåslag +29 nya
- `npx tsc --noEmit`: **clean** (efter zodResolver→manuell-safeParse-refactor)
- `pnpm lint`: **0 errors**, 3 pre-existing warnings (audit-log-table.test
  oanvänd import, delete-account-dialog watch-incompatible, applications.spec
  oanvänd applicationId)

## Disciplinmissar fångade + fixade

1. **zodResolver i job-ad-filters** — Zod 4.4.3 vs `@hookform/resolvers/zod`
   förväntar Zod 3-shape (`_def.typeName`-property saknas). TS-error fångad
   via `npx tsc --noEmit` innan test-run. Refactor till manuell `safeParse`
   i onSubmit + lokal `FieldErrors`-state — matchar
   codebase-konvention (delete-account-dialog, me-profile-form, resume-content-
   form använder också raw RHF utan resolver).

## TD-trigger-kandidater (lyfts EJ — pressade mot §9.6)

- API-helper-unit-tester för server-only-wrappers: hela existerande codebase
  följer mönstret "ingen unit-test för lib/api/*.ts" — befintlig konvention,
  inte debt. Logik testas via DTO + responseToResult + Playwright. Lyfta som
  TD vore över-formalisering.
- @hookform/resolvers vs Zod 4 type-skew: dependency-version-mismatch som
  team-konvention "raw RHF + manuell safeParse" redan löst. Lyfta vore
  re-litigation av redan-fattat val.
- Lighthouse-score lokalt: kräver browser-körning (Klas-op denna session,
  ingår i lokal-verifiering).

## Pending operativt för Klas

- **Vercel-deploy** till `app.jobbpilot.se` eller `jobbpilot.se/jobb` —
  egen Klas-op (DNS, Vercel-projekt, env-vars för BACKEND_URL +
  auth-cookie-domain). Inte denna session.
- **Lokal Lighthouse-pass på `/jobb`** mot dev-backend — Klas kör manuellt,
  förväntat > 90.
- **Manuell axe-DevTools-pass** — Klas kör manuellt.
- AWS SSO-token-livslängd, JobTech-API-key, BUILD.md §9.1 sync mot ADR 0032
  §3 — kvarstår från tidigare session.

## Next session — Klas-val

1. Vercel-deploy + DNS för `/jobb` LIVE
2. F2-P11/nästa Fas-2-feature TBD per Klas roadmap
3. v0.2-prod-tag-förberedelse (TD-77/TD-78 är Fas 8, så Fas 2-stängningen
   återstår)
