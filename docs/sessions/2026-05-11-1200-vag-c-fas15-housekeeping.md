---
session: Väg C — Fas 1.5 housekeeping
datum: 2026-05-11 efm
slug: vag-c-fas15-housekeeping
status: KLAR
commits:
  - feat(api): TD-55 — retro-fit PagedResult<T> på paginerade queries
  - feat(web): TD-55 — konsumera PagedResult-shape från paginerade endpoints
  - fix(api,web): TD-55 — in-block-fixar från Block A reviews
  - test(arch): TD-48 — Mono.Cecil arch-test för Trust Server Certificate=true-läckage
  - ci(infra): TD-47 — månatlig RDS CA-bundle-rotation-bevakning
  - docs(runbook): TD-50 — prod-konfig-källa för AdminBootstrap__InitialAdminEmail
---

# Väg C — Fas 1.5 housekeeping

## Mål

Stänga 4 TDs i samlad batch som housekeeping mellan Fas 1-milestone-stängning
och Fas 2-uppstart (som är blockerad till ADR 0005 + kostnadsskydd):

- TD-55 (PagedResult retro-fit)
- TD-50 (admin-bootstrap prod-konfig runbook)
- TD-48 (Architecture-test för Trust=true-läckage)
- TD-47 (RDS CA-bundle-rotation cron)

## Sammanfattning

6 commits levererade. Backend 594/594 + Frontend 150/150 grönt. 2 agent-reviews
APPROVED (code-reviewer + dotnet-architect parallellt på Block A), alla 3 Minor
in-block-fixade. Ny TD-56 lyft (ListJobAds Fas 2-paginering).

## CTO-beslut bakom design

Multi-approach-val per CLAUDE.md §9.2 → senior-cto-advisor invokerad direkt
med samlad bedömning (TD-55 commit-strategi + TD-48 impl-approach + block-
ordning). CTO-beslut entydigt motiverat:

**Beslut 1 — TD-55 commit-strategi**
- **Alt 3 vald:** två commits (backend-bundle + frontend-bundle)
- Conventional Commits scope-disciplin (api/web olika contexts)
- REP/CCP (Martin 2017): backend och frontend ändras för olika skäl
- Diff-granskbarhet > atomicitet i direct-push-praxis (ADR 0019)

**Beslut 1b — ListJobAds-deferral**
- Defer full paginering till Fas 2 JobTech-integration (TD-56)
- Lägg in `.Take(500)` hard cap som defense-in-depth nu
- YAGNI: ingen konsument kräver paginering idag, URL-kontrakt designas mot
  JobTech-API i Fas 2

**Beslut 2 — TD-48 impl-approach**
- **Alt A2 vald:** Mono.Cecil IL string-table-scan
- Alt A1 (reflection-on-fields) missar inline-strings — falsk trygghet
- Alt A3 (Roslyn) bryter NetArchTest assembly-baserad konvention
- Mono.Cecil är de facto standard för .NET IL-manipulation (Mono, JetBrains,
  Microsoft.NET.ILLink, NetArchTest internt) — inte exotisk dep

**Beslut 3 — Block-ordning**
- Seriell A → B.1 → B.2 → C (inte parallell)
- CC-sessioner effektivt single-threaded mot codebase
- Risk-first: störst risk + bug-impact först
- Cognitive load: TD-48 och TD-47 är båda säkerhets-tankar men olika nivåer

## Implementation

### Block A — TD-55 PagedResult retro-fit

**Discovery-fynd:** Frontend `GetApplicationsResult` förväntade redan
`{items, totalCount, page, pageSize}`-shape men backend returnerade bare
array. TypeScript-cast utan runtime-validering dolde buggen — TD-55 uppgraderad
från "Minor housekeeping" till "faktisk runtime-bug" i scope-bedömning.

**Backend (commit `c2f539e`):**
- `GetApplicationsQuery` + `GetResumesQuery` returnerar `PagedResult<T>`
- Handlers refactor:ade till separate count-query (CLAUDE.md §3.6)
- Empty-helper för noUser/noJobSeeker edge cases bibehåller `query.PageSize`
  så wire-shape förblir konsistent (Stripe/GitHub-pattern)
