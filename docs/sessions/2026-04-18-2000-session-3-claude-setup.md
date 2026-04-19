---
session: 3
datum: 2026-04-18
slug: claude-setup
status: komplett
commits: 5
duration: ~5 timmar
---

# Session 3 — Claude Code-setup (STEG 1–6)

Första implementation-sessionen efter research (session 1), plan (session 2)
och Claude Design-research (session 2.5). Fokus på Claude Code-infrastruktur:
agenter, design-skills, AWS-foundation, lokal dev-miljö. Hooks och
GitHub-integration sköts till session 4.

## Pre-work (mellan session 2 och 3)

Klas körde AWS-setup-runbook manuellt innan session startade — aktiverade
MFA på root, skapade bootstrap IAM-user (`jobbpilot-bootstrap-admin` med
access keys för Terraform-backend-skapande), aktiverade IAM Identity
Center i eu-north-1, skapade SSO-user `klas` med AdministratorAccess, och
konfigurerade CLI-profiler `jobbpilot-bootstrap` + `jobbpilot`.

Account ID: 710427215829. Region: eu-north-1 (Stockholm). Bootstrap-user
raderas som sista steg av Fas 0 per säkerhetspolicy.

## Mål

Köra STEG 1–6 av SESSION-2-PLAN §15:

1. BUILD.md version-patch
2. AWS foundation via Terraform
3. docker-compose dev/test-miljöer
4. `.claude/`-grundstruktur + settings.json
5. 11 Claude Code-agenter + ADRs
6. 5 design-skills + DESIGN.md-refactor

## Genomfört

### STEG 1 — BUILD.md version-patch

Alla versioner från SESSION-1-VERSION-AUDIT synkade: .NET 9→10, C# 13→14,
EF Core 9→10, MediatR→Mediator.SourceGenerator, FluentAssertions→Shouldly,
PostgreSQL 17→18.3, Redis 7.4→8.6, Next.js 15→16.2, TypeScript 5.6→6.0,
Terraform 1.9→1.14. GitHub Flow formaliserat (develop-branch borttagen
från §18 per ADR 0004-förarbete). Bedrock-modell-ID:n uppdaterade till
Claude Opus 4.7, Sonnet 4.6, Haiku 4.5 via EU-inference-profiles.

### STEG 2 — AWS foundation

Terraform-bootstrap i `infra/terraform/bootstrap/` (S3 state-bucket +
DynamoDB locks, körd med `jobbpilot-bootstrap`-profil). Prod-environment
i `infra/terraform/environments/prod/` med moduler: budgets (zero-spend +
$50/mån alerts), cloudtrail, kms (master + byok keys), secrets_manager
placeholders, bedrock_model_access för Haiku 4.5 + Sonnet 4.6 + Opus 4.7
i eu-central-1 + eu-west-1.

Bedrock model-access kräver manuell godkännande via AWS-teamet — kan ta
timmar. Dokumenterat i runbook att BUILD.md §8 uppdateras när godkännande
kommer.

Inference profile-verifiering sparad i
`docs/research/bedrock-inference-profiles.md`. Runbook
`docs/runbooks/aws-setup.md` skapad parallellt.

### STEG 3 — docker-compose

`docker-compose.yml` för dev (postgres 18.3 + redis 8.6-alpine + seq 2025.2)
och test-profile för integration (ports 5433/6380). End-to-end-verifierad:
alla 5 services healthy, version-outputs matchar BUILD.md.

Två tekniska upptäckter under verifieringen:

1. **Seq 2025.2+ kräver explicit firstRun-konfig.** Första start fick
   exit code 1 med `No default admin password was supplied`. Lagt till
   `SEQ_FIRSTRUN_NOAUTHENTICATION=true` för lokal dev (Seq är bunden till
   loopback, innehåller bara dev-loggar). Produktion kör CloudWatch istället.
2. **PostgreSQL 18+ ny volym-mount-konvention.** Standard mount flyttades
   till `/var/lib/postgresql` (inte `/var/lib/postgresql/data`) så
   version-specifika subkataloger kan skapas för `pg_upgrade --link`.
   Uppdaterat path:en i både dev och test, dokumenterat i runbook §6.5.

