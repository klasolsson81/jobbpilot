# Current work — JobbPilot

**Status:** **VÄG C — FAS 1.5 HOUSEKEEPING LEVERERAD 2026-05-11 efm.** TD-55, TD-50, TD-48, TD-47 stängda i samlad batch (6 commits) ovanpå Fas 1-milestone-stängningen. Ny TD-56 (ListJobAds Fas 2-paginering) lyft som konsekvens av TD-55-CTO-beslut. **Stängda TDs totalt:** TD-15, TD-31, TD-38, TD-43, TD-44, TD-45, TD-46, TD-47, TD-48, TD-50, TD-55. **Aktiva TDs:** TD-39, TD-40, TD-41, TD-42, TD-49, TD-51, TD-52, TD-53, TD-54, TD-56. **Nästa fas:** Fas 2 (JobTech Integration) — blockerad till ADR 0005 go-to-market + kostnadsskydd.
**Senast uppdaterad:** 2026-05-11
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu

**Stationär-CC-session 2026-05-11 ~12:00 — VÄG C FAS 1.5 HOUSEKEEPING.** Bundle:
TD-55 PagedResult retro-fit + TD-48 Mono.Cecil arch-test + TD-47 CA-bundle-cron + TD-50 admin-bootstrap runbook. CTO-beslut entydigt motiverat: Alt 3 commit-strategi (backend+frontend separat), Alt A2 (Mono.Cecil) över reflection/Roslyn, seriell ordering A→B.1→B.2→C.

### Sub-block-summary

| Block | Scope | Commits | Status |
|-------|-------|---------|--------|
| Discovery | TD-55/48/47 file-state-kartläggning | (no commit) | ✓ |
| CTO-beslut | Alt 3 + A2 + seriell ordning | (in chat) | ✓ |
| Block A Commit 1 | TD-55 backend (PagedResult retro-fit + arch-test + integration-tester) | `c2f539e` | ✓ |
| Block A Commit 2 | TD-55 frontend (GetResumesResult + type-guard + cv-page) | `0b0886d` | ✓ |
| Block A reviews | code-reviewer (APPROVE) + dotnet-architect (APPROVE-WITH-FIXES) parallellt | (reports) | ✓ |
| Block A Commit 3 | In-block-fixar: PagedResultContractTests Page+PageNumber, generisk isPagedResult<T>, MaxItems TD-56 ref | `5784120` | ✓ |
| Block B.1 | TD-48 Mono.Cecil ConnectionStringLeakageTests + Migrate-exkludering | `9f33897` | ✓ |
| Block B.2 | TD-47 .github/workflows/rds-ca-bundle-check.yml (månatlig hash-diff) | `f9313af` | ✓ |
| Block C | TD-50 admin-bootstrap.md runbook + AdminBootstrapOptions remarks | `a9ca126` | ✓ |

### Discovery-fynd som påverkade scope

**TD-55 var inte bara housekeeping.** Frontend `GetApplicationsResult`-typ förväntade redan `{items, totalCount, page, pageSize}`-shape men backend returnerade bare array. TypeScript-cast utan runtime-validering dolde buggen — ingen konsument anropade `getApplications` idag så typskew var latent. Uppgraderar TD-55 från "Minor housekeeping" till "faktisk runtime-bug" i scope-bedömning.

**TD-47 + TD-48 trigger:** AWS RDS CA-bundle är frusen lokalt i `infra/certs/rds-global-bundle.pem`. Vid AWS-rotation utan upptäckt → SSL VerifyFull fail → DB-anslutning tappas hårt utan deploy-möjlighet. Båda TDs är defense-in-depth mot samma rotation-event på olika nivåer.

### Reviews i Block A

| Reviewer | Verdict | Fynd | Status |
|----------|---------|------|--------|
| code-reviewer | APPROVE | 0 blocker, 0 major, 2 minor, 3 nit | Båda Minor fixade in-block, nits lämnade |
| dotnet-architect | APPROVE-WITH-FIXES | 0 blocker, 3 minor, 2 nit | Alla 3 Minor fixade in-block, nits lämnade |

**In-block-fixar applicerade (4h-regel):**
1. PagedResultContractTests heuristik utökad till `Page` + `PageNumber` + IQuery<>-filter (annars PagedResult<T> själv plockades upp som false positive — fångad vid första körning)
2. Generisk `isPagedResult<T>` i `lib/types/paged.ts` ersätter duplicerade per-endpoint type-guards
3. ListJobAdsQueryHandler MaxItems får TODO-kommentar mot Fas 2 + TD-56-referens

### Nya commits (denna session)

| SHA | Beskrivning |
|-----|-------------|
| `a9ca126` | docs(runbook): TD-50 — prod-konfig-källa för AdminBootstrap__InitialAdminEmail |
| `f9313af` | ci(infra): TD-47 — månatlig RDS CA-bundle-rotation-bevakning |
| `9f33897` | test(arch): TD-48 — Mono.Cecil arch-test för Trust Server Certificate=true-läckage |
| `5784120` | fix(api,web): TD-55 — in-block-fixar från Block A reviews |
| `0b0886d` | feat(web): TD-55 — konsumera PagedResult-shape från paginerade endpoints |
| `c2f539e` | feat(api): TD-55 — retro-fit PagedResult<T> på paginerade queries |

### Aktiva TDs efter denna session

