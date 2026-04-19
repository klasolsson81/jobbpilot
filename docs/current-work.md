# Current work — JobbPilot

**Status:** FAS 0 BOOTSTRAP KOMPLETT. Nästa: Fas 0 kod-scaffolding (.NET Solution + Next.js scaffolding).
**Datum:** 2026-04-19

---

## Aktivt nu

**FAS 0 BOOTSTRAP KOMPLETT.** Alla 21 steg från SESSION-2-PLAN §15 hanterade
(STEG 11 deferred — se nedan). Bootstrap IAM-user raderad. SSO är enda
AWS-åtkomstvägen.

**Vad som är på plats:**
- AWS-foundation (Terraform: budgets, KMS, Bedrock model access, secrets manager)
- Docker-compose dev/test (Postgres 18 + Redis 8.6 + Seq 2025.2)
- 11 Claude Code-agenter (7 Opus 4.7 + 4 Sonnet 4.6 per ADR 0002)
- 5 design-skills (DESIGN.md som index per ADR 0003)
- 7 Claude Code-hooks + 2 Husky-hooks (med kända begränsningar i ADR 0006)
- GitHub-integration (templates, CODEOWNERS, Dependabot, branch protection B-nivå)
- Komplett docs-struktur (8 ADRs med index, runbooks, session-loggar)
- CLAUDE.md uppdaterat med Session Protocol + Docs structure (STEG 10)
- guard-spec-files-hook reellt aktiv (jq-beroende borttaget per fix `1879b4b`)

**När nästa session startar (Fas 0 kod-scaffolding):**

1. Kör `git log --oneline -10` — verifiera STEG 12-commits överst
2. Läs `docs/sessions/` senaste session-logg
3. Verifiera SSO: `aws sts get-caller-identity --profile jobbpilot`
4. Verifiera hooks: `bash .claude/hooks/session-start.sh`
5. Diskutera med Claude web (ny chat) innan första scaffolding —
   beslut: Solution-layout, projekt-struktur, exakt Mediator.SourceGenerator-integration

**Aktiva skyddslager på main:**
- Pre-push gitleaks-scan med 3-stegs fallback-lookup
- Branch protection B-nivå (no force push, no deletion)
- Claude Code-hooks (alla 7 fungerande, guard-spec-files reellt aktiv)
- Husky pre-commit scaffold-gate (full test-gates aktiveras i Fas 0/1)

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
- Session 4 STEG 9 ✅: Docs-struktur komplett. ADR 0001 fylld, ADR 0004 + ADR-index skapade, session 3 + 4 loggade. 3 commits: 6763e65, 8c50c75, 7c4ad28.
- Session 5 STEG 10 ✅: CLAUDE.md uppdaterad med Session Protocol + Docs structure + spec-drift-fix (.NET 10/14, Mediator.SourceGenerator, GitHub Flow, ingen LocalStack). Commit: bda9f72.
- Session 5 STEG 10-followup ✅: guard-spec-files-hook reellt aktiv (jq-beroende borttaget, ADR 0006 utökad med Begränsning 4). Commits: 1879b4b, 6c37a1c.
- Session 5 STEG 11: DEFERRED till Fas 1 — feature-flödet-smoke-test (§15 rad 21) kräver slash-commands som inte existerar och scaffold som inte finns. STEG 7.6 (commit 44c7592) gjorde redan meningsfull infrastruktur-smoke-test som producerade ADR 0006.
- Session 5 STEG 12 ✅: Bootstrap-IAM-user raderad (säkerhetsskuld stängd). SSO är nu enda AWS-åtkomstvägen. Fas 0 bootstrap KOMPLETT.

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
| `6763e65` | docs(decisions): STEG 9.1 ADR 0001 (Clean Architecture) + ADR 0004 (GitHub Flow) |
| `8c50c75` | docs(decisions): STEG 9.2 ADR-index + filnamn-fix |
| `7c4ad28` | docs(sessions): STEG 9.3 session 3 + session 4 logs |
| `bda9f72` | docs(claude): STEG 10 — Session Protocol + Docs structure + spec-drift fix |
| `1879b4b` | fix(hooks): bash-native parsing in guard-spec-files (drop jq dependency) |
| `6c37a1c` | docs(decisions): ADR 0006 add 4th limitation — silent dependency failures |

## Nästa

**Fas 0 kod-scaffolding** börjar nu (efter Fas 0 bootstrap).

Beslut att fatta innan första kod skrivs:
- .NET Solution-layout (single solution, antal projekt, naming)
- Next.js scaffolding (App Router struktur, src/lib organisering)
- Hur Mediator.SourceGenerator integreras konkret (config, registrering)
- Tailwind-config-beslut (CSS-first @theme vs hybrid — research-issue väntar)

Diskutera med Claude web (ny chat) innan Claude Code börjar koda.

**Efter scaffolding (Fas 1):**
- Domain-projekt: alla aggregates med >80% test coverage
- EF Core-konfiguration + migrations + SSYK seed-data
- Mediator.SourceGenerator-pipeline (Logging→Validation→Auth→UnitOfWork)
- Application: queries/commands för JobSeeker, Resume, Application (utan AI)
- API-endpoints
- MILSTOLPE Fas 1: Manuellt skapa CV, submit:a "fake" ansökan, se i admin-audit

---

## Fas 0 bootstrap — slutsummering

**Tidsåtgång:** ~14 timmar fördelade över 5 sessioner (3 oktober — 19 april 2026)

**Levererat:**
- Komplett Claude Code-infrastruktur (settings, agents, skills, hooks, commands)
- AWS-foundation via Terraform (idempotent, reproducerbar)
- Docker-compose dev/test
- GitHub-integration (templates, CODEOWNERS, Dependabot, branch protection)
- 8 ADRs (Clean Arch, model versions, design as skills, GitHub Flow, go-to-market,
  hooks limitations, branch protection, +ADR-index)
- Spec-trio (BUILD.md, CLAUDE.md, DESIGN.md) på senaste versioner
- Lokala skyddslager (gitleaks pre-push, hooks, Husky)

**Tekniska skulder noterade för Fas 1:**
- Code-reviewer auto-trigger funkar inte (ADR 0006 §2) — manuell invocation
- SessionStart-hook output osynlig i VS Code-extension (ADR 0006 §1)
- Code-reviewer sparar inte rapport i docs/reviews/ (ADR 0006 §3)
- Tailwind-config-beslut väntar (`docs/research/issues/`)
- Model-version-management-skills planerade (`docs/research/issues/`)

**Stoppat utan att lösa (medvetet):**
- Slash-commands (`/new-feature`, `/test`, `/review`, `/commit`) — Fas 1
- ADR 0005 go-to-market-beslut — Fas 2-prereq
- Pipeline-ordering ADR — innan Fas 1 kodning

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