- `ListJobAdsQueryHandler` får `.Take(500)` hard cap mot DoS-vektor
- Architecture-test `PagedResultContractTests` låser kontraktet
- Integration-tester uppdaterade (`JsonValueKind.Array` → `Object` med
  items/totalCount/page/pageSize-properties)

**Frontend (commit `0b0886d`):**
- Ny `GetResumesResult`-typ + uppdaterad `getResumes()` returnerar
  `GetResumesResult | null`
- Lättviktig runtime-validering via type-guards mot kontrakts-skew
  (`res.json()` är effektivt unknown, CLAUDE.md §4.1 förbjuder any)

**Reviews:**
- **code-reviewer:** APPROVE (0 blocker, 0 major, 2 minor, 3 nit)
- **dotnet-architect:** APPROVE-WITH-FIXES (0 blocker, 3 minor, 2 nit)

**In-block-fixar (commit `5784120`):**
1. PagedResultContractTests heuristik utökad till `Page` + `PageNumber` +
   IQuery<>-filter (annars PagedResult<T> själv plockades upp som false
   positive — fångad vid första körning)
2. Generisk `isPagedResult<T>` i `lib/types/paged.ts` ersätter duplicerade
   per-endpoint type-guards
3. ListJobAdsQueryHandler MaxItems får TODO-kommentar + TD-56-referens

### Block B.1 — TD-48 Mono.Cecil arch-test (commit `9f33897`)

`tests/JobbPilot.Architecture.Tests/ConnectionStringLeakageTests.cs`:
- Theory över Api/Worker/Infrastructure-assemblies
- Walka `AssemblyDefinition.Modules → Types → Methods → Body.Instructions`
- Filtrera `OpCodes.Ldstr` + check string-operand mot "Trust Server Certificate=true"
- Migrate explicit exkluderad (ForMigrate har Trust=true by design)
- Separat sanity-test asserterar att Migrate-assembly faktiskt innehåller
  Trust=true — om den någonsin slutar göra det ska arch-testet uppdateras

Mono.Cecil 0.11.5 lagt till Directory.Packages.props + csproj. Test-only.
Transitiv via NetArchTest redan men explicit ref tillåter direkt `using
Mono.Cecil`.

Migrate-projekt även lagt till som ProjectReference i Architecture.Tests
(saknades — nödvändigt för typeof-marker till Migrate-assembly).

### Block B.2 — TD-47 GH Actions CA-bundle-cron (commit `f9313af`)

`.github/workflows/rds-ca-bundle-check.yml`:
- Cron 03:00 UTC första dagen varje månad + workflow_dispatch
- `curl` AWS upstream-bundle + `sha256sum` diff mot lokal
- Vid diff: öppna GitHub-issue (labels: td-47, security) med komplett
  7-stegs rotation-procedur
- Idempotent — skippar om öppet td-47-issue redan finns

Månatlig kadens vald över kvartalsvis: AWS-annonseringar kommer typiskt
med 12+ månaders varsel, månadsvis ger snabbare detekt vid oannonserad
rotation till försumbar kostnad.

### Block C — TD-50 admin-bootstrap runbook (commit `a9ca126`)

`docs/runbooks/admin-bootstrap.md` (ny):
- Källa per miljö (local/dev/staging/prod) som tabell
- Förbudet motiverat (PII i git, privilege-escalation, cross-env-läckage)
- 4-stegs setup (Secrets create → ECS task-def secrets-block → IAM grants
  → verify deploy)
- Rotation-procedur när admin byts
- Lokal dev-bypass via appsettings.Local.json (gitignored)

`AdminBootstrapOptions.cs` får utökad `<remarks>`-sektion som förbjuder
appsettings.json-källa i prod och pekar mot runbook.

## Reviews-rapporter

- `docs/reviews/2026-05-11-vag-c-td55-code-reviewer.md`
- `docs/reviews/2026-05-11-vag-c-td55-dotnet-architect.md`

## Tester

