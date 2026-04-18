# Current work — JobbPilot

**Status:** Session 3 pågår. Claude Code-setup + AWS foundation under implementation.
**Datum:** 2026-04-18

---

## Aktivt nu

- **STEG 2 (AWS foundation)** via Terraform är klar:
  bootstrap + prod applicerade, Bedrock inference profiles verifierade.
  Nästa: STEG 3 (docker-compose).

## Klart senaste session

- Session 1: research (`docs/research/SESSION-1-*.md`).
- Session 2: plan godkänd (`docs/research/SESSION-2-PLAN.md`).
- Session 2.5: Claude Design-research + skill-arkitektur (`docs/research/CLAUDE-DESIGN-FINDINGS.md`).
- Session 3 steg 1: BUILD.md version-patch applicerad (.NET 10, C# 14, Mediator.SourceGenerator, Shouldly, GitHub Flow, tag-baserad deploy, Bedrock-ID:n).
- Session 3 steg 2: AWS foundation — Terraform bootstrap + prod applicerade (budgets, cloudtrail, kms, secrets_manager, bedrock_model_access). Bedrock inference profiles verifierade, alla tre modeller `AUTHORIZED` + `AVAILABLE` utan manuell Console-request.

## Nästa

- Session 3 steg 3: docker-compose för dev/test
- Session 3 steg 4-15: `.claude/`-struktur, agenter, skills, commands, hooks, GitHub, docs, smoke test, commits + push
- Fas 0 avslut: radera bootstrap-IAM-user när SSO-profile fungerar mot alla Terraform-stackar

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

### Terraform backend — dynamodb_table → use_lockfile (ej brådskande)

`backend.tf` använder `dynamodb_table = "jobbpilot-terraform-locks"` som är
deprecated i provider ~>5.80. Fix:

1. Ändra `dynamodb_table = ...` → `use_lockfile = true` i `backend.tf`.
2. Kör `terraform init -migrate-state` — Terraform konverterar lock-mekanismen
   till S3-nativa locks (använder ConditionalCheckFailedException på `.tflock`-objekt).
3. DynamoDB-tabellen kan sen raderas från bootstrap-stacken.

Inte blocker nu; fungerar som-är. Behandlas i separat session.