Runbook `docs/runbooks/local-dev-setup.md` skapad med 6 troubleshooting-cases.

### STEG 4 — `.claude/`-struktur

`.claude/settings.json` committad med permissions-matris. Kataloger för
agents/skills/hooks/commands/rules/templates/patterns/memory skapade med
korta README:s per katalog.

**Auto mode-diskussion:** Claude Code introducerade Auto mode (classifier-
baserad permission i research preview) sent mars 2026. Övervägdes som
alternativ till statiska allow/deny/ask-listor — särskilt relevant för
AFK-scenarier. Beslut: använd statiska lists nu, utvärdera Auto mode
senare när det är GA.

Vissa permissions flyttade från `ask` till `allow`: `git push`,
`gh pr create`. Kvar i `ask`: `gh pr merge`, `terraform apply`,
`dotnet ef database update`, edits på BUILD.md/CLAUDE.md/DESIGN.md.

Runbook `docs/runbooks/claude-code-setup.md` skapad med förklaring av
permissions-modellen.

### STEG 5 — 11 agenter + ADR 0002 + ADR 0005

**Modell-mappning per ADR 0002 (fylld under STEG 5.1):**

Klas kör **Claude MAX 5x-plan** — flat-rate, inte API per-token. Kostnad är
inte primär faktor — latency och usage-limits är. Därför splittrat på
tier-kvalitet per agent:

- **Opus 4.7** (7 agenter — creative/design/kritiska): dotnet-architect,
  nextjs-ui-engineer, ai-prompt-engineer, test-writer, code-reviewer,
  security-auditor, design-reviewer
- **Sonnet 4.6** (4 agenter — pattern-matching/latency-känsliga):
  test-runner, db-migration-writer, docs-keeper, adr-keeper

**Första degraderingspath om limits blir problem:** design-reviewer → Sonnet
(reviewar triggas vid varje FE-PR, men pattern-matching-delen klarar Sonnet).

ADR 0002 (explicit model versions) fyllde session 2-stub. ADR 0005
(go-to-market strategi, Fas 2-prereq) skapad som Proposed.

**11 agenter skapade en i taget med granskning per agent** — 4 572
insertions totalt. Varje agent har tool access-matris, trigger-definition,
och delegation-mönster.

**Upptäckt spec-konflikt under STEG 5.5 (nextjs-ui-engineer):** DESIGN.md
§12 visar CSS-first @theme-block (Tailwind v4-rekommendation); agenten
refererade "BUILD.md hybrid mode med tailwind.config.ts". Verifiering
behövs innan FE-implementation börjar. Skapades som
`docs/research/issues/tailwind-config-approach.md` för att inte blockera
STEG 5.6.

**Upptäckt arkitektur-fråga under STEG 5.10-11:** Modell-IDs är hårdkodade
på 6-7 platser (agent-frontmatters, settings.json, prompts, BUILD.md,
ADR 0002). Vid modell-uppgradering måste alla synkas manuellt — felbenäget.

Övervägdes: centralised alias (t.ex. `sonnet-latest`) med runtime-resolution.
**Avvisat** eftersom det bryter reproducerbarhet (samma prompt ger olika
output beroende på när den körs), eval-integritet, GDPR-audit-trail
("vilken modell processade denna användares data?").

**Accepterad lösning:** explicit versioner + centraliserad uppgradering via
tre framtida skills (Fas 1):

- `/audit-models` — hittar nya versioner (read-only)
- `/evaluate-new-model` — eval-testar innan uppgradering
- `/update-model-version` — konsistent uppgradering + skapar ADR

Dokumenterat i `docs/research/issues/model-version-management.md`.

**Commit-strategi:** Tre separata commits i kausal ordning:

1. Docs först (policy): ADR 0002, ADR 0005, BUILD.md Fas 2-prereq
2. Implementation: 11 agenter
3. Session-state: current-work + research issues

Motivering: när framtida läsare ser "feat(claude): add 11 agents" med
explicit modell-versioner i frontmatters, kan de kolla previous commit
och hitta ADR 0002 som motiverar valet.

