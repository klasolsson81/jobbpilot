# Session 1 — Findings

> **Status:** Research only. No code written.
> **Datum:** 2026-04-18
> **Scope:** Claude Code + Opus 4.7 research (spår A) och LMS-projektet som mönsterreferens (spår B).
> **Systerdokument:** [`SESSION-1-VERSION-AUDIT.md`](./SESSION-1-VERSION-AUDIT.md)

---

## 0. Sammanfattning för Klas

1. **Claude Code har gjort ett generationsskifte sedan 2025.** Dokumenten har flyttat från `docs.claude.com` till `code.claude.com/docs/en/*`. Slash-commands har slagits ihop med Skills — Skills är nu "center of gravity". Hooks-ytan är cirka tre gånger så stor (~25 event-typer), och plugins + marketplaces är förstklassiga. Sandbox och managed-policies gör att IT-avdelningar kan låsa ner allt.
2. **Opus 4.7 släpptes 2026-04-16 (två dagar sedan).** 1M kontext-fönster, samma pris som 4.6, men **breaking changes**: `temperature`/`top_p`/`top_k` och manuell thinking-budget är borttagna, ny tokenizer ger ~5–15% högre kostnad per workload, och modellen följer instruktioner mer bokstavligt — gammal "dubbelkolla layouten"-prompt-scaffolding kan försämra resultat snarare än hjälpa.
3. **LMS-projektet (Nemo/Infinet) är värt att stjäla rakt av i struktur.** `.claude/`-layouten (agents/commands/skills/rules/patterns/templates + current-work.md + cqrs-complete-map.md) är en beprövad civic-utility-vänlig ritning. Vi bör anta ~80% av den och modifiera det som rör MYH/utbildningsdomän.
4. **LMS-skelettet matchar JobbPilots CLAUDE.md redan.** Samma Clean Arch-lager, samma Result-pattern, samma "Swedish error messages", samma ADR-discipline. Den största skillnaden är domänen, inte disciplinen.
5. **Rekommendation för session 2:** Bygg `.claude/`-strukturen efter LMS-mönstret men med JobbPilot-agent-namn och JobbPilot-specifika rules (civic-design, GDPR, BYOK, MYH → svensk arbetsmarknad-domänkunskap).

---

## 1. Spår A — Claude Code & Opus 4.7

### 1.1 Claude Code docs (färska, 2026-04-18)