- **TD-39:** Error-summary-mönster för stora formulär
- **TD-40:** Path-equality regression-bevakning
- **TD-41:** Select-komponent-konvention native vs shadcn
- **TD-42:** Touch-target projektbrett <44px (WCAG 2.5.5)
- **TD-49:** HstsOptions unit-test (blockerad — kräver JobbPilot.Api.UnitTests-projekt)
- **TD-51:** Admin-läs-aktioner audit-logging (Fas 6 GDPR Art. 30)
- **TD-52:** Admin-endpoint dedikerad rate-limit-policy (Fas 6)
- **TD-53:** Frontend API-resultatformat kind-union standardisering (>4h scope)
- **TD-54:** text-text-tertiary kontrast-brott projektbrett
- **TD-56:** ListJobAdsQuery full paginering (Fas 2 JobTech-integration) — NY

### Tester (full svit grön)

- **Backend:** 594/594 (157 Domain + 196 Application UnitTests + 31 Architecture + 26 Worker + 178 Api Integration + 6 Migrate UnitTests)
- **Frontend Vitest:** 150/150 oförändrade
- **Backend tillkomst:** +9 sedan föregående HEAD (b7eec42 → a9ca126)
  - +2 Application UnitTests (TotalCount-independent-av-PageSize regression-test + ListJobAds MaxItems-cap)
  - +4 Architecture: 3 PagedResultContractTests + 4 ConnectionStringLeakage (3 Theory + 1 Migrate-sanity) − 0 borttagna
  - +3 Api Integration: PagedResult-shape-assertions ersätter array-assertions (samma antal tester, uppdaterad shape)

### Föregående session-summary (referens)

**Stationär-CC 2026-05-11 0940→efm:** Fas 1-milestone-stängning — admin-audit-vy + roll-claim-flow + admin-seeder. HEAD = `b7eec42` vid session-start för Väg C.

---

## Föregående session — Fas 1 milestone-stängning (referens)

### Fas 1-stängning sub-block-summary

| Steg | Scope | Status |
|------|-------|--------|
| Discovery | Audit-log-schema + auth-roll-modell + frontend-route | ✓ |
| CTO-beslut | A1 (per-request claims) + B1 (IdempotentAdminRoleSeeder) + C1 (/admin/granskning) | ✓ |
| Backend impl | A1 + B1 + AuthZ-policy + AdminEndpoints + 7 integration-tester | ✓ |
| Frontend impl | (admin)/granskning + 3 komponenter + 17 komponent-tester | ✓ |
| 5 agent-reviews | code-reviewer + security-auditor + dotnet-architect + design-reviewer × 2 | ✓ Alla APPROVED |
| CTO-triage | 12 in-block-fixar + 6 nya TDs + ADR 0028 | ✓ |
| ADR 0028 | Admin authorization defense-in-depth | ✓ |

### TDs stängda totalt (kumulativt)

- TD-15 (Resume-formulär a11y) — Block A1 (laptop-session 2026-05-11 0540→0940)
- TD-31 (UseHttpsRedirection env-gate-test) — Block A3
- TD-38 (Trust Server Certificate hardening) — Block A4
- TD-43 (komponent-tests för LoginForm + MeProfileForm + ResumeContentForm) — parallell CC
- TD-44 (HSTS-header anti-regression-test) — laptop-session
- TD-45 (LoginForm focus-flytt vid state.error) — laptop-session
- TD-46 (extrahera pathToElementId per-domän) — laptop-session
- TD-47 (RDS CA-bundle-rotation-bevakning) — **denna session, Block B.2**
- TD-48 (Architecture-test för Trust=true-läckage) — **denna session, Block B.1**
- TD-50 (Prod-konfig-källa för AdminBootstrap__InitialAdminEmail) — **denna session, Block C**
- TD-55 (PagedResult retro-fit) — **denna session, Block A**

---

## Pre-existing infra (oförändrat)

| Resurs | Identifier |
|---------|-----------|
| Public URL | `https://dev.jobbpilot.se/api/ready` |
| API task-def | `jobbpilot-dev-api` (post-TD-38 apply) |
| Worker task-def | `jobbpilot-dev-worker` (post-TD-38 apply) |
| Tag (senaste) | `v0.1.2-dev` på SHA `7cde3c7` |

---

## När nästa session startar

### Alternativ framåt

**Väg A — Fas 2-prereq (ADR 0005 + kostnadsskydd):** Strategiska val med webb-Claude. Inte primärt kod. Fas 2-blockerare.

**Väg B — TDs-cleanup å la carte:** Välj 2-3 från återstående aktiva TDs. Bra kandidater:
- TD-54 (text-text-tertiary kontrast-brott projektbrett) — a11y-pass
- TD-42 (touch-target projektbrett <44px) — kan paras med TD-54
- TD-41 (Select-komponent-konvention) — kräver design-beslut, blandar med TD-53
- TD-39 + TD-40 — paras

**Väg D — Block A code-reviewer Nice-to-have:** Wire-shape integration-test för PagedResult-kontraktet. Mindre värde nu eftersom existing integration-tester (`GET_applications_with_auth_returns_200_with_paged_result` + motsvarande för resumes) redan asserterar items/totalCount/page/pageSize-properties.

**Min rek:** Väg B (a11y-pass: TD-54 + TD-42) om Klas vill fortsätta städa. Annars Väg A med webb-Claude för strategi.

### Workflow-disciplin (oförändrad)

Per CLAUDE.md §9.2 + §9.6:
1. Discovery först — rapportera fil-state, befintliga patterns
2. Multi-approach-val → senior-cto-advisor auto-invokeras
3. STOPP-rapport till Klas innan implementation om CTO osäker
4. Agent-reviews paralleller vid relevant scope
5. In-block-fix-default per 4h-regel
6. Commit + push efter Klas-diff-granskning (direct-push till main per ADR 0019)