### STEG 6 — Design-skills + DESIGN.md-refactor

**ADR 0003 reviderad under STEG 6.1.** Original specificerade strikt
"ingen duplicering mellan DESIGN.md och skills" — men det betyder att
framtida läsare måste navigera 5 filer för att få helhetsbild. Alt B
valt: DESIGN.md behåller §1 (filosofi) + kortade sammanfattningar av
§2-§10 som pekar till skills för fulla specer. Skills har fullständiga
specer. Docs-keeper verifierar att sammanfattningar stays synced vid
session-end (drift-protection).

Fem skills skapade en i taget:

- `jobbpilot-design-principles` — civic-filosofi, do/don't
- `jobbpilot-design-tokens` — färger, typografi, spacing, radius
- `jobbpilot-design-components` — Button, Card, Input, Table
- `jobbpilot-design-copy` — svensk ton, microcopy
- `jobbpilot-design-a11y` — WCAG, fokusring, keyboard, testing-checklist

Progressive-disclosure-mönster: SKILL.md besvarar 80%-fallen, references/
för edge cases. Tumregel som följdes: SKILL.md <280 rader, references
<120 rader.

**Storleks-disciplin-undantag:** a11y-skill SKILL.md blev 343 rader (över
280-gränsen) eftersom 20-punkt testing-checklist är design-reviewers
direktkälla — flyttad till references skulle betyda extra load-operation
per granskning. 23% över är motiverat för mest frekvent-använt material.

DESIGN.md transformerad från 631 rader → 180 rader. `.claude/README.md`
uppdaterad av docs-keeper med agent-lista (11 rader) och skill-lista
(5 rader + references-counts). **Docs-keeper verifierade agent-lista från
frontmatter** istället för att lita på briefing-information — god disciplin.

## Beslut och designval (samlade)

**design-reviewer vs code-reviewer — ansvarsfördelning:** "Suboptimal
component composition" flyttades från design-reviewer till code-reviewer
(Area 5, Conventions/TypeScript). design-reviewer äger estetik, a11y,
copy — code-reviewer äger komponent-sammansättning.

**App-shell-layout deferred:** DESIGN.md original §4.2 hade specifikation
för app-shell (sidebar 240px, collapsed 60px). Finns inte i skills — ska
dokumenteras i `jobbpilot-design-components/references/` under Fas 1 när
app-shell byggs.

**"Är detta workflow löjligt?"-fråga (STEG 5.10-11):** Klas ifrågasatte
om Claude web verifierade Claude Code när svaret är "godkänd utan
ändringar" i ~95% av fallen. Ärlig bedömning: delar av workflow tillför
marginalt värde när Claude Code producerar bra arbete på uppenbara
uppgifter (commit-instruktioner, rutin-agent-granskning). Design-beslut,
scope-konflikter, och arkitektur-frågor är där Claude web bidrar
substantiellt. Klas kan välja att hoppa granskning när uppgiften är
rutin-artad.

## Commits

| Commit | Innehåll |
|--------|----------|
| `756ca1c` | docs(decisions): ADR 0002 + ADR 0005 + BUILD.md Fas 2-prereq |
| `7699bb8` | feat(claude): 11 agenter (4 572 insertions) |
| `b72969a` | docs(session): current-work + 2 research issues |
| `c36c26d` | feat(claude): 5 design skills (3 100 insertions) |
| `cae7ccb` | docs(design): DESIGN.md → index-format + README skill-lista |

## Öppna research-issues vid session-slut

- **tailwind-config-approach:** CSS-first (`@theme` i globals.css) vs hybrid
  (tailwind.config.ts). Beslut behövs innan frontend scaffoldas.
- **model-version-management:** ramverk för framtida skills A/B/C. Risk
  bedömd låg (Bedrock inference profiles abstraherar versioner) men behöver
  dokumenteras och implementeras innan första modell-uppgradering.

## Nästa session

Session 4 startar med STEG 7 (hooks-blocket). Preliminärt också STEG 8
(GitHub-integration) beroende på tidsåtgång.
