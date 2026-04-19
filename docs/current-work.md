# Current work — JobbPilot

**Status:** STEG 8 klar. Nästa: STEG 9 (docs-struktur + fler ADRs).
**Datum:** 2026-04-19

---

## Aktivt nu

**PAUS efter session 4.** STEG 7 (hooks-infrastruktur) + STEG 8 (GitHub-integration) + gitleaks-fallback-fix klart. Allt pushat till origin/main.

**När nästa session startar:**

1. Kör `git log --oneline -6` — ska visa senaste commits med `e1c48eb` som HEAD.
2. Läs ADR 0006 (hooks-begränsningar) och ADR 0007 (branch protection).
3. Verifiera att hooks fortfarande triggar: `bash .claude/hooks/session-start.sh` ska ge output.
4. STEG 9 startar: docs-struktur + fler ADRs (§15 rad 17-18). Lättare omfattning än STEG 7-8.

**Aktiva skyddslager på main:**
- Pre-push gitleaks-scan (blockerar push vid saknad binär, scannar hela historiken)
- Branch protection: no force push, no deletion
- Claude Code-hooks: guard-bash, guard-spec-files, post-todo-review, m.fl.
- Husky pre-commit: scaffold-gate (aktiveras fullt i Fas 0/1)

## Klart senaste session

