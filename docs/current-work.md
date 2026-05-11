# Current work — JobbPilot

**Status:** **VÄG B — a11y-pass LEVERERAD 2026-05-11 sen efm.** TD-54 + TD-42 stängda i samlad batch (4 commits) ovanpå Väg C-housekeeping. Ny TD-57 (native form-controls divergerar från Input-primitive) lyft som konsekvens av TD-42-design-review. **Stängda TDs totalt:** TD-15, TD-31, TD-38, TD-42, TD-43, TD-44, TD-45, TD-46, TD-47, TD-48, TD-50, TD-54, TD-55. **Aktiva TDs:** TD-39, TD-40, TD-41, TD-49, TD-51, TD-52, TD-53, TD-56, TD-57. **Nästa fas:** Fas 2 (JobTech Integration) — blockerad till ADR 0005 go-to-market + kostnadsskydd.
**Senast uppdaterad:** 2026-05-11
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu

**Stationär-CC-session 2026-05-11 sen efm — VÄG B a11y-PASS.** Bundle:
TD-54 (text-text-tertiary kontrast-fix för funktionell text, WCAG AA 1.4.3) +
TD-42 (touch-target-uppgradering till skill-doc-defaults: h-9 default + h-11
critical CTAs). Skill-doc-konformitet snarare än multi-approach-val:
kod-baseline var drift från `jobbpilot-design-components` + `jobbpilot-design-a11y`-skills.

### Sub-block-summary

| Block | Scope | Commits | Status |
|-------|-------|---------|--------|
| Discovery | TD-54 16 träffar i 9 filer + TD-42 5 komponenter | (no commit) | ✓ |
| Skill-läsning | tokens/a11y/components — alla 3 bekräftade entydiga | (no commit) | ✓ |
| Block A Commit 1 | TD-54 kontextuell mapping (10 funktionell → secondary, 5 dekorativ kvar tertiary) | `8cfbde4` | ✓ |
| Block A reviews | code-reviewer (APPROVE 0/0/0/0) + design-reviewer (APPROVE 0/0/4/2) parallellt | (rapporter) | ✓ |
| Block A Commit 2 | In-block-fix N1 (cursor-not-allowed pagination disabled) + review-rapporter | `52f3b45` | ✓ |
| Block B Commit 1 | TD-42 primitives: input/button/select + 2 native-form-controls | `f2b179a` | ✓ |
| Block B reviews | code-reviewer (APPROVE 0/0/1/1) + design-reviewer (APPROVE-WITH-FIXES 0/3/2/0) | (rapporter) | ✓ |
| Block B Commit 2 | In-block-fixar M1+M2 (Avbryt h-7→h-9 + page-header CTAs sm→default) + TD-57 lyft + review-rapporter | `1b0b9ec` | ✓ |

### Discovery-fynd som påverkade scope

**TD-54 var inte multi-approach-val.** Skill-doc `jobbpilot-design-tokens`
säger explicit att `text-text-tertiary` är för "Disabled, placeholder", och
`jobbpilot-design-a11y` §4 säger "text-tertiary fails for body text on
surface-secondary — only use it for decorative/non-essential text". Hela
användningen var dokumentations-drift, inte design-val. Kontextuell
mapping (funktionell → secondary, separatorer → tertiary) följde befintliga
skill-docs.

**TD-42 var inte multi-approach-val.** Skill-doc `jobbpilot-design-components`
säger "Input/Textarea/Select Height: 36px (h-9) default, 32px (h-8) for dense
contexts". Skill-doc `jobbpilot-design-a11y` §9 säger "Button (default,
h-9 = 36px) — Use lg (h-11 = 44px) for critical CTAs". Kod-baseline (h-8
default + h-9 lg) var pure drift från skill-docs. Bytet är skill→kod-konvergens.

**TD-42 M2 fynd:** Page-header primary-CTAs ("Ny ansökan", "Nytt CV") använde
`size="sm"` (h-7 = 28px) — inkonsekvent mot form-submit-CTAs (h-9). Pre-existing
problem som blev tydligare efter default-uppgradering. In-block-fix tillämpad.

### Reviews i Block A (TD-54)

| Reviewer | Verdict | Fynd | Status |
|----------|---------|------|--------|
| code-reviewer | APPROVE | 0/0/0/0 | Inga åtgärder behövs |
| design-reviewer | APPROVE | 0/0/4/2 | N1 (cursor-not-allowed) fixad in-block, övriga acceptabla observations |

### Reviews i Block B (TD-42)

| Reviewer | Verdict | Fynd | Status |
|----------|---------|------|--------|
| code-reviewer | APPROVE | 0/0/1/1 | Minor = TD-57 (lyft) |
| design-reviewer | APPROVE-WITH-FIXES | 0/3/2/0 | M1+M2 fixade in-block, M3 lyft som TD-57 |

