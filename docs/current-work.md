# Current work — JobbPilot

**Status:** **VÄG A TD-60 ADR 0029 LEVERERAD 2026-05-11 ~18:00 — väntar Klas-diff-granskning innan push.** ADR 0029 (HTTP-auth-pipeline + IClaimsTransformation-disciplin) + 5 integration-tester. Backend 607 → 612. TD-60 stängd. 0 nya TDs lyfta.
**Senast uppdaterad:** 2026-05-11
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu — Väg A: TD-60 ADR 0029 (pending push)

**Stationär-CC-session 2026-05-11 ~17:00 — TD-60-stängning via dedikerat docs-pass.** Klas valde Väg A efter Fas 2 polish-block pushad. Original-scope: ~45 min pure docs. Faktisk scope: ~2.5h efter agent-review-driven in-block-fix av Major-fynd.

### Leverans

| Artefakt | Output | Status |
|----------|--------|--------|
| ADR 0029 | `docs/decisions/0029-auth-pipeline-and-claims-transformation.md` — 4 beslut: pipeline-ordning, claim-placering, per-request-fetch, allowlist | ✓ Klart |
| Index | `docs/decisions/README.md` — rad för 0029 insorterad efter 0028 | ✓ Klart |
| Integration-tester | `tests/JobbPilot.Api.IntegrationTests/Auth/SessionRoleClaimsTransformationTests.cs` — 5 tester | ✓ Klart |
| TD-60 stängd | `docs/tech-debt.md` rad 1891 — status: STÄNGD | ✓ Klart |

### ADR 0029 — 4 beslut

1. **HTTP-pipeline-ordning explicit:** `UseAuthentication` → `IClaimsTransformation` → `UseAuthorization` formaliserad som JobbPilot-specifik single source of truth. Komplementär till ADR 0008/0022/0028 (Mediator-pipeline är separat).
2. **Claim-placerings-regel:** auth-handler emit:ar bara protokoll-claims (`NameIdentifier`, `Sub`, `session_id_prefix`); claims-transformation emit:ar claims som kräver extern lookup (`ClaimTypes.Role`, framtida impersonation/IdP/tenant).
3. **Per-request-fetch utan cache i Fas 1:** security-first över micro-prestanda. Sentinel-claim `jobbpilot:roles_resolved` för idempotens. Trigger för omvärdering: >1000 req/s sustained eller federerat IdP.
4. **Konsument-allowlist via `ClaimsTransformationAllowlistTests`:** strukturell spärr analogt med ADR 0024 D1 audit-bypass-port-pattern. Ny transformation bryter build:en.

ADR 0029 är **komplementär** till ADR 0028 — supersedas inte. ADR 0028:s kärnbeslut (A1, defense-in-depth, marker, bootstrap, konstant-separation) är oförändrade; bara claim-placering har flyttats (H-3 SoC-split).

### Agent-reviews (2 parallella + 1 CTO-triage)

| Agent | Fynd | Åtgärd |
|-------|------|--------|
| code-reviewer | 2 Major + 1 Minor | Alla fixade in-block: M-1 prefix 8→6, M-2 falsk test-coverage-claim → Alt B integration-tester, Min-1 ADR 0028-path-fotnot |
| dotnet-architect | 0 Blocker / 0 Major / 3 Minor / 1 Nit | Approved as-is. 3 Minor avvisade som TD per CTO-rek (NetArchTest-stil cosmetic, sentinel-pattern-ADR YAGNI Rule-of-Three, pipeline-ordnings-arch-test mitigerat av integration-test) |
| senior-cto-advisor | M-2 multi-approach-val (Alt A/B/C) | Beslut: Alt B (integration-test i Api.IntegrationTests täcker både M-2 + dotnet-architect Minor 3). Motiverat mot Martin 2017 (REP/CCP, SRP), Cohn 2009 (Test Pyramid), Hunt/Thomas 1999 (YAGNI), Fowler 2018 (Rule of Three), Ford/Parsons/Kua 2017 (ADR append-only). 0 nya TDs lyfta. |

### Tester (full svit grön — pending push)

- Domain.UnitTests: **163** (oförändrat)
- Application.UnitTests: **201** (oförändrat)
- Architecture.Tests: **32** (oförändrat)
- Migrate.UnitTests: **6** (oförändrat)
- Api.IntegrationTests: **184** (+5 från SessionRoleClaimsTransformationTests)
- Worker.IntegrationTests: **26** (oförändrat)
- **Total: 612** (+5 från Väg A)

### Pending commits (1, väntar Klas-diff-granskning)

| Commit | Scope | Filer |
|--------|-------|-------|
| 1 | `docs(adr): 0029 — HTTP-auth-pipeline + IClaimsTransformation-disciplin + integration-tester` | `docs/decisions/0029-*.md` + `docs/decisions/README.md` + `docs/tech-debt.md` + `tests/.../SessionRoleClaimsTransformationTests.cs` + `docs/current-work.md` + `docs/sessions/` + `docs/steg-tracker.md` + `STARTPROMPT-STATIONAR-2026-05-11.md` (raderas) |

Single bundled commit: docs-pass-natur med integration-test som essentiell del av ADR-claim. CTO-godkänt scope.

---

## När nästa session startar

Klas reviewar diff per CLAUDE.md §6.3 punkt 4. Vid GO: 1 commit + push.

Sedan optionell väg:

- **Väg B:** TD-61 (audit-trail-evidence-test) som observability-pass (~1h)
- **Väg C:** Fortsätt feature-arbete (Fas 2 JobTech blockerad till ADR 0005)
- **Väg D:** Pausa

Aktiva TDs: TD-39, TD-41, TD-51, TD-52, TD-53, TD-56, TD-57, TD-58, TD-59, TD-61. (TD-60 stängd.)

Inga aktiva TDs blockerar feature-arbete.

---

## Föregående session-summary (referens) — Fas 2 Polish-block

**2026-05-11 ~16:30:** 5 audit-fynd fixade in-block (N-1 + N-3 + H-4 + N-2 + H-3), 4 TDs lyfta (TD-58/59/60/61). Backend 594 → 607. 4 commits pushade (`ff3704f`, `a683ae1`, `35b9dc0`, `c0ada25`). H-3 SoC-split levererade `SessionRoleClaimsTransformation` — vilket triggade TD-60 som denna session stängde.

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

1. Discovery först
2. Multi-approach-val → senior-cto-advisor auto-invokeras (denna session: M-2 Alt A/B/C-val, CTO valde Alt B entydigt motiverat mot Martin/Cohn/Hunt/Thomas/Fowler/Ford-Parsons-Kua)
3. STOPP-rapport till Klas innan implementation om CTO osäker / fas-strategiskt (denna session: ingen STOPP behövd — CTO-rek entydigt + användar-mode "kör utan att stanna")
4. Agent-reviews parallellt vid relevant scope (2 reviews + 1 CTO-triage)
5. In-block-fix-default per 4h-regel (alla agent-fynd hanterade in-block, 0 nya TDs)
6. Commit + push efter Klas-diff-granskning (direct-push till main per ADR 0019)