- Session 1: research (`docs/research/SESSION-1-*.md`).
- Session 2: plan godkänd (`docs/research/SESSION-2-PLAN.md`).
- Session 2.5: Claude Design-research + skill-arkitektur (`docs/research/CLAUDE-DESIGN-FINDINGS.md`).
- Session 3 steg 1: BUILD.md version-patch (.NET 10, C# 14, Mediator.SourceGenerator, Shouldly, GitHub Flow, Bedrock-ID:n).
- Session 3 steg 2: AWS foundation — Terraform bootstrap + prod (budgets, cloudtrail, kms, secrets_manager, bedrock_model_access).
- Session 3 steg 3: docker-compose dev/test (Postgres 18 + Redis 8.6 + Seq 2025.2).
- Session 3 steg 4: `.claude/`-struktur committad (settings.json, README, runbook).
- Session 3 steg 5 ✅: 11 Claude Code-agenter skapade och committade. ADR 0002 komplett, ADR 0005 skapad.
- Session 3 steg 6 ✅: 5 design-skills skapade och committade. DESIGN.md transformerad till index-format (631 → 180 rader, per ADR 0003 Alt B). `.claude/README.md` uppdaterad med agent- och skill-lista.
- Session 3/4 STEG 7.1-7.5 ✅: 7 Claude Code-hooks + 2 Husky-hooks (pre-commit, pre-push). Commits: 584f048 (7.1-7.3), 46e5feb (7.4), 4d96a00 (7.5).
- Session 4 STEG 7.6 ✅: End-to-end smoke-test. Tre begränsningar dokumenterade i ADR 0006.
- Session 4 STEG 8 ✅: GitHub-integration (templates, CODEOWNERS, Dependabot, branch protection). Repo bytt till publikt. 3 commits: 4d403f3, acf007e, 2550ae6.

## Committat i session 3-4 (nuläge på origin/main)

| Commit | Innehåll |
|--------|----------|
| `756ca1c` | docs(decisions): ADR 0002 komplett, ADR 0005, BUILD.md Fas 2-prereq |
| `7699bb8` | feat(claude): 11 agenter (4 572 insertions) |
| `b72969a` | docs(session): current-work + 2 research issues |
| `c36c26d` | feat(claude): 5 design skills (principles, tokens, components, copy, a11y) — 3 100 insertions |
| `cae7ccb` | docs(design): DESIGN.md → index-format + .claude/README.md skill-lista |
| `584f048` | feat(claude): STEG 7.1-7.3 hooks infrastructure |
| `46e5feb` | feat(claude): STEG 7.4 code-reviewer auto-trigger (post-todo-review) |
| `4d96a00` | feat(claude): STEG 7.5 Husky + test-gates (pre-commit, pre-push) |
| `44c7592` | docs: STEG 7.6 smoke-test results + ADR 0006 |
| `4d403f3` | feat(github): STEG 8.1 issue + PR templates |
| `acf007e` | feat(github): STEG 8.2 CODEOWNERS + Dependabot |
| `2550ae6` | docs(decisions): STEG 8.3 ADR 0007 branch protection (B-nivå) |
| `bc70e5f` | docs(session): STEG 8 klar — current-work updated + repo public note |
| `e1c48eb` | fix(husky): robust gitleaks-lookup + block push on missing binary |

## Nästa

- **STEG 9**: Docs-struktur + fler ADRs (§15 rad 17-18)
- STEG 10: CLAUDE.md-uppdateringar (§15 rad 20)
- STEG 11: End-to-end smoke test hela feature-flödet (§15 rad 21)
- STEG 12: Final push + handover

---

## Repo-visibility-ändring (STEG 8.3)

Repot ändrades från **privat → publikt** under STEG 8.3. Motivering dokumenterad
i ADR 0007. Konsekvenser:

- All kod, commits och historik nu synliga för världen
- Gitleaks-scan genomförd utan fynd innan första publika push
- GitHub Actions-quota ökar från 500 till 2000 min/mån (gratis tier)
- Branch protection B-nivå aktiv på main

---

## Kända begränsningar från STEG 7.6

Dokumenterade i **ADR 0006**:

- SessionStart-hook stdout osynlig i VS Code-extensionen (hooken körs, output syns inte)
- PostToolUse(TodoWrite) `additionalContext` propageras inte reliable → code-reviewer auto-trigger funkar inte; manuell invocation eller Husky pre-commit är fallback
- Code-reviewer sparar inte rapport i `docs/reviews/` per spec — fix vid första Fas 0-review

---

## Öppna research-issues

Finns i `docs/research/issues/`:

- **tailwind-config**: CSS-first (`@theme` i globals.css) vs hybrid (tailwind.config.ts). Klas fattar beslut innan frontend startar.
- **model-version-management**: Hur hårdkodade modell-ID:n (claude-opus-4-7 etc.) hanteras vid version-bump. Bedömd risk: låg (Bedrock inference profiles abstraherar versioner), men behöver dokumenteras.

---

## Öppna strategiska beslut

### Go-to-market-strategi (ADR 0005)

**Deadline:** Innan Fas 2 public launch (BUILD.md §18).

**Dokumenterat i:** [`docs/decisions/0005-go-to-market-strategy.md`](decisions/0005-go-to-market-strategy.md)

**Tre alternativ:**

| Alt | Beskrivning | Kostnad/mån | CV-värde |
|-----|-------------|-------------|----------|
| A | Stängd klassapp (`@nbi.se` + invite) | $20–30 | Medel |
| B | Public freemium (Stripe, 49–99 SEK/mån) | $50–200+ | Högt |
| C | Invite-only public beta med hård kapp | Kontrollerbar | Medel–högt |

**Obligatoriska kostnadsskydd innan Fas 2 oavsett val:**

- [ ] AWS Budget Action vid $80/mån (auto-disable Bedrock IAM + stoppa ECS)
- [ ] Feature flag `registrations_open` (togglebar utan deploy)
- [ ] Rate limiting per user på AI-anrop
- [ ] Runbook `docs/runbooks/aws-cost-recovery.md`

---

## TODO — externa + uppskjutna steg

### Bootstrap-IAM-user cleanup (SISTA steg av Fas 0)

User `jobbpilot-bootstrap-admin` ska raderas när SSO-profilen är validerad mot
alla Terraform-stackar. Procedur: `docs/runbooks/aws-setup.md` §3.2.

### Opus 4.7 i Bedrock IAM-policy (vid behov)

`modules/bedrock_model_access/variables.tf` defaultlista täcker bara Haiku + Sonnet.
När en konkret use case kräver Opus 4.7 (Premium-tier i BUILD.md §8.2):

1. Lägg till `"eu.anthropic.claude-opus-4-7"` i `terraform.tfvars`.
2. `terraform apply` från `infra/terraform/environments/prod/`.

Model access är redan `AUTHORIZED` för Opus 4.7 — ingen Console-request behövs.

### Mediator pipeline-ordning — dokumentera innan Fas 1

Ordningen Logging → Validation → Authorization → UnitOfWork är inte explicit
motiverad. Dokumentera i CLAUDE.md §3 eller som ADR 0007 innan Fas 1 kodning börjar.

### Terraform backend — dynamodb_table → use_lockfile (ej brådskande)

`backend.tf` använder `dynamodb_table` som är deprecated i provider ~>5.80.
Behandlas i separat session — inte blocker nu.