**In-block-fixar applicerade (4h-regel):**
1. TD-54 N1: `cursor-not-allowed` på pagination disabled-spans
2. TD-42 M1: `/ansokningar/ny` Avbryt-button size="sm" → default
3. TD-42 M2: `/ansokningar/page.tsx` + `/cv/page.tsx` page-header CTAs size="sm" → default

### Nya commits (denna session)

| SHA | Beskrivning |
|-----|-------------|
| `1b0b9ec` | fix(web): TD-42 — in-block-fixar från design-review + TD-57 lyft |
| `f2b179a` | feat(web): TD-42 — touch-target-uppgradering till skill-doc-defaults |
| `52f3b45` | fix(web): TD-54 — in-block-fix N1 + review-rapporter |
| `8cfbde4` | fix(web): TD-54 — text-text-tertiary kontrast-fix för funktionell text |

### Aktiva TDs efter denna session (9)

- **TD-39:** Error-summary-mönster för stora formulär
- **TD-40:** Path-equality regression-bevakning
- **TD-41:** Select-komponent-konvention native vs shadcn
- **TD-49:** HstsOptions unit-test (blockerad — kräver JobbPilot.Api.UnitTests-projekt)
- **TD-51:** Admin-läs-aktioner audit-logging (Fas 6 GDPR Art. 30)
- **TD-52:** Admin-endpoint dedikerad rate-limit-policy (Fas 6)
- **TD-53:** Frontend API-resultatformat kind-union standardisering (>4h scope)
- **TD-56:** ListJobAdsQuery full paginering (Fas 2 JobTech-integration)
- **TD-57:** Native form-controls divergerar från Input-primitive — NY

### Tester (full svit grön)

- **Backend:** 594/594 oförändrat (ingen backend-touch i Väg B)
- **Frontend Vitest:** 150/150 oförändrade efter samtliga 4 commits

### Föregående session-summary (referens)

**Stationär-CC 2026-05-11 ~12:00 efm:** Väg C Fas 1.5-housekeeping. TD-47/48/50/55
stängda. HEAD = `acf2b28` vid session-start för Väg B.

---

## Föregående session — Väg C Fas 1.5 housekeeping (referens)

### Stängda TDs totalt (kumulativt)

- TD-15 (Resume-formulär a11y) — Fas 1 Block A1
- TD-31 (UseHttpsRedirection env-gate-test) — Fas 1 Block A3
- TD-38 (Trust Server Certificate hardening) — Fas 1 Block A4
- TD-42 (Touch-target projektbrett) — **denna session, Block B**
- TD-43 (komponent-tests för LoginForm + MeProfileForm + ResumeContentForm)
- TD-44 (HSTS-header anti-regression-test)
- TD-45 (LoginForm focus-flytt vid state.error)
- TD-46 (extrahera pathToElementId per-domän)
- TD-47 (RDS CA-bundle-rotation-bevakning) — Väg C Block B.2
- TD-48 (Architecture-test för Trust=true-läckage) — Väg C Block B.1
- TD-50 (Prod-konfig-källa för AdminBootstrap__InitialAdminEmail) — Väg C Block C
- TD-54 (text-text-tertiary kontrast-fix för funktionell text) — **denna session, Block A**
- TD-55 (PagedResult retro-fit) — Väg C Block A

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

**Väg A — Fas 2-prereq (ADR 0005 + kostnadsskydd):** Strategiska val med webb-Claude.
Inte primärt kod. Fas 2-blockerare. Föredragen approach.

**Väg E — Mer TDs-cleanup:** Återstående aktiva TDs:
- TD-57 (native form-controls divergerar) — kan paras med eventuell datepicker
- TD-39 (error-summary) + TD-40 (path-equality regression) — parade
- TD-41 (Select-konvention) + TD-53 (kind-union) — kräver design-beslut

**Väg D — Wire-shape integration-test** (PagedResult-kontraktet) — kan deferas.

**Min rek:** Väg A med webb-Claude för strategi — TDs-listan är nu trimmad nog
att Fas 2-uppstart är högsta värde-aktion.

### Workflow-disciplin (oförändrad)

Per CLAUDE.md §9.2 + §9.6:
1. Discovery först — alltid (rapportera fil-state, befintliga patterns)
2. Multi-approach-val → senior-cto-advisor auto-invokeras (entydigt → direkt impl)
3. STOPP-rapport till Klas innan implementation om CTO osäker / fas-strategiskt
4. Agent-reviews parallellt vid relevant scope
5. In-block-fix-default per 4h-regel
6. Commit + push efter Klas-diff-granskning (direct-push till main per ADR 0019)
