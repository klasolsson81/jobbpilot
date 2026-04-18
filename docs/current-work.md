# Current work — JobbPilot

**Status:** Session 3 pågår. Claude Code-setup + AWS foundation under implementation.
**Datum:** 2026-04-18

---

## Aktivt nu

- **STEG 5 (agenter)**: dotnet-architect skapad, granskas. ADR 0002 och ADR 0005 skrivna.

## Klart senaste session

- Session 1: research (`docs/research/SESSION-1-*.md`).
- Session 2: plan godkänd (`docs/research/SESSION-2-PLAN.md`).
- Session 2.5: Claude Design-research + skill-arkitektur (`docs/research/CLAUDE-DESIGN-FINDINGS.md`).
- Session 3 steg 1: BUILD.md version-patch applicerad (.NET 10, C# 14, Mediator.SourceGenerator, Shouldly, GitHub Flow, tag-baserad deploy, Bedrock-ID:n).
- Session 3 steg 2: AWS foundation — Terraform bootstrap + prod applicerade (budgets, cloudtrail, kms, secrets_manager, bedrock_model_access). Bedrock inference profiles verifierade, alla tre modeller `AUTHORIZED` + `AVAILABLE` utan manuell Console-request.
- Session 3 steg 3: docker-compose för dev/test verifierat (Postgres 18 + Redis 8.6 + Seq 2025.2).
- Session 3 steg 4: `.claude/`-struktur committad (settings.json, agents/commands/hooks/rules/skills/.gitkeep, README, runbook).
- Session 3 steg 5 (pågår): ADR 0002 komplett, ADR 0005 skapad, dotnet-architect.md skapad.

## Nästa

- Godkänn dotnet-architect.md → fortsätt STEG 5 (10 agenter kvar)
- STEG 6: Skills + rules
- STEG 7–15: Slash commands, hooks, GitHub, docs, smoke test, commits + push

---

## Öppna strategiska beslut

### Go-to-market-strategi (ADR 0005)

**Deadline:** Innan Fas 2 public launch (BUILD.md §18) — senast när 10+ klasskamrater
testat i intern beta.

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
2. `terraform apply` från `infra/terraform/environments/prod/` — enda diff:en är
   IAM-policyn; ingen destructive action.

Model access är redan `AUTHORIZED` för Opus 4.7 — ingen Console-request behövs
(verifierat i `docs/research/bedrock-inference-profiles.md`).

### Mediator pipeline-ordning — dokumentera innan Fas 1

Agenter och CLAUDE.md §2.3 refererar pipeline-ordningen Logging → Validation →
Authorization → UnitOfWork, men ordningen är inte explicit motiverad någonstans.
Dokumentera i CLAUDE.md §3 eller som ADR 0006-mediator-pipeline-order.md
**innan Fas 1 kodning börjar.**

### Terraform backend — dynamodb_table → use_lockfile (ej brådskande)

`backend.tf` använder `dynamodb_table = "jobbpilot-terraform-locks"` som är
deprecated i provider ~>5.80. Fix:

1. Ändra `dynamodb_table = ...` → `use_lockfile = true` i `backend.tf`.
2. Kör `terraform init -migrate-state` — Terraform konverterar lock-mekanismen
   till S3-nativa locks (använder ConditionalCheckFailedException på `.tflock`-objekt).
3. DynamoDB-tabellen kan sen raderas från bootstrap-stacken.

Inte blocker nu; fungerar som-är. Behandlas i separat session.