| Suite | Före session | Efter session | Tillkomst |
|-------|--------------|---------------|-----------|
| Domain UnitTests | 157 | 157 | — |
| Application UnitTests | 194 | 196 | +2 (TotalCount-independent-av-PageSize + ListJobAds MaxItems-cap) |
| Architecture Tests | 24 | 31 | +7 (3 PagedResultContractTests + 4 ConnectionStringLeakage) |
| Worker IntegrationTests | 26 | 26 | — |
| Api IntegrationTests | 178 | 178 | 0 (3 uppdaterade) |
| Migrate UnitTests | 6 | 6 | — |
| **Backend totalt** | **585** | **594** | **+9** |
| Frontend Vitest | 150 | 150 | — |

## Lärdomar

- **PagedResultContractTests false positive:** Min första heuristik
  matchade `PageNumber + PageSize` på alla typer (inkl PagedResult<T>
  själv som har Page + PageSize properties). Krävs IQuery<>-filter som
  första steg. Test-design-lärdom: reflection-baserade arch-tester behöver
  *explicit* "is-a-query"-filter, inte bara strukturella property-heuristiker.

- **Mono.Cecil för IL-scan är pragmatisk:** Det är inte exotisk dep — det
  är de facto standard i .NET-världen och redan transitiv via NetArchTest.
  Att explicit ref:a är dokumentation av att vi använder det.

- **TD-55 var inte housekeeping — det var en latent runtime-bug:** Discovery
  avslöjade typskew som dolts av TypeScript-cast utan runtime-validering.
  Lärdom: housekeeping-TDs förtjänar discovery innan scope-bedömning. Det
  som ser ut som "städning" kan vara dolt bug-fix.

- **CTO-beslut för commit-strategi värt arbetet:** Alt 3 (två commits) gav
  granskbarare diffar och bättre Conventional Commits scope-disciplin än
  Alt 1 (bundle). Mindre commits stödjer ADR 0019 direct-push-modell.

- **`.Take(500)` som defense-in-depth-defer:** Pragmatisk pattern för att
  acceptera scope-deferral utan att lämna säkerhetshål öppet. Kombinerat
  med TODO-kommentar + ny TD som ankarpunkt.

- **Architectural defense-in-depth fångar Fas 6-regression preemptivt:**
  Trust=true-arch-test skyddar Api/Worker/Infrastructure-assemblies mot
  framtida regression som factory-unit-testet inte fångar. Liknande
  pattern bör övervägas för andra säkerhetskritiska strings (t.ex.
  hardkodade session-tokens, API-keys).

## Aktiva TDs efter denna session

10 aktiva TDs (var 13 vid start, 4 stängda denna session, 1 ny):

- TD-39 (error-summary-mönster)
- TD-40 (path-equality regression)
- TD-41 (Select-komponent-konvention)
- TD-42 (touch-target projektbrett)
- TD-49 (HstsOptions unit-test — blockerad: kräver JobbPilot.Api.UnitTests-projekt)
- TD-51 (admin-läs audit-logging Fas 6)
- TD-52 (admin-endpoint rate-limit Fas 6)
- TD-53 (frontend kind-union standardisering >4h)
- TD-54 (text-text-tertiary kontrast projektbrett)
- **TD-56 (ListJobAds Fas 2-paginering) — NY**

## Pre-existing infra (oförändrat)

| Resurs | Identifier |
|---------|-----------|
| Public URL | `https://dev.jobbpilot.se/api/ready` |
| API task-def | `jobbpilot-dev-api` |
| Worker task-def | `jobbpilot-dev-worker` |
| Tag (senaste) | `v0.1.2-dev` på SHA `7cde3c7` |

## Nästa session

Klas väljer vägen framåt:
- **Väg A — Fas 2-prereq:** ADR 0005 (go-to-market) + kostnadsskydd (kräver webb-Claude)
- **Väg B — TDs-cleanup å la carte:** a11y-pass (TD-54 + TD-42) eller annat
- **Väg D — Block A nice-to-have:** Wire-shape integration-test för PagedResult-kontraktet

Startprompt sparas separat eller embedded i nästa session.

## Cost

Oförändrat ~$79.65/mån. Block B.2 (GH Actions cron) kostar några sekunder
Actions-minuter per månad — försumbart inom free-tier.
