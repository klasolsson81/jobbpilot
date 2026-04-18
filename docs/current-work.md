# Current work — JobbPilot

**Status:** Session 3 pågår. STEG 1–6 klara. Nästa: STEG 7 (slash commands / hooks).
**Datum:** 2026-04-18

---

## Aktivt nu

Inget aktivt — session-break efter STEG 6.

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

## Committat i session 3 (nuläge på origin/main)

| Commit | Innehåll |
|--------|----------|
| `756ca1c` | docs(decisions): ADR 0002 komplett, ADR 0005, BUILD.md Fas 2-prereq |
| `7699bb8` | feat(claude): 11 agenter (4 572 insertions) |
| `b72969a` | docs(session): current-work + 2 research issues |
| `c36c26d` | feat(claude): 5 design skills (principles, tokens, components, copy, a11y) — 3 100 insertions |
| `cae7ccb` | docs(design): DESIGN.md → index-format + .claude/README.md skill-lista |

## Nästa

- **STEG 7**: Slash commands per SESSION-2-PLAN §15
- STEG 8: Hooks (pre-commit, pre-push)
- STEG 9: Code-reviewer auto-trigger workflow
- STEG 10: GitHub-integration
- STEG 11: Docs-struktur + ADRs
- STEG 12: CLAUDE.md uppdateringar
- STEG 13: End-to-end smoke test
- STEG 14: Final push
- STEG 15: Handover-dokument

---

## Design-system-noteringar (från STEG 5-6 review)

- **"Suboptimal component composition"** (STEG 5.9-5.10): Fördes från design-reviewer till code-reviewer (Area 5, Conventions/TypeScript-sektion). design-reviewer renodlad till estetik/a11y/copy; code-reviewer äger komponent-sammansättning.
- **App-shell-layout** (sidebar 240px, collapsed 60px): Finns i DESIGN.md:s ursprungsversion §4.2 men inte i skills. Ska dokumenteras i `jobbpilot-design-components/references/` under Fas 1 när app-shell byggs.

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
motiverad. Dokumentera i CLAUDE.md §3 eller som ADR 0006 innan Fas 1 kodning börjar.

### Terraform backend — dynamodb_table → use_lockfile (ej brådskande)

`backend.tf` använder `dynamodb_table` som är deprecated i provider ~>5.80.
Behandlas i separat session — inte blocker nu.