> **Viktigt:** Dokumenten har flyttats från `docs.claude.com/en/docs/claude-code/*` till **[`code.claude.com/docs/en/*`](https://code.claude.com/docs/en)** med 301-redirects. Det finns även en [`llms.txt`](https://code.claude.com/docs/llms.txt)-index som är en genväg för framtida agent-körningar.

#### Överblick över ytan (alla docs)

| Område | Viktigaste 2026-förändringen | Källa |
|--------|-----------------------------|-------|
| Overview | Claude Code finns nu på fem ytor: CLI, Desktop-app, Web (claude.ai/code + iOS), VS Code/JetBrains, och via Agent SDK. Features som Routines, Channels (Telegram/Discord), `--teleport`, `/desktop` hand-off. | [overview](https://code.claude.com/docs/en/overview) |
| Sub-agents | Ny frontmatter: `model: inherit`, `isolation: worktree`, `memory: user\|project\|local`, `skills: [...]`, `effort: low\|medium\|high\|xhigh\|max`, `background: true`. `Task`-tool omdöpt till **`Agent`** i v2.1.63 (alias bevarat). | [sub-agents](https://code.claude.com/docs/en/sub-agents) |
| Skills | **Custom slash-commands är nu en delmängd av skills** (`disable-model-invocation: true` = traditionell command). Skills triggar på beskrivning-match, inte bara `/name`. Path: `.claude/skills/<name>/SKILL.md`. | [skills](https://code.claude.com/docs/en/skills) |
| Slash commands | Sidan redirectar till Skills. `.claude/commands/*.md` fungerar fortfarande men skills har precedence vid namnkonflikt. | [skills](https://code.claude.com/docs/en/skills) |
| Hooks | Eventlistan har tredubblats (ca 25 events). Nya: `SessionStart`, `SessionEnd`, `StopFailure`, `FileChanged`, `CwdChanged`, `ConfigChange`, `InstructionsLoaded`, `WorktreeCreate/Remove`, `PreCompact`, `PostCompact`, `Task*`, `PermissionRequest`, etc. Hook-typer utökade: `"command" \| "http" \| "prompt" \| "agent"`. JSON-output via `hookSpecificOutput` med `permissionDecision: "allow"\|"deny"\|"ask"\|"defer"`. | [hooks](https://code.claude.com/docs/en/hooks) |
| Settings | Fyra scopes: **Managed → CLI args → `.claude/settings.local.json` → `.claude/settings.json` → `~/.claude/settings.json`**. Hela nya `sandbox`-sektionen för OS-nivå bash/network-isolation på macOS/Linux/WSL2. Plugin-marketplace-kontroller (`strictKnownMarketplaces`, `allowManagedHooksOnly`). Nytt `auto` permission-läge med LLM-klassificerare. | [settings](https://code.claude.com/docs/en/settings) |
| MCP | **SSE-transport är deprecated** — HTTP (streamable) rekommenderas. OAuth 2.0-flöde på plats. **MCP Tool Search** (deferred loading av tool-schemas) default-på; minskar context-kostnad upp till ~95%. Plugin-bundlade MCP-servrar. | [mcp](https://code.claude.com/docs/en/mcp) |
| Memory | Två system: **CLAUDE.md** (du skriver) och **auto memory** (Claude skriver till `~/.claude/projects/<project>/memory/MEMORY.md`). CLAUDE.md konkateneras (ej overrides) över managed/user/project/local-lager. Path-scoped `.claude/rules/*.md` med `paths: ["src/**/*.ts"]`-frontmatter. | [memory](https://code.claude.com/docs/en/memory) |

#### Konkreta schema-citat vi kommer använda

**Sub-agent (`.claude/agents/<name>.md`)**
```yaml
---
name: domain-invariant-checker
description: Reviews new aggregate PRs against CLAUDE.md §2.2 invariants.
tools: [Read, Grep, Glob]
model: inherit
isolation: worktree          # ny 2026
memory: project              # ny 2026 — persistent MEMORY.md per agent
effort: high                 # ny 2026 — (low|medium|high|xhigh|max)
---
You are a domain-invariant auditor for JobbPilot...
```

**Skill (`.claude/skills/<name>/SKILL.md`)**
```yaml
---
name: add-application-handler
description: Scaffold a new Application-aggregate MediatR handler with pipeline behaviors.
when_to_use: When the user asks to create a new command or query handler for the Application aggregate.
argument-hint: "<command|query> <ActionName>"
disable-model-invocation: false    # true = klassisk slash-command
allowed-tools: [Read, Write, Edit, Bash(dotnet build:*)]
---
```

**Hook (`.claude/settings.json`)** — ny form med `permissionDecision`:
```json
{
  "hooks": {
    "PreToolUse": [{
      "matcher": "Bash",
      "hooks": [{
        "type": "command",
        "command": "bash -c 'echo \"$TOOL_INPUT\" | grep -q \"git commit\" && dotnet format --verify-no-changes'",
        "timeout": 30000
      }]
    }]
  }
}
```

#### Gotchas som är värda att skriva ned (alla verifierade från dokumenten)

1. **Array-settings konkateneras över scopes** — managed + user + project `permissions.allow` slås ihop. Detta är ett skifte från tidigare override-beteende.
2. **Exit code 1 i hook blockerar INTE** — endast exit code 2 blockerar (och på `WorktreeCreate` blockerar alla icke-0). Det är ett vanligt misstag.
3. **Subagenter kan inte spawna subagenter.** Nested delegation måste gå via Skills eller chained subagents från main.
4. **Plugin-provided subagents ignorerar tyst `hooks`, `mcpServers`, `permissionMode`-frontmatter** av säkerhetsskäl.
5. **Projekt-rot CLAUDE.md överlever `/compact`** (återinjiceras), men **nested CLAUDE.md i underkataloger gör det INTE** förrän Claude nästa gång läser en fil i den katalogen.

### 1.2 Opus 4.7 (du är denna modell nu)

Hämtat från [anthropic.com](https://www.anthropic.com/news/claude-opus-4-7), [platform.claude.com](https://platform.claude.com/docs/en/about-claude/models/whats-new-claude-4-7), [llm-stats.com](https://llm-stats.com/blog/research/claude-opus-4-7-vs-opus-4-6), [finout.io](https://www.finout.io/blog/claude-opus-4.7-pricing-the-real-cost-story-behind-the-unchanged-price-tag), [axios.com](https://www.axios.com/2026/04/16/anthropic-claude-opus-model-mythos).

**Facts:**

- **Release: 2026-04-16** (två dagar sedan när detta skrivs). GA dag ett på Anthropic API, Bedrock, Vertex AI, Microsoft Foundry, GitHub Copilot.
- **API model ID: `claude-opus-4-7`**.
- **Context: 1M tokens på standardpris** (inget long-context-premium längre).
- **Max output: 128k tokens.**
- **Pris oförändrat nominellt: $5/MTok input, $25/MTok output.** MEN: ny tokenizer → samma text blir 1.0×–1.35× fler tokens. Räkna med ~5–15% högre faktisk kostnad per workload.
- **Breaking API-change**: `temperature`, `top_p`, `top_k` på icke-default-värden returnerar **HTTP 400**. Styr via prompting/effort istället.
- **Breaking API-change**: `thinking.budget_tokens` borttaget. Endast `thinking: {type: "adaptive"}` accepteras. Thinking är **off by default** — måste aktiveras.
- **Nytt**: `effort: xhigh` mellan `high` och `max`. Rekommenderas som default för coding/agentic.
- **Task Budgets** (beta-header `task-budgets-2026-03-13`): advisory token-budget över hela agentic-loopen (thinking + tools + output). Min 20k. Modellen ser en räknare och prioriterar.
- **Vision**: 2576px / 3.75 MP max image (upp från 1568px / 1.15 MP), **1:1 pixel-to-coordinate mapping**. Meaningful för screenshot-agenter.
- **Agent-beteende**: literal instruktionsföljning, färre tool-calls by default, färre subagents spawnade by default (steerable), mer frekventa progress-uppdateringar, mer opinionated ton.
- **Skifte i prompt-scaffolding**: "double-check the layout before returning" och liknande patterns är nu ofta redundanta och kan försämra kvalitet. Ta bort sådant.
- **File-system memory är bättre** — scratchpads, notes-filer, client-side memory-tool visar mätbara förbättringar.
- **CursorBench 70%** (mot 4.6:s 58%) på developer-benchmark. 12-punkters hopp.
- **Landmine**: Anthropic medger själva att 4.7 trailar deras interna "Mythos"-modell på vissa axlar.

**Direkt påverkan på hur JobbPilot sätts upp:**

1. **Agent-orchestration är förbättrad** — vi vågar ha flera specialized subagents (code-reviewer, test-writer, architecture-checker) utan att main Claude tappar tråden.
2. **1M context räcker för att ha hela Domain-lagret öppet** när vi skriver ett nytt aggregat. Vi behöver inte komplicera scope-strategier i session 2.
3. **Remove legacy prompt-scaffolding** — våra CLAUDE.md-instruktioner "stoppa och tänk om när..." etc. kan förbli, men tomma "dubbelkolla"-checklistor utan substans ska rensas bort.
4. **Skip `thinking.budget_tokens` i all kod** — planera direkt för `thinking.type: "adaptive"` om vi använder extended thinking.
5. **Effort-default**: för codegen i Claude Code-sessioner, `xhigh` ska vara baseline. Vi kan sätta det i `.claude/settings.json` via `effortLevel: "xhigh"`.
6. **Kostnadsberäkning i AI-lagret**: vår §19.1 (freemium credit-modell) behöver räkna med nya tokenizern. Reell input-kostnad blir ~10% högre än BUILD.md antar.

### 1.3 Community-patterns värda att stjäla

Sex patterns med källor. Källorna är välrenommerade (Anthropic officiellt cookbook, Anthropic blog, välkända communities).

#### Pattern 1: Anthropic officiella 4-parallel reviewer + validation-pass

**Källa:** [github.com/anthropics/claude-code — code-review plugin](https://github.com/anthropics/claude-code/blob/main/plugins/code-review/commands/code-review.md)

- **Steg 1–3:** Billiga Haiku-agenter short-circuitar (PR closed/draft/trivial?) och en Sonnet-agent summerar diff:en.
- **Steg 4:** Fyra parallella reviewers — två Sonnets checkar CLAUDE.md-compliance, två Opuses jagar bugs/logic/security. Var och en returnerar structured issues med reasoning.
- **Steg 5:** Varje flaggad issue får en oberoende **validator agent** (Opus för logic, Sonnet för CLAUDE.md) som antingen bekräftar med hög konfidens eller dropar.
- **High-signal filter:** flagga bara kod som **inte kompilerar**, **producerar fel resultat**, eller **bryter en citerad CLAUDE.md-regel**. Inga stilnits, inga preexisting issues, inga "potential" issues.

**Varför stjäla:** Matchar JobbPilots civic-utility-ton exakt (inga nitpicks, allt ska kunna försvaras). Tiered model-routing (Haiku gate → Sonnet summary → Opus reviewers → validator) passar vårt §8.2-modellval. Vi implementerar `/review` som en plugin-aktig slash-command + agent-team.

#### Pattern 2: `SessionStart`-hook injicerar `.claude/tasks/session-current.md`

**Källa:** [claudefa.st — session lifecycle hooks](https://claudefa.st/blog/tools/hooks/session-lifecycle-hooks)

- `.claude/settings.json` har en `SessionStart`-hook vars stdout injiceras som context. Typisk payload: current branch, `git status --short`, `cat .claude/tasks/session-current.md`.
- Pair med `SessionEnd`-hook som skriver `{session_id, ended_at, reason}` till `.claude/logs/session-history.jsonl`.
- Konventions-directory: `.claude/tasks/session-current.md`, `.claude/logs/session-history.jsonl`, `.claude/backups/` för pre-compaction-transcripts.

**Varför stjäla:** LMS har en liknande pattern via `.claude/current-work.md` (se §2.2) men triggar det via prompt istället för hook. Att lyfta till hook gör att Claude **alltid** börjar med kontext laddad, även om användaren glömmer att säga det. Minimal kodarbete, stor winst för sessions-kontinuitet.

#### Pattern 3: Read-only specialist-subagenter under `.claude/agents/`

**Källor:** [claude.com/blog/subagents-in-claude-code](https://claude.com/blog/subagents-in-claude-code), [VoltAgent awesome-claude-code-subagents](https://github.com/VoltAgent/awesome-claude-code-subagents)

- Varje subagent = en markdown-fil i `.claude/agents/` (team-shared) eller `~/.claude/agents/` (personal), med YAML-frontmatter för `name`, `description`, och explicit `tools:`.
- **Utelämna `tools:` = full access** — #1 footgun.
- Standard-roster: `code-reviewer` (Read/Grep/Glob), `security-reviewer`, `test-runner` (Read + scoped Bash), `research-agent` (läser många filer, returnerar syntes).
- Var och en kör i sitt eget context-fönster → main context förorenas inte av råa logs.

**Varför stjäla:** Matchar precis det LMS gör (se §2.2). För JobbPilot bygger vi: `domain-invariant-checker` scoped till `src/JobbPilot.Domain/`, `architecture-checker` som kör NetArchTest-regler, `swedish-copy-checker` som validerar UI-text mot DESIGN.md §8.

#### Pattern 4: `PreToolUse`-hook på `Bash(git commit*)` som quality-gate

**Källor:** [dev.to — Git Hooks with Claude Code](https://dev.to/myougatheaxo/git-hooks-with-claude-code-build-quality-gates-with-husky-and-pre-commit-27l0), [code.claude.com hooks-guide](https://code.claude.com/docs/en/hooks-guide)

- `PreToolUse`-hook med matcher `Bash` och pattern `git commit*` kör `dotnet format --verify-no-changes && dotnet test --filter Category=Architecture` med 30s timeout.
- **Exit code 2 blockerar** och returnerar stdout till Claude som feedback → Claude själv-korrigerar istället för att pusha broken kod.
- Samma trick blockerar commits till `main` eller `develop` direkt.

**Varför stjäla:** 1:1-match mot JobbPilot CLAUDE.md §6 (branch protection) och §11.1 (Husky). När Claude kör i auto-mode eller sleep-less plan mode kan vi inte lita på att den kör Husky — hooken gör det inifrån Claude Code istället. Dubbla säkerhetsnät.

#### Pattern 5: Context7 + Sentry + Playwright MCP-triad

**Källor:** [claudefa.st — best MCP addons](https://claudefa.st/blog/tools/mcp-extensions/best-addons), [TrueFoundry — MCP integrations guide](https://www.truefoundry.com/blog/claude-code-mcp-integrations-guide)

- **Context7** (Upstash) injicerar version-specifika, up-to-date bibliotek-docs vid request-time. Dödar hallucinerade API-former från training-data-drift.
- **Playwright MCP** ger Claude en riktig browser: kör E2E, screenshot failures, itererar fixes. Matchar JobbPilot §17 Playwright-krav direkt.
- **Sentry MCP** hämtar riktiga stack traces, breadcrumbs, affected-user counts in i context.
- **Caveat:** varje MCP brännar context på tool-schemas vid session start. Använd **MCP Tool Search** (lazy loading, ~95% reduktion). Börja med EN MCP som stänger största gapet, inte alla.

**Varför stjäla:** Context7 är ett no-brainer för JobbPilot eftersom vår stack förändras snabbt (Next.js 16, EF Core 10, Tailwind 4.2). Playwright MCP kommer igång senare (fas 7). Sentry MCP när vi har errors i prod (fas 6+).

#### Pattern 6: Skills som mappar med `SKILL.md` + `scripts/` + `resources/`

**Källor:** [anthropics/claude-cookbooks/skills](https://github.com/anthropics/claude-cookbooks/tree/main/skills), [platform.claude.com agent-skills](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview)

- Layout: `my_skill/SKILL.md` (YAML + instruktioner), `scripts/` (exekverbar Python/TS), `resources/` (templates, datasets).
- Anthropic ships first-party för `xlsx`, `pptx`, `pdf`, `docx`. Trail of Bits ships 12+ security-audit-skills.
- **Skills triggar på description-match, inte bara slash-command** — Claude invocerar dem autonomt när task matchar.
- **Progressive disclosure**: Claude läser skillens fulla innehåll bara när det är relevant. Flera skills bloatar inte varje session.

**Varför stjäla:** LMS använder skills för scaffolding (add-command, add-endpoint, add-entity). Vi replikerar mönstret (se §2.2) och lägger dessutom till AI-specifika skills för våra prompt-scaffolds (generera ny `.prompt.md`-fil, lägga till ny credit-vikt, etc).

---

## 2. Spår B — LMS-projektet som mönsterreferens

Nemo (Infinet Code) har byggt [DOJO LMS](file:///c:/DOTNET-UTB/DOJO-LMS/). BE- och FE-repo ligger i `c:\DOTNET-UTB\DOJO-LMS\dojo-future-be\` respektive `c:\DOTNET-UTB\DOJO-LMS\dojo-future-fe\`. Båda har välutvecklade `.claude/`-strukturer. Detta är JobbPilots kvalitetsreferens.

### 2.1 Överblick över `.claude/`-strukturen

**BE (.NET 10 Clean Arch):**

```
dojo-future-be/.claude/
├── CLAUDE.local - BE.md                (lokal override, personliga notes)
├── CLAUDE.local.md                     (samma pattern)
├── agents/                             (5 st — code-reviewer, feature-scaffolder,
│                                        map-updater, migration-helper, test-writer)
├── commands/                           (15 st — plan, execute, verify, pr, resume,
│                                        pause, progress, status, scope, sync-board,
│                                        cross-repo, orchestrate, team-status, review, security)
├── cqrs-complete-map.md                (38 kB — alla Commands/Queries per feature)
├── current-work.md                     (4.6 kB — granular session-tracker)
├── endpoints-complete-map.md           (31 kB — alla endpoints per feature)
├── entities-complete-map.md            (31 kB — alla 54 entities + properties)
├── feature-status.md                   (12 kB — BE+FE feature-matrix)
├── patterns/                           (3 st — command-full-template, entity-full-template,
│                                        query-full-template)
├── refactoring-backlog.md              (4.5 kB)
├── rules/                              (6 st — architecture, coding-standards,
│                                        github-board, myh-compliance, swedish-education, testing)
├── settings.json                       (permissions + hooks, team-shared)
├── settings.local.json                 (personlig, gitignored)
├── skills/                             (13 st — session-start, session-end, test, build,
│                                        coverage-report, add-command, add-query, add-entity,
│                                        add-endpoint, update-maps, update-docs, status, review)
└── templates/planning/                 (STATE.md, PHASE.md, SUMMARY.md templates)
```

**FE (Next.js 16):**

```
dojo-future-fe/.claude/
├── agents/                             (7 st — code-reviewer, test-writer, feature-scaffolder,
│                                        map-updater, api-integrator, page-builder, chart-builder)
├── commands/                           (samma 15 som BE)
├── components-inventory.md             (7.1 kB)
├── current-work.md                     (1.2 kB)
├── data-fetching-map.md                (7.7 kB)
├── feature-status.md                   (5.3 kB)
├── pages-complete-map.md               (11 kB)
├── patterns/                           (4 st — component-template, form-template,
│                                        page-template, query-hook-template)
├── refactoring-backlog.md              (6.7 kB)
├── rules/                              (7 st — development-patterns, github-board, instructions,
│                                        myh-compliance, services, swedish-education, testing-summary)
├── settings.json                       (permissions + hooks, team-shared)
├── settings.local.json                 (personlig)
├── skills/                             (18 st — session-start/-end, build, lint, lint-check, test,
│                                        coverage-report, review, status, update-maps,
│                                        add-chart, add-component, add-form, add-hook, add-page,
│                                        create-feature, create-page)
├── templates/planning/                 (samma)
├── test-coverage-roadmap.md
├── test-execution-plan.md
├── testing-standards.md                (18 kB — största testdokumentet)
├── types-audit.md
└── validation-map.md
```

### 2.2 Detaljerad inventering

#### Agents (citat + roll)

**BE `code-reviewer`** ([`dojo-future-be/.claude/agents/code-reviewer.md:6-66`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.claude/agents/code-reviewer.md))
> "You are a code review specialist for a .NET 10 Clean Architecture project... Review code changes for compliance with project standards, architecture rules, and best practices."

- Läser först: `.claude/rules/architecture.md`, `coding-standards.md`, `testing.md`, `myh-compliance.md`, `refactoring-backlog.md`, `docs/claude/CODING_STANDARDS.md`.
- Strukturerad checklist: Architecture Compliance, Command/Handler Quality, Entity/Repository Quality, Testing, Security, MYH Compliance.
- Output-format: **Critical / Warning / Info**.

**BE `feature-scaffolder`** ([`agents/feature-scaffolder.md`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.claude/agents/feature-scaffolder.md))
> "Create the complete CQRS pipeline for a new feature: entity, config, repository, commands, queries, handlers, validators, DTOs, mappings, and endpoints."

- Läser patterns-templates + cqrs/entities/endpoints-maps före scaffolding.
- 6-fas process: Domain → Infrastructure → Application → API → Database → Verify.
- Konkret filsökväg-konventioner i varje fas.

**BE `test-writer`** ([`agents/test-writer.md`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.claude/agents/test-writer.md))
> "Write unit tests for command handlers, query handlers, and services using the project's test stack: xUnit 2.9.3 / FakeItEasy 8.3.0 (NOT Moq) / Shouldly 4.3.0 (NOT FluentAssertions)."

- Naming: `MethodName_Scenario_ExpectedResult`.
- FakeItEasy patterns, Shouldly assertions, explicit "what to test" per handler-typ.

**BE `map-updater`** ([`agents/map-updater.md`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.claude/agents/map-updater.md))
> "Scan actual source code and update .claude/ reference map files to reflect the current state."

- Scannar `Application/Features/`, `Domain/Entities/`, `Infrastructure/Persistence/Configurations/`, `API/Endpoints/`.
- Regenererar `cqrs-complete-map.md`, `entities-complete-map.md`, `endpoints-complete-map.md`, `refactoring-backlog.md`.

**BE `migration-helper`** ([`agents/migration-helper.md`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.claude/agents/migration-helper.md))
> "EF Core migration specialist... creating, reviewing, and troubleshooting schema changes."

- FK-behavior-tabell, PostgreSQL-specific notes (legacy timestamp behavior), troubleshooting.

**FE `api-integrator`** ([`dojo-future-fe/.claude/agents/api-integrator.md`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-fe/.claude/agents/api-integrator.md))
> "Given a backend endpoint, create the full frontend data pipeline: TypeScript types, endpoint function, query keys, and TanStack Query hook."

- **Sätter uttryckligen BE→FE-type-mapping-regler**: `Guid` → `string`, `DateTime` → ISO-string, `decimal` → `number`, `PascalCase` → `camelCase` via Axios.
- Fyrastegs-pipeline: Types → Endpoint functions → Query keys → Query hooks.

**FE `page-builder`** ([`agents/page-builder.md`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-fe/.claude/agents/page-builder.md))
> "Create complete pages with proper SSR/CSR split, loading states, error boundaries, and data integration."

- Page-typer: List (KPI + search + table + empty), Detail (breadcrumb + tabs + back), Dashboard (Mosaic-KPI + charts), Form (RHF + Zod + Swedish labels).

#### Commands (slash)

15 commands, samma uppsättning i BE och FE. Gruppering:

**Planning lifecycle:** `/plan`, `/execute`, `/progress`, `/verify`, `/pause`, `/resume`

**Code quality:** `/review`, `/pr`, `/security`

**Context:** `/status`, `/scope`

**Cross-repo & team:** `/cross-repo`, `/orchestrate`, `/team-status`, `/sync-board`

**Nyckel-citat ur `/plan` ([`commands/plan.md:17-48`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.claude/commands/plan.md)):**
> "Before planning, investigate the codebase to understand: What exists that's relevant to this feature... What patterns are already established... Any cross-repo implications (BE ↔ FE)... **MYH compliance requirements**."
> "Break the work into phases. Each phase should be completable in one session. Keep phases small and focused — prefer 3 small phases over 1 large one."

Sedan skrivs `.planning/STATE.md` och GitHub-issue skapas automatiskt via `gh issue create` + `gh project item-add 14`. **Board sync är mandatory i varje command** — `/plan`, `/execute`, `/verify`, `/pr` uppdaterar alltid project-board status.

**Nyckel-citat ur `/pr` ([`commands/pr.md:26-50`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.claude/commands/pr.md)):**
> "Use `gh pr create` with Dojo Future formatting: `--title '<type>: <concise description>' --body ...`"
> PR-body strukturen: `## Summary / ## Changes / ## MYH Compliance / ## Test Plan / ## Cross-Repo Impact / Closes #[ISSUE_NUMBER]`.

**Nyckel-citat ur `/verify` ([`commands/verify.md:14-25`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.claude/commands/verify.md)):**
> "Run `dotnet build --no-restore 2>&1 | tail -10`... `dotnet test --no-build --verbosity minimal 2>&1 | tail -20`... Check for: Unintended file changes, Hardcoded secrets or URLs, Missing error handling at system boundaries..."

#### Skills

13 BE-skills (hälften är scaffolding, hälften är workflow). Format: `.claude/skills/<name>/SKILL.md` med YAML-frontmatter.

**Citat ur `skills/session-start/SKILL.md`:**
> "Loads context from previous sessions and reports status... Read `docs/claude/SESSION_HISTORY.md`... `git log --oneline -20`... Ask the user what they want to work on today."

**Citat ur `skills/add-command/SKILL.md`:**
> "Creates a new MediatR command following the project's CQRS pattern... Create the following files in `src/DojoFuture.Application/Features/{Feature}/Commands/{Action}/`: {Action}Command.cs, {Action}CommandHandler.cs, {Action}CommandValidator.cs..."

#### Hooks (`.claude/settings.json`)

**BE ([`settings.json:31-53`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.claude/settings.json)):**

```json
"hooks": {
  "PostToolUse": [{
    "matcher": "Edit|Write",
    "hooks": [{
      "type": "command",
      "command": "bash -c 'FILE=$(echo \"$TOOL_INPUT\" | jq -r \".file_path // empty\" 2>/dev/null); if [ -n \"$FILE\" ] && echo \"$FILE\" | grep -qE \"\\.(cs|csproj)$\"; then echo \"[hook] .NET file modified: $FILE — run dotnet build before commit\"; fi'"
    }]
  }],
  "UserPromptSubmit": [{
    "matcher": "",
    "hooks": [{
      "type": "command",
      "command": "bash -c 'if [ -f \".planning/STATE.md\" ]; then echo \"[context] Active plan detected — read .planning/STATE.md for current state\"; fi'"
    }]
  }]
}
```

**Två hooks, båda lätta:**
1. `PostToolUse` på `Edit|Write` → påminner om att köra `dotnet build` om `.cs`-fil ändrats.
2. `UserPromptSubmit` → om `.planning/STATE.md` finns, påminner om aktivt plan.

**FE gör samma sak men för `.ts/.tsx` + `npm run lint`.**

Det är diskret, informativt, icke-blockerande. Vi kan vara mer ambitiösa (se §3.4 nedan).

#### Permissions

BE `settings.json` (team-shared, committerad) — **endast säkra kommandon**:
```
dotnet build/test/run/restore/ef/tool/new/sln/add, git (alla vanliga), ls/tree/curl/gh, WebSearch
```

BE `settings.local.json` — mer omfattande, personlig, gitignored. Visar att Nemo tillåter `docker exec`, `ssh`, `Read(~/.claude/**)`, 20+ specifika `WebFetch(domain:...)`.

**Slutsats för JobbPilot:** `settings.json` committas med minimal allowlist; varje utvecklare har egen `settings.local.json` som gitignoras.

#### Namnkonventioner

- **Alla `.claude/`-filer:** kebab-case (`code-reviewer.md`, `feature-scaffolder.md`, `session-start/`, `add-command/`).
- **Reference-maps:** `<thing>-complete-map.md` eller `<thing>-status.md`.
- **Commands:** kort verb (`plan`, `execute`, `verify`, `pr`).
- **Patterns:** `<thing>-full-template.md` eller `<thing>-template.md`.

#### Planning-templates (`.claude/templates/planning/`)

Tre templates:
- `STATE.md` — fas-för-fas planering.
- `PHASE.md` — per-fas detalj.
- `SUMMARY.md` — feature completion report.

Genereras av `/plan`, uppdateras av `/execute`, läses av alla andra commands.

#### Docs-struktur (parallellt med .claude/)

**BE** ([`dojo-future-be/CLAUDE.md:655-747`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/CLAUDE.md)):

```
docs/claude/
├── ARCHITECTURE.md
├── API_REFERENCE.md
├── CODING_STANDARDS.md
├── BUSINESS_LOGIC.md
├── SESSION_HISTORY.md            ← uppdateras varje session-slut
└── 2026-02-19-STAGING-DEPLOYMENT.md   ← detaljerad record
```

**Plus `.planning/STATE.md`** under aktiv feature-utveckling (läses av /execute, /verify, /pr).

#### PR-flöde & commit-konventioner

- **Branches:** `development` (main), feature-branches, conventional-commits i meddelanden.
- **Commits:** `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`, `build`, `ci`. Scope = kontext.
- **PR-body** (via `/pr`): `## Summary / ## Changes / ## MYH Compliance / ## Test Plan / ## Cross-Repo Impact / Closes #N`.
- **Co-authored-by-trailer:** `Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>` (vi uppdaterar till 4.7).
- **Project board auto-sync:** varje `/plan /execute /verify /pr` skriver till GitHub Project #14 via `gh project item-edit` med pinade field-ID:n.

#### CI/CD (GitHub Actions)

BE har tre workflows ([`dojo-future-be/.github/workflows/`](file:///c:/DOTNET-UTB/DOJO-LMS/dojo-future-be/.github/workflows/)):

- `backend-build-and-test.yml` — PR-trigger, kör `dotnet build` + unit + integration + upload artifacts + Discord-notification.
- `backend-deploy-staging.yml` — push till `development`, manuell approval via GitHub Environments → Docker build → ghcr.io → SSH deploy till GleSYS VPS.
- `backend-deploy-production.yml` — motsvarande för `main`.

**Discord-integration** med color-coded embeds för CI-resultat är ett trevligt extra som vi kan replikera senare.

### 2.3 `CLAUDE.md`-struktur (mental map)

LMS BE:s CLAUDE.md är **38 kB**. Mycket mer omfattande än JobbPilots 16 kB. Skillnaden är att LMS har byggt upp CLAUDE.md **efter** att features etablerats — det är en levande "mental map" med tech-stack, entity-count, CQRS-count, endpoint-count som auto-uppdateras via `/update-docs`-skill.

**JobbPilots CLAUDE.md är bra som det är för fas 0.** När vi har entities och commands bör vi addera motsvarande "mental map"-sektioner.

**Struktur LMS CLAUDE.md använder (värt att matcha):**

1. Title + status + repo-länkar
2. **SESSION PROTOCOL (MANDATORY)** — läs current-work.md + SESSION_HISTORY.md vid start, uppdatera vid slut.
3. Domän-compliance-block (MYH for LMS, kan bli GDPR/civic-utility för JobbPilot).
4. **WORKFLOW COMMANDS-tabell** — alla /commands.
5. Quick Context (2–3 meningar).
6. Tech Stack-tabell med exakta versioner.
7. CODE STANDARDS (Result pattern, DTO guidelines, validation, repository, error handling, clean code principles, testing requirements).
8. Architecture Structure (ASCII-träd).
9. Quick Reference Paths (var viktig kod ligger).
10. CQRS Summary (auto-genererad).
11. Core Entities (auto-genererad).
12. API Endpoints Summary (auto-genererad).
13. Critical Rules.
14. Common Tasks (step-by-step för "add new entity / command / query").
15. Deployment (miljöer, arkitektur, CI/CD).
16. Documentation Index (vilka filer läser Claude när).
17. Production Readiness Checklist.

### 2.4 Vad som är unikt för JobbPilot (inte i LMS)

- **BYOK AI-keys** — LMS har bara intern Cloudinary/SMTP-auth. Vi behöver en `byok-security`-agent och en `/ai-key-rotate`-skill.
- **Prompts som `.prompt.md`-filer** — LMS har inga AI-prompts. Vi behöver en `/add-prompt`-skill och ev. en `prompt-versioner`-agent.
- **Civic-utility design system** — LMS använder Mosaic/generic shadcn. Vi måste ha en `design-reviewer`-agent som kollar DESIGN.md §5–12 (emoji-free, radius ≤6px, kontrast, svensk copy).
- **GDPR-flöden** — LMS har MYH-compliance; vi behöver en `gdpr-reviewer`-agent som checkar §13.
- **Svensk-först-copy** — LMS har `swedish-education.md` för domän-vokabulär. Vi behöver `swedish-job-market.md` (SSYK, YH/Yrgo, platsbanken-terminologi, svenska rekryterings-konventioner).
- **JobTech-integration** — unik domän. En skill `/add-job-source` för att lägga `IJobSource`-implementationer.

---

## 3. Rekommendationer — vad vi gör i session 2

### 3.1 Återanvänd rakt av från LMS

✅ **Hela `.claude/`-layouten** (agents/commands/skills/rules/patterns/templates).

✅ **Namnkonventioner** (kebab-case, `-complete-map.md`, `add-<thing>` skills).

✅ **`current-work.md`** som session-tracker.

✅ **Planning-triaden** (`STATE.md / PHASE.md / SUMMARY.md` i templates/planning/).

✅ **Commands: `/plan /execute /verify /pr /pause /resume /progress /status /scope /review /security /cross-repo /orchestrate /team-status /sync-board`** — men med JobbPilot-anpassning (GitHub project-ID byts, Discord-webhook optional).

✅ **Settings-dualism**: `settings.json` committed med minimal allowlist, `settings.local.json` gitignored.

✅ **Skills-mönster**: `<verb>-<noun>` (`add-command`, `add-entity`, `add-endpoint`, `update-maps`, `update-docs`, `session-start`, `session-end`).

✅ **Hooks**: BE-stilens diskreta påminnelse-hooks (PostToolUse på Edit|Write, UserPromptSubmit för planning-context).

✅ **Rules/-directory**: `architecture.md`, `coding-standards.md`, `testing.md` — separata från CLAUDE.md så de kan path-scopas (ny Claude Code-feature!).

### 3.2 Modifiera för JobbPilot

🔧 **Agents** — byt namn och scope:
- `code-reviewer` → samma namn, men checklistan uppdateras mot JobbPilot CLAUDE.md §5 anti-patterns.
- `feature-scaffolder` → scoped till JobbPilots Clean Arch (5 projekt istället för 4 — Api+Worker+Application+Infrastructure+Domain).
- `test-writer` → byt `FakeItEasy+Shouldly` mot **xUnit + FluentAssertions + NSubstitute** (vilket är CLAUDE.md §17-ekvivalenten — obs! FluentAssertions är licensierad sen 2025, se version-audit).
- `migration-helper` → identisk.
- `map-updater` → identisk.
- **Nya för JobbPilot:**
  - `domain-invariant-checker` — reviewer scoped till `Domain/`.
  - `design-reviewer` — UI-PR-reviewer mot DESIGN.md.
  - `gdpr-reviewer` — GDPR §13-checklist.
  - `byok-security-reviewer` — kryptering, loggning, KMS-flöde.
  - `swedish-copy-reviewer` — UI-copy vs DESIGN.md §8.
  - `api-integrator` (FE) — samma pattern som LMS FE.
  - `prompt-versioner` — för `/prompts/*.prompt.md`.

🔧 **Skills** — mesta detsamma:
- Samma `add-command`, `add-query`, `add-entity`, `add-endpoint`, `update-maps`, `session-start/-end`, `build`, `test`, `coverage-report`, `lint`.
- **Nya:**
  - `add-aggregate` (för nytt Aggregate Root i Domain).
  - `add-domain-event`.
  - `add-prompt` (skapa ny `.prompt.md`-fil i `/prompts/`).
  - `add-ai-operation` (CQRS + credit-vikt + token-tracking).
  - `add-job-source` (IJobSource-implementation).
  - `add-hangfire-job`.

🔧 **Rules** — JobbPilot-specifika:
- `architecture.md` — Clean Arch + DDD aggregate rules från CLAUDE.md §2.1–2.3.
- `coding-standards.md` — CLAUDE.md §3.
- `testing.md` — CLAUDE.md §17.
- `design-system.md` — DESIGN.md-kondensat + §5 anti-patterns.
- `gdpr.md` — §13.
- `ai-layer.md` — §8 (prompts, credits, BYOK).
- `swedish-language.md` — §10 + DESIGN.md §8.
- `swedish-job-market.md` — SSYK, YH, platsbanken-vokabulär.
- `anti-patterns.md` — CLAUDE.md §5 (dir-load-path vid vissa filer via `paths:`-frontmatter).

🔧 **Patterns/-directory** — add:
- `aggregate-root-template.md`
- `command-full-template.md`
- `query-full-template.md`
- `domain-event-template.md`
- `ai-prompt-template.md`
- (FE) `page-template.md`, `server-component-template.md`, `client-component-template.md`, `form-template.md`, `query-hook-template.md`

🔧 **Docs/claude-struktur**:
- `docs/claude/SESSION_HISTORY.md` — samma pattern.
- `docs/claude/ARCHITECTURE.md` — detaljerad arkitektur.
- `docs/claude/CODING_STANDARDS.md` — expandering av CLAUDE.md §3.
- `docs/ADR/NNNN-slug.md` — som redan planerat i BUILD.md Bilaga B.

### 3.3 Skippa eller skjut upp

❌ **GitHub Project Board-auto-sync i /commands** — LMS har hårdkodade `PVTSSF_*`-field-ID:n mot Project #14. Vi kan inte återanvända detta förrän vi har egen project board. Skjut till fas 1 eller senare.

❌ **Discord-notifications i CI** — trevligt men inte nödvändigt. Skjut till fas 6+.

❌ **Separat `/superadmin`-portal-kod** — vårt admin är mindre komplext i v1.

❌ **Cloudinary-service-integration** — vi använder S3 istället (§3.2 infra).

❌ **MYH-compliance** — helt irrelevant för oss. Ersätt med civic-utility + GDPR.

### 3.4 Utöka utöver LMS (pga Opus 4.7 + 2026-features)

🆕 **`SessionStart`-hook med context-injection** (Pattern 2 från §1.3) — LMS kör det via prompt, vi gör det via hook så det alltid händer.

🆕 **`PreToolUse`-hook på `Bash(git commit*)`** (Pattern 4) — dubblar vårt Husky pre-commit som backup.

🆕 **`auto` permission-mode** (nytt 2026) — sätt `effortLevel: "xhigh"` och `autoMode` med rimliga rules för att låta Claude köra mer autonomt på säker kod.

🆕 **MCP Context7** — för Next.js 16/EF Core 10/Tailwind 4.2 docs. Lägg till först av alla MCP.

🆕 **Sub-agent frontmatter med `isolation: worktree`** för saker som `feature-scaffolder` där vi vill att agenten ska köra isolerat och vi kan review:a innan merge.

🆕 **`.claude/skills/<name>/SKILL.md`-form** istället för `.claude/commands/<name>.md` där det passar (commands är nu en delmängd av skills).

---

## 4. Öppna frågor till Klas

> **Klas — du behöver besvara dessa innan session 2 kör igång.**

1. **Co-authored-by-trailer:** LMS använder `Co-Authored-By: Claude Opus 4.6`. Ska vi uppdatera till `Claude Opus 4.7` nu? (Ja/nej)

2. **Project board:** Ska vi skapa en GitHub Project Board för JobbPilot direkt i fas 0, eller skjuta? Om ja: vilken owner (din personliga eller en org)?

3. **Discord/Slack notifieringar:** Vill du ha CI-notifieringar i fas 0, eller skippa till fas 6?

4. **LMS `CLAUDE.local - BE.md` + `CLAUDE.local.md`:** Vill du ha en personlig `.claude/CLAUDE.local.md` (gitignored) för saker som inte går in i committed CLAUDE.md? Det är det Nemo använder för sina Computer-specifika paths och snabba anteckningar.

5. **MCP-servrar:** OK att jag lägger in Context7 (docs-injection) direkt i fas 0? Det är gratis (Upstash) och sparar oss mycket tid. Playwright+Sentry väntar tills fas 5/6.

6. **Auto-mode:** Jag såg att du kör med auto-mode aktiverat nu. Vill du att det ska vara default i projektet via `.claude/settings.json` eller hålla det personligt?

7. **Test-framework:** CLAUDE.md §17 säger xUnit + FluentAssertions. Men **FluentAssertions blev commercial license 2025** (USD 130/dev/yr från v8). Antingen (a) pinna till FluentAssertions v7 under Apache-2.0, (b) byta till **Shouldly** som LMS gör, eller (c) betala. Vad föredrar du? (Rekommendation: Shouldly — gratis, bra DX, LMS-kompatibel, ingen risk att licensen ändras igen).

8. **MediatR:** MediatR 12.x är fortfarande OSS under Apache-2.0, men **MediatR 14.x (2025-07-02)** är Community-licens "gratis under USD 5M revenue" men kommersiell över. JobbPilot kvalificerar men är inte framtidssäker. Ska vi byta till **Mediator.SourceGenerator** (martinothamar, MIT, zero-runtime-cost)? Se version-audit för detaljer. (Rekommendation: ja — swap:en är enkel på greenfield).

9. **QuestPDF:** Samma licens-situation. Community MIT gratis under USD 1M revenue. OK?

10. **Next.js 15 vs 16:** BUILD.md säger 15. **Next.js 16 är GA sen Q1 2026.** Ska nya projekt verkligen starta på 16? Det är +1 major. (Rekommendation: ja, 16.2.x — den stabila linjen just nu. Men det är din call).

---

## 5. Provenans & osäkerheter

- **Alla Claude Code-doc-citat** är från WebFetch mot `code.claude.com/docs/en/*` idag 2026-04-18.
- **Alla Opus 4.7-facts** är från anthropic.com + platform.claude.com + publicerade benchmark-sidor från 16–18 april 2026.
- **LMS-citat** är verifierade via direkt filläsning med exakta sökvägar; alla länkar är `file:///`-prefixed.
- **Version-audit** (separat dokument) har WebFetch/WebSearch-källor för varje rad.
- **Osäkerhetsflaggor:**
  - Cache-pricing för Opus 4.7 är inte explicit bekräftad som oförändrad — source säger "unchanged pricing" men räcker inte för att veta om cache-raterna gäller lika. Verifiera mot Anthropic pricing-sidan innan vi räknar credit-kostnader i §8.3.
  - Next.js 16.2.3 exakt version är rimlig men inte dubbel-bekräftad — pin via `pnpm view next version` innan vi skriver `package.json`.
  - TypeScript 6.0.3 släpptes enligt källa 2026-04-16 — supernytt, vi kan initial pinna till 5.x och uppgradera tidigast efter 6.0.x.minor++.

---

## 6. Öppna frågor — beslut

> **Datum:** 2026-04-18 (efter Klas review av denna fil).
> Sektionen dokumenterar Klas beslut. Ordningen följer hans beslutsnoterings-nummer 1–11 (inte ordningen i §4). Några beslut matchar frågor i §4; andra är nya riktningar som ersätter eller utökar §4.

### 6.1 Agenter från LMS — kuratera urvalet

**Originalfråga:** Ska vi kopiera alla 13+ LMS-agenter blint till JobbPilot?

**[BESLUTAT]** Nej. Claude Code avgör vilka som är essentiella för JobbPilot v1 och motiverar urvalet i session 2-planen. Kriterier:
- Behövs agenten för JobbPilots specifika domän (jobbansökningar)?
- Finns en feature i BUILD.md som direkt kräver den?
- Är den ett CTO-krav oavsett domän (code-reviewer, security-auditor)?

Skippa LMS-specifika agenter (T-SQL-writer om vi kör EF Core migrations, etc.) och klart över-specialiserade agenter.

**Planimplikation:** Session 2-plan måste innehålla en tabell "agent → återanvänd / modifiera / ersätt / skippa" med motivering per rad. Samma format som §3.1–3.3 men mer detaljerat per agent.

### 6.2 Modellstrategi för Claude Code-subagenter

**Originalfråga:** Vilken Claude-modell ska varje subagent använda?

**[BESLUTAT]** Default: `sonnet` för alla agenter. Undantag: `opus` för `security-auditor` och `code-reviewer` (kritiska kvalitetsagenter där träffsäkerhet väger tyngre än kostnad). `haiku` används INTE för agenter — för snabb för granskning.

OBS: Detta gäller modellerna Claude Code-SUBAGENTERNA använder. JobbPilot-appens EGEN AI (via Bedrock + BYOK) har separat mix enligt BUILD.md §8.2 (Haiku för CV-parsing, Sonnet för brev-generering, etc.).

**Planimplikation:** Varje `.claude/agents/*.md` får explicit `model:`-frontmatter (`sonnet` eller `opus`). Session 2 producerar en tabell "agent → modell → motivering". Notera att vi inte använder 2026:s nya `effort: xhigh` per default — `sonnet` + standard effort är billigare och Klas kan öka per case.

### 6.3 Session-protokoll från LMS

**Originalfråga (implicit från §2.3):** Ska vi replikera LMS:s SESSION PROTOCOL (start läser current-work.md, end uppdaterar)?

**[BESLUTAT]** Ja, kopiera LMS-mönstret rakt av. Start läser `current-work.md`; end skriver till både det och `SESSION_HISTORY.md`. Språk: se §6.5 nedan.

**Planimplikation:** Filplaceringen är liten men viktig. LMS kör `.claude/current-work.md` + `docs/claude/SESSION_HISTORY.md`. Klas beslut nämner `docs/current-work.md` — session 2 måste lösa placeringen. **Rekommendation:** behåll LMS-konventionen (`.claude/current-work.md` + `docs/claude/SESSION_HISTORY.md`) eftersom (a) den är etablerad, (b) `.claude/`-directory håller Claude-artefakter samlat, (c) `SessionStart`-hook-injiceringen från Pattern 2 (§1.3) kan enkelt peka på den. Bekräfta med Klas i början av session 2.

### 6.4 Mediator-val

**Originalfråga:** MediatR 12 (OSS) vs Mediator.SourceGenerator (MIT)?

**[BESLUTAT]** **Mediator.SourceGenerator** (martinothamar/Mediator). MIT-licens, source generators, Native AOT-kompatibelt. Motsvarar §6.8 i mina ursprungliga §4-frågor + rad 5 i version-auditen. MassTransit används INTE i v1. Om message bus behövs senare läggs MassTransit eller Wolverine på som additivt lager ovanför.

**Planimplikation:** BUILD.md §3.1 tabellen, §4.4 (pipeline behaviors), §6 (API-konventioner), §17 (tester) behöver söka-och-ersätta "MediatR" → "Mediator". Pipeline-syntax skiljer sig något (source-generator-baserad registrering istället för assembly scan); session 2 dokumenterar diffen i den patch som uppdaterar BUILD.md.

### 6.5 Skills — språkstrategi (hybrid)

**Originalfråga:** Vilket språk ska `.claude/skills/*.md` skrivas på?

**[BESLUTAT]** Hybrid:
- **Tekniska skills** (testing, architecture, migrations, git) skrivs på **engelska** (matchar internationell .NET-terminologi och LMS-bas).
- **Användar-facing återkoppling** (PR-kommentarer, review-rapporter som Klas läser) på **svenska**.
- `code-reviewer`-agenten skriver rapporter på svenska men behåller engelska tekniska termer ("Domain layer", "aggregate root", "handler", "validator").

**Planimplikation:** Varje skill som producerar output Klas ser måste ha explicit instruktion "Skriv rapporten på svenska, behåll engelska tekniska termer." Scaffolding-skills (add-command, add-entity) skriver KOD på engelska enligt CLAUDE.md §3.2, men deras interaktiva dialog (frågor till Klas, slutstatus) är på svenska.

### 6.6 Skills från LMS

**Originalfråga:** Återanvänd samma skills som LMS eller designa om?

**[BESLUTAT]** Samma skills som LMS som bas, med tillägg:
- Användar-facing text översatt till svenska (§6.5).
- JobbPilot-specifika skills enligt BUILD.md §4: `jobbpilot-domain`, `jobbpilot-design-system`, `jobbpilot-ai-prompts`, `jobbpilot-gdpr`.

**Planimplikation:** Session 2 måste motivera varje LMS-skill individuellt: **återanvänd direkt / modifiera / ersätt / skippa**. Minst dessa LMS-skills kräver JobbPilot-modifiering: `add-command`, `add-query`, `add-entity`, `add-endpoint` (Clean Arch-paths skiljer), `update-maps` (våra maps blir andra), `coverage-report` (våra coverage-mål skiljer mot LMS).

### 6.7 CI/CD-timing

**Originalfråga:** När i roadmapen ska GitHub Actions sättas upp?

**[BESLUTAT]** Claude Code-setup FÖRST (sessions 2–4), sedan GitHub Actions som en del av Fas 0 Foundation i BUILD.md §18. CI:n ska köra samma hooks/agenter som lokalt → den måste komma efter att den lokala setupen är validerad.

**Planimplikation:** Session 2-plan placerar GitHub Actions EFTER att `.claude/`-strukturen är på plats och testad. Ordning: **session 2 `.claude/`-setup → session 3 AWS + Terraform → session 4 CI/CD**. BUILD.md §15.3 workflow-filer skrivs inte i session 2.

### 6.8 Secrets management

**Originalfråga (implicit från BUILD.md §13.2):** Hur hanteras secrets i dev vs staging/prod?

**[BESLUTAT]** Pragmatisk split:
- `.env`-fil i dev (gitignored, `.env.example` committad).
- AWS Secrets Manager i staging + prod.
- `IConfiguration`-abstraktionen gör att koden är identisk oavsett källa — bara DI-registreringen skiljer.
- **BYOK-nycklar ALDRIG i `.env` eller env-vars**; alltid KMS envelope enligt BUILD.md §8.4.

**Planimplikation:** BUILD.md §3.2 behåller AWS Secrets Manager för staging + prod men session 2 lägger till explicit dev-`.env`-flöde. Session 2-fil-leveranser inkluderar `.env.example` med alla nycklar listade (men inga värden). `.gitignore` måste redan ha `.env` och `.env.local`.

### 6.9 AWS-konto

**Originalfråga (implicit från BUILD.md §15.1):** Använd Klas befintliga AWS-konto eller separat?

**[BESLUTAT]** Separat AWS-konto för JobbPilot.
- Skapa nytt konto via Root-konto i AWS Organizations om Klas redan har en organization.
- Annars fristående konto med egen root-email.
- MFA på root direkt; IAM-användare/SSO för daglig användning; root används aldrig efter första setup.

**Planimplikation:** Session 3 steg 2 måste verifiera: (a) AWS-konto skapat, (b) AWS CLI konfigurerad, (c) MFA på root. **Inbyggd stoppunkt** i session 3-planen — Terraform kan inte köras innan detta är klart. Session 2 bör lämna en runbook-lista till Klas att göra klart mellan session 2 och 3.

### 6.10 Branching-strategi

**Originalfråga (implicit från BUILD.md §6.1):** Git Flow (main + develop + release) eller enklare?

**[BESLUTAT]** **GitHub Flow**:
- `main` = production-ready, skyddad branch.
- Feature-branches direkt från `main`.
- PR → `code-reviewer`-agent auto-körs → tester hard gate → merge.
- **Squash-merge till `main`** (ren historia).
- INGA `develop`-branch, INGA release-branches.

**Planimplikation:** BUILD.md §6.1 och §18 nämner `develop` + `staging`-branch — detta ska bort i session 2. `staging` blir en **environment**, inte en branch (deploys från tagged commits på `main`). Detta påverkar också session 4:s GitHub Actions — trigger-matriser blir enklare (PR till main, push till main, tagged release).

### 6.11 Docker Compose-profiler

**Originalfråga (implicit från BUILD.md §11.3 dev env):** Hur struktureras `docker-compose.yml` för dev/test/e2e?

**[BESLUTAT]** Använd Compose profiles:
- `default` (tom profile) = `postgres-dev`, `redis-dev`, `seq`.
- `test` profile = `postgres-test` (port 5433), `redis-test` (port 6380).
- `full` profile = allt ovan + frontend-container för integration-debugging.

Kommandon: `docker compose up -d` (dev), `docker compose --profile test up -d` (tester), `docker compose --profile full up -d` (E2E).

**Planimplikation:** Session 2 skapar `docker-compose.yml` i repo-root med denna struktur direkt. Ingen separat `docker-compose.test.yml`-fil. Compose Specification v5-format (inget top-level `version:`-nyckel, `depends_on` med `condition: service_healthy` för ordning).

---

## 7. Verifierade fakta-fynd (från Klas review)

Fynd som Klas dubbel-verifierat mot primärkällor i samband med sitt beslutsdokument.

### 7.1 Bedrock modell-ID (verifierade mot AWS docs 2026-04-18)

**Base model IDs (bekräftade):**
- `anthropic.claude-sonnet-4-6` — **INGEN datumsuffix**
- `anthropic.claude-haiku-4-5-20251001-v1:0`
- `anthropic.claude-sonnet-4-5-20250929-v1:0` (tidigare generation, referens)

**EU inference profile-prefix:** `eu.anthropic.claude-sonnet-4-6`-format i allmänt mönster.

⚠️ **VIKTIGT:** Exakta inference profile-ARNs **varierar per källregion** och får INTE cachas från docs eller blogg. Session 3 steg 2 ska köra `aws bedrock list-inference-profiles --region eu-north-1` när AWS-kontot är på plats och dokumentera output i `docs/research/bedrock-inference-profiles.md`. Först därefter skrivs slutgiltiga model-ID:n in i BUILD.md §8.2.

### 7.2 MediatR — verifierade licens-villkor

- LuckyPennySoftware/MediatR **v13+** är commercial under **RPL-1.5** (Reciprocal Public License 1.5).
- Community-edition är gratis för organisationer **under USD 5M revenue**, för utbildning och non-production.
- JobbPilot kvalificerar idag men vi väljer **Mediator.SourceGenerator** ändå (se §6.4) för framtidssäkring + prestanda (source-generators, Native AOT).

### 7.3 Anthropic C# SDK — officiellt val

- Officiell SDK: NuGet-paketet **`Anthropic`** version **10+** (tidigare `Anthropic` 3.x var tryAGI community, flyttad till `tryAGI.Anthropic`).
- Senaste version (verifierad av Klas): **12.11.0**, kräver .NET Standard 2.0+.
- Alternativ: `Anthropic.SDK` (tghamm, community) v5.10.0 — fortsatt underhållet, har vissa features snabbare än officiell.
- **Rekommendation för BYOK-flöde: officiella `Anthropic` v12+** för långsiktig stabilitet.

> Not: SESSION-1-VERSION-AUDIT.md rad 16 hade `Anthropic 12.16.0` från subagent-source; Klas verifierade 12.11.0. Mindre delta, kan vara en minor-release mellan sökningarna. Session 3 bekräftar exakt version i package.json.

### 7.4 FluentAssertions — bekräftat commercial 2025

- Projektet togs över av **Xceed** under 2025. Licensen bytte till **Xceed Community License** (USD 130/dev/år från v8).
- **Ersättning:** Shouldly (MIT, enklare API, redan använt i LMS).
- **Alternativ ej valt:** AwesomeAssertions (MIT fork av FA med **identisk API** — kan övervägas om en team-member är djupt investerad i FA-syntax). JobbPilot är solo-dev-grund så Shouldly vinner på enkelhet.

**Planimplikation för tidigare §4-fråga 7 (test-framework):** avgjord — **Shouldly** används. CLAUDE.md §17 uppdateras i session 2.

---

**Slut på SESSION-1-FINDINGS.md.** Fortsätt till [`SESSION-1-VERSION-AUDIT.md`](./SESSION-1-VERSION-AUDIT.md) för konkreta version-ändringar i BUILD.md §3.1–3.2 (nu kompletterad med verifierade Bedrock-ID:n och session 3-verifieringsnot).
