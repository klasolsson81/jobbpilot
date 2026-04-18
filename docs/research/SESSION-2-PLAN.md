# Session 2 — Claude Code-setup för JobbPilot

> **Status:** Plan only. Inget implementerat i denna session.
> **Datum:** 2026-04-18
> **Scope:** Exakt specifikation av `.claude/`-strukturen + tillhörande docs/ + GitHub + Docker + AWS för JobbPilot.
> **Bindande beslut:** alla referenser till "Klas-beslut #N" pekar till [`SESSION-1-FINDINGS.md §6.N`](./SESSION-1-FINDINGS.md).
> **Kvalitetsreferens:** Nemo's LMS-projekt, `c:\DOTNET-UTB\DOJO-LMS\`. Avvikelser från LMS motiveras per sektion.

---

## 0. Språk- och ton-regel (bindande för alla agenter/skills/hooks)

Från Klas-beslut #5:

| Yta | Språk |
|-----|-------|
| Agent-instruktionsfiler (`.claude/agents/*.md`) — system prompt, checklista | **Engelska** |
| Skill-instruktionsfiler (`.claude/skills/*/SKILL.md`) — processbeskrivning | **Engelska** |
| Rule-filer (`.claude/rules/*.md`) — tekniska konventioner | **Engelska** (tekniska termer) |
| Commit-meddelanden, branch-namn | **Engelska** (conventional commits) |
| Kod-identifierare, filnamn | **Engelska** (per CLAUDE.md §3.2) |
| Code-reviewer-rapport till Klas | **Svenska** med engelska tekniska termer ("Domain layer", "aggregate root", "handler") |
| PR-beskrivning sektion-rubriker | **Svenska** (Varför / Vad / Hur testat) |
| PR-beskrivning tekniskt innehåll | **Svenska** + engelska termer |
| Issue-templates | **Svenska** |
| Tool-error-messages som Klas ser (t.ex. hook-varningar) | **Svenska** |
| UI-copy i JobbPilot-appen (inte Claude Code) | **Svenska** (per DESIGN.md §8) |
| Skill-interaktiv dialog ("Ska jag skapa X?") | **Svenska** |
| ADR-rubriker | **Svenska** |
| ADR-teknisk diskussion | **Svenska** + engelska termer |

**Praktisk regel för varje agent/skill:** Frontmatter och systemprompt på engelska. Sista stycket i systempromten: explicit instruktion "Report all findings to the user in Swedish, keeping English technical terms (Domain layer, aggregate root, handler, validator, etc.) untranslated."

---

## 1. Agenter (kurerat urval)

Per Klas-beslut #1: inte kopiera LMS:s 13 agenter blint. 8 essentiella agenter för v1 + motivering av LMS-urval.

### 1.1 Urval för JobbPilot v1

| # | Agent | Modell | LMS-motsvarighet | Motivering för JobbPilot |
|---|-------|--------|------------------|----------------------------|
| 1 | `code-reviewer` | **opus** | `code-reviewer` (BE + FE) | Kvalitets-vakt. Klas högsta prio (DEL 5). |
| 2 | `security-auditor` | **opus** | `/security` command (BE + FE) | BYOK + GDPR + OAuth — kritisk yta i JobbPilot (BUILD.md §8.4, §13). |
| 3 | `dotnet-architect` | sonnet | `feature-scaffolder` (BE) | Clean Arch-vakt + scaffolding av aggregates/handlers. |
| 4 | `nextjs-ui-engineer` | sonnet | `feature-scaffolder` + `page-builder` + `api-integrator` (FE) | Scaffolding av pages, komponenter, query hooks + civic-design-compliance. |
| 5 | `test-writer` | sonnet | `test-writer` (BE + FE) | Skriver xUnit + Shouldly + Vitest-tester innan implementation. |
| 6 | `db-migration-writer` | sonnet | `migration-helper` (BE) | EF Core 10 + PostgreSQL 18 + svensk kollation + soft delete. |
| 7 | `ai-prompt-engineer` | sonnet | *(ny — LMS har ingen)* | Hanterar `/prompts/*.prompt.md` för JobbPilot AI-layer (BUILD.md §8.5). |
| 8 | `docs-keeper` | sonnet | `map-updater` (BE + FE) + doc-uppdaterings-delar av `/pr` | Uppdaterar `current-work.md`, session-loggar, entity/cqrs-maps, ADRs. |

### 1.2 LMS-agenter som SKIPPAS (motivering)

| LMS-agent | Beslut | Motivering |
|-----------|--------|------------|
| `chart-builder` (FE) | Skippa | JobbPilot v1 har endast `/ansokningar/statistik` med charts. Gränsfallet hanteras av `nextjs-ui-engineer`. Ingen egen agent förrän v2. |
| `feature-scaffolder` (BE + FE) | Ersätts | Uppdelas i `dotnet-architect` + `nextjs-ui-engineer` + `test-writer` (per yta). LMS:s monolitiska "scaffold hela feature"-agent passar inte JobbPilots tvåspråk-split. |
| `map-updater` | Slås samman | Funktionaliteten faller under `docs-keeper`. Separat agent är onödig komplexitet. |
| `page-builder` (FE) | Slås samman | Inkluderad i `nextjs-ui-engineer`. |
| `api-integrator` (FE) | Slås samman | Inkluderad i `nextjs-ui-engineer` (BE→FE-mapping är ett kapitel i agent-instruktionen). |

### 1.3 Agent-specifikationer

För varje agent: fil, syfte, modell, tools, triggers, input, output, success-definition.

#### 1.3.1 `code-reviewer`

| Fält | Värde |
|------|-------|
| **Fil** | `.claude/agents/code-reviewer.md` |
| **Syfte** | Reviews staged changes or current diff against JobbPilot CLAUDE.md standards, flags violations with severity and fix hints. |
| **LMS-referens** | `dojo-future-be/.claude/agents/code-reviewer.md` + `dojo-future-fe/.claude/agents/code-reviewer.md` (slås ihop till en fil som handlar båda lager). |
| **Modell** | `opus` (Klas-beslut #2 — kritisk kvalitetsagent) |
| **Tools tillåtna** | `Read, Grep, Glob, Bash(git diff:*), Bash(git status:*), Bash(git log:*), Bash(dotnet build --verify-no-changes:*)` |
| **Tools blockerade** | `Write, Edit, Bash(git commit:*), Bash(git push:*)` (read-only granskare) |
| **Isolation** | Ingen (körs i huvudsession, har full kontext) |
| **Auto-trigger** | (a) **PostToolUse(TodoWrite)** — endast när task markeras `completed` OCH `git diff` visar kodändringar sedan senaste review (§4.6). **INTE per `Edit`/`Write`-operation** (skulle bli spammigt vid 15 edits/task). (b) Husky pre-commit (§4.8). (c) GitHub Action på PR (DEL 11). |
| **Manuell trigger** | `/review` slash command (DEL 3) |
| **Input** | `git diff` (staged eller HEAD) + lista av ändrade filer + relevant sektion av BUILD.md/CLAUDE.md/DESIGN.md |
| **Output** | Strukturerad svensk markdown-rapport (se DEL 5 format); alltid sparad i `docs/reviews/YYYY-MM-DD-HH-MM-<slug>.md` |
| **Success-definition** | Rapport genererad + inga "Kritiska fynd" → exit 0; minst ett kritiskt fynd → exit 1 (blockerar nedströms hooks) |

**Checklist (se DEL 5 för full text):**

1. Clean Arch-gränser (CLAUDE.md §2.1)
2. Anti-patterns (CLAUDE.md §5)
3. DESIGN.md-compliance för FE-ändringar
4. Diff-coverage ≥ 80% på Domain
5. Secrets-scan (BEGIN PRIVATE KEY, `sk-ant-`, `AKIA*`, etc.)
6. GDPR: ny PII → soft-delete + retention-tag?
7. Mediator.SourceGenerator-mönster (inte MediatR-kvarlevor)
8. Commit-meddelande conventional (om staged commit)

**System prompt (utkast, engelska):**

> You are the JobbPilot code reviewer. You review staged or unstaged changes against strict Clean Architecture, DDD, and civic-utility design rules documented in BUILD.md, CLAUDE.md, and DESIGN.md at the repository root. You are read-only: never Edit, Write, or commit. Your output is a markdown report to the user in Swedish, using English technical terms (Domain layer, aggregate root, handler, validator, value object, pipeline behavior, invariant) untranslated.
>
> Before starting, read:
> - `.claude/rules/clean-arch.md`
> - `.claude/rules/anti-patterns.md`
> - `.claude/rules/design-system.md` (only if frontend files are in diff)
> - `.claude/rules/gdpr.md`
> - `.claude/rules/mediator-pattern.md`
>
> Report format: see DEL 5 of `docs/research/SESSION-2-PLAN.md` (you have read access).

---

#### 1.3.2 `security-auditor`

| Fält | Värde |
|------|-------|
| **Fil** | `.claude/agents/security-auditor.md` |
| **Syfte** | Deep security review focused on BYOK key flow, OAuth token storage, GDPR PII handling, and common OWASP classes. |
| **LMS-referens** | `/security`-command (BE) + manuella checks. LMS har ingen egen agent; vi lyfter det till agent pga BYOK-komplexitet. |
| **Modell** | `opus` (Klas-beslut #2) |
| **Tools tillåtna** | `Read, Grep, Glob, Bash(git grep:*), Bash(gitleaks detect:*), Bash(dotnet list package --vulnerable:*)` |
| **Tools blockerade** | `Write, Edit, Bash(git commit:*), Bash(git push:*), Bash(curl:*)` |
| **Auto-trigger** | Husky pre-push-hook (DEL 4); GitHub Action på PR som ändrar filer under `src/JobbPilot.Infrastructure/AiProviders/`, `Auth/`, `Identity/` eller `Persistence/` |
| **Manuell trigger** | `/security` slash command |
| **Input** | Hela repot (eller diff på request) |
| **Output** | Svensk rapport i `docs/reviews/security-YYYY-MM-DD-<slug>.md`; grupperas: `## Kritiska / Viktiga / Nice-to-have` |
| **Success-definition** | 0 kritiska fynd |

**Checklist-specifika krav:**

1. BYOK-kryptering: varje `AiProvider*`-kodväg kontrolleras: nyckel plaintext får INTE loggas, INTE serialiseras, INTE skrivas till disk utan KMS envelope.
2. OAuth-tokens: `oauth_connections.encrypted_access_token` får aldrig exponeras via API, endast per-provider-handler.
3. Rate limiting: alla endpoints har rate-limiter (BUILD.md §13.5).
4. Dependency CVE-scan: `dotnet list package --vulnerable --include-transitive` och `pnpm audit` → rapportera nya högrisk-CVE:er.
5. Gitleaks-körning mot hela historiken vid första körningen, sen diff framåt.
6. Hårdkodade secrets: sök mönster `(sk-ant-|AKIA|AIza|ghp_|ghs_|Bearer eyJ)`.
7. CORS-konfig: inte `*` med credentials (CLAUDE.md §5.4).
8. JWT-hantering: inte i localStorage (CLAUDE.md §5.4, §13.2).

---

#### 1.3.3 `dotnet-architect`

| Fält | Värde |
|------|-------|
| **Fil** | `.claude/agents/dotnet-architect.md` |
| **Syfte** | Scaffolds new aggregates, entities, value objects, commands, queries, and domain events following Clean Architecture and DDD conventions. |
| **LMS-referens** | `dojo-future-be/.claude/agents/feature-scaffolder.md` (adapterat för JobbPilots 5-projekt-struktur) |
| **Modell** | `sonnet` |
| **Tools tillåtna** | `Read, Write, Edit, Glob, Grep, Bash(dotnet build:*), Bash(dotnet new:*), Bash(dotnet add:*), Bash(dotnet ef migrations add:*), Bash(dotnet sln:*)` |
| **Tools blockerade** | `Bash(git push:*), Bash(rm:*), Bash(dotnet ef database drop:*)` |
| **Auto-trigger** | Ingen (manuell via skill) |
| **Manuell trigger** | `/new-feature` (DEL 3) och `.claude/skills/add-aggregate/`, `add-command`, `add-query`, `add-domain-event` |
| **Input** | Feature-namn + kort beskrivning på svenska eller engelska |
| **Output** | Filer skapade i korrekt projekt-lager; `docs/current-work.md` uppdaterad; kort sammanfattning på svenska: "Skapade X fil(er), ändrade Y. Nästa steg: `/test` eller `/review`." |
| **Success-definition** | `dotnet build` passerar; alla nya filer matchar templates i `.claude/patterns/`; inga cross-lager-beroenden |

**Kärnregler i systemprompt:**

- Domain projekt får INTE importera `Microsoft.EntityFrameworkCore`, `MediatR`, `Mediator` (source-generator-package), `Microsoft.AspNetCore.*`, eller något Infrastructure-namespace.
- Application får importera Mediator-attributes + Domain + common interfaces, inget Infrastructure.
- Infrastructure implementerar Application-interfaces; EF Core, externa SDKs här.
- Api + Worker är composition roots.
- Aggregate roots: private set, ingen public setter för collections, alla state-transitions via explicita metoder (CLAUDE.md §2.2).
- Strongly-typed IDs per aggregate (record struct wrapping Guid).

---

#### 1.3.4 `nextjs-ui-engineer`

| Fält | Värde |
|------|-------|
| **Fil** | `.claude/agents/nextjs-ui-engineer.md` |
| **Syfte** | Scaffolds pages, components, query hooks, Zod schemas, and endpoint functions for the Next.js 16 frontend, enforcing civic-utility design system. |
| **LMS-referens** | Slås ihop av `dojo-future-fe/.claude/agents/feature-scaffolder.md` + `page-builder.md` + `api-integrator.md`. Motivering: JobbPilot FE är mindre än LMS FE (~15 pages v1 vs LMS:s 100+), och tre agenter är overkill. |
| **Modell** | `sonnet` |
| **Tools tillåtna** | `Read, Write, Edit, Glob, Grep, Bash(pnpm run build:*), Bash(pnpm run lint:*), Bash(pnpm tsc:*), Bash(pnpm add:*), Bash(npx shadcn:*)` |
| **Tools blockerade** | `Bash(git push:*), Bash(rm -rf:*), Bash(curl:*)` |
| **Auto-trigger** | Ingen |
| **Manuell trigger** | `/new-feature` (när scope är FE), `.claude/skills/add-page/`, `add-component/`, `add-query-hook/`, `add-form/` |
| **Input** | Feature-namn + svenska UI-copy |
| **Output** | Filer i `web/jobbpilot-web/src/` enligt patterns; `messages/sv.json` uppdaterad; svensk dialog om nästa steg |
| **Success-definition** | `pnpm run build` passerar; `pnpm run lint` 0 errors; `pnpm tsc --noEmit` grönt; inga emoji eller utropstecken i JSX-strängar |

**Kärnregler i systemprompt:**

- Server components som default; `'use client'` endast där interaktivitet krävs.
- Alla hårdkodade strängar → `messages/sv.json` (next-intl).
- Form-state: React Hook Form + Zod; ingen oberoende `useState` för stora formulär (CLAUDE.md §4.3).
- Data fetching via TanStack Query; query keys i `lib/api/query-keys.ts`.
- Shadcn/ui customiserat mot DESIGN.md-tokens (radius ≤ 6px, myndighetsblå, inga gradients).
- BE→FE type-mapping: Guid→string, DateTime→ISO-string, decimal→number, PascalCase→camelCase (Axios).
- Rutterna på svenska: `/ansokningar`, `/jobb`, `/installningar` (BUILD.md §10.1).
- ARIA-etiketter svenska; fokusring synlig (DESIGN.md §9).

---

#### 1.3.5 `test-writer`

| Fält | Värde |
|------|-------|
| **Fil** | `.claude/agents/test-writer.md` |
| **Syfte** | Writes xUnit + Shouldly (backend) and Vitest + RTL (frontend) tests, ideally before implementation. Integration tests via Testcontainers. |
| **LMS-referens** | `dojo-future-be/.claude/agents/test-writer.md` + `dojo-future-fe/.claude/agents/test-writer.md`. **Viktig ändring:** Shouldly ersätter LMS:s FakeItEasy-bias — BE använder **NSubstitute** för mockar (CLAUDE.md §17), inte FakeItEasy. |
| **Modell** | `sonnet` |
| **Tools tillåtna** | `Read, Write, Edit, Glob, Grep, Bash(dotnet test:*), Bash(pnpm test:*), Bash(pnpm vitest:*), Bash(docker compose --profile test:*)` |
| **Tools blockerade** | `Bash(git push:*), Bash(rm:*)` |
| **Auto-trigger** | PostToolUse-hook efter `dotnet-architect` eller `nextjs-ui-engineer` skapat nya filer (om `.claude/settings.json` tillåter det — annars manuell) |
| **Manuell trigger** | `/test --write <path>`, `.claude/skills/add-test/` |
| **Input** | Path till handler/aggregate/komponent, eller diff |
| **Output** | Test-filer i `tests/<project>.UnitTests/` eller `__tests__/`; svensk sammanfattning "Skapade X tester mot Y, täcker happy-path + Z felscenarion." |
| **Success-definition** | Alla nya tester passerar; diff-coverage ≥ 80%; namngivningsmönster `<ClassUnderTest>_<Scenario>_<Expected>` (CLAUDE.md §3.2) |

**Shouldly-syntax i systemprompt (för kopieringsförhindring):**

```csharp
result.IsSuccess.ShouldBeTrue();
result.Value.ShouldNotBeNull();
result.Value.Email.ShouldBe("test@example.com");
action.ShouldThrow<DomainException>();

// INTE FluentAssertions-syntax (licens-hinder):
// result.Should().BeTrue();  ❌
// INTE xUnit Assert.*:
// Assert.True(result.IsSuccess);  ❌

// Mockar via NSubstitute (INTE FakeItEasy):
var repo = Substitute.For<IApplicationRepository>();
repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(entity);
await repo.Received(1).AddAsync(Arg.Any<Application>(), Arg.Any<CancellationToken>());
```

---

#### 1.3.6 `db-migration-writer`

| Fält | Värde |
|------|-------|
| **Fil** | `.claude/agents/db-migration-writer.md` |
| **Syfte** | Creates, reviews, and troubleshoots EF Core 10 migrations against PostgreSQL 18. |
| **LMS-referens** | `dojo-future-be/.claude/agents/migration-helper.md` (lätt anpassad — PG 18 istället för 17, inga SQL Server-reminiscenser) |
| **Modell** | `sonnet` |
| **Tools tillåtna** | `Read, Write, Edit, Glob, Bash(dotnet ef migrations add:*), Bash(dotnet ef migrations remove:*), Bash(dotnet ef migrations script:*), Bash(psql:*)` |
| **Tools blockerade** | `Bash(dotnet ef database drop:*), Bash(dotnet ef database update:* --connection <prod>), Bash(git push:*)` |
| **Auto-trigger** | Ingen |
| **Manuell trigger** | `/migrate <name>`, `.claude/skills/add-migration/` |
| **Input** | Förändring i entitet(er); migration-namn |
| **Output** | Migration-fil + översikt av genererad SQL på svenska ("Skapar tabell `applications`, index på `(job_seeker_id, status)`, FK till `job_ads` med Restrict-beteende.") |
| **Success-definition** | Migration kompilerar; SQL-preview reviewed; inga oavsiktliga data-loss-varningar utan Klas approval |

**PG 18-specifika notes (BUILD.md §3.2, §7.1):**

- `timestamp with time zone` för alla timestamps.
- `text` istället för `varchar(n)` där max-längd inte är en invariant.
- Soft delete: `deleted_at timestamptz NULL` + global query filter `e => e.DeletedAt == null`.
- Partial indexes med PG-syntax: `.HasFilter("\"deleted_at\" IS NULL")`.
- Svensk full-text: `using gin(to_tsvector('swedish', title || ' ' || description))` för `job_ads`.

---

#### 1.3.7 `ai-prompt-engineer`

| Fält | Värde |
|------|-------|
| **Fil** | `.claude/agents/ai-prompt-engineer.md` |
| **Syfte** | Creates, versions, and tests prompts stored in `/prompts/*.prompt.md` for JobbPilot's AI layer (Bedrock + BYOK). |
| **LMS-referens** | *Ingen — LMS har ingen AI-layer.* Ny för JobbPilot. |
| **Modell** | `sonnet` |
| **Tools tillåtna** | `Read, Write, Edit, Glob, Bash(dotnet test --filter Category=PromptEvals:*)` |
| **Tools blockerade** | `Bash(git push:*), Bash(rm:*)` — prompt-filer raderas aldrig, bara deprecateras |
| **Auto-trigger** | Ingen |
| **Manuell trigger** | `.claude/skills/add-prompt/` + `.claude/skills/version-prompt/` |
| **Input** | Use case (cv-tailor, cover-letter-generate, cliche-detect, etc.) + önskat schema |
| **Output** | `prompts/<id>.prompt.md` med frontmatter `id / version / tier / output / schema` (BUILD.md §8.5) + svensk beskrivning av vad prompten gör och hur den ska testas |
| **Success-definition** | Prompt-fil valid YAML-frontmatter; schema-referens finns i `JobbPilot.Application.AiAssist.Schemas/`; manuell eval mot 3 sample-inputs passerar |

**Kärnregler:**

- Alla prompts på **svenska** (BUILD.md §8.6).
- Systemprompt alltid inleder med "Du är en svensk karriärrådgivare...".
- Token-substitution: `{{field_name}}` (handlebars-liknande).
- Versions-bump: minor för pattern-ändring utan schema-diff, major för schema-diff. Gamla versioner behålls i git men markeras `deprecated: true` i frontmatter.
- Ingen prompt får raderas — bara deprecateras (BUILD.md §8.5 om versionering).

---

#### 1.3.8 `docs-keeper`

| Fält | Värde |
|------|-------|
| **Fil** | `.claude/agents/docs-keeper.md` |
| **Syfte** | Updates `docs/current-work.md`, `docs/sessions/*.md`, `docs/decisions/*.md`, and reference maps (`.claude/cqrs-complete-map.md` etc.) by scanning repo state. |
| **LMS-referens** | `dojo-future-be/.claude/agents/map-updater.md` + doc-uppdaterings-delen av `/pr`-commandot. Slås ihop. |
| **Modell** | `sonnet` |
| **Tools tillåtna** | `Read, Write, Edit, Glob, Grep, Bash(git log:*), Bash(git diff:*), Bash(git status:*)` |
| **Tools blockerade** | `Write(BUILD.md), Write(CLAUDE.md), Write(DESIGN.md)` — se PreToolUse-hook DEL 4, dessa filer är Klas ensamma beslut |
| **Auto-trigger** | PreCompact-hook (DEL 4); SessionEnd-hook (via `/session-end`) |
| **Manuell trigger** | `/session-end`, `.claude/skills/update-maps/`, `.claude/skills/update-docs/`, `/adr` |
| **Input** | Git log sedan senaste session + diff mot last-known state i `docs/current-work.md` |
| **Output** | Uppdaterade doc-filer + svensk sammanfattning "Uppdaterade: X, Y, Z. ADR-förslag för: Q (skapas efter godkännande)." |
| **Success-definition** | Alla maps i `.claude/` matchar faktisk kod; `docs/current-work.md` har rubriker `## Aktivt / ## Klart senaste session / ## Nästa`; inga maps redigerade manuellt (signaturer: maskin-genererad notering i toppen) |

---

## 2. Skills

Per Klas-beslut #6: samma skills som LMS (med svensk översättning av användar-output) + JobbPilot-specifika tillägg.

### 2.1 LMS-skills — mappning

| LMS-skill | Återanvänds? | JobbPilot-ändring | Motivering |
|-----------|--------------|-------------------|------------|
| `session-start` | ✅ Ja | Svensk dialog; läser `docs/current-work.md` (se DEL 10 om placering); kör `docker compose ps` som sanity-check (DEL 4 `SessionStart`-hook gör det tyngre jobbet) | Klas-beslut #3 (session-protokoll). |
| `session-end` | ✅ Ja | Svensk dialog; uppdaterar `docs/current-work.md` + `docs/sessions/YYYY-MM-DD-HHMM-<slug>.md`; delegerar till `docs-keeper`-agenten | Klas-beslut #3. |
| `test` | ✅ Ja | Argument: `--scope domain|application|integration|frontend|e2e|all` (default: domain+application); startar Docker test-profile om integration-scope | Hård gate (DEL 6). |
| `build` | ✅ Ja | `dotnet build` BE + `pnpm run build` FE; returnerar felsammanfattning svenska | Standard. |
| `coverage-report` | ✅ Ja | Cross-lager: coverlet för BE, Vitest coverage för FE; rapport på svenska med diff-coverage-fokus | Trash-ut total coverage, lyft diff coverage. |
| `lint` (FE) | ✅ Ja | `pnpm run lint` + `prettier --check` | Pre-commit-hook kör också. |
| `status` | ✅ Ja | Visar `docs/current-work.md` + `git status` + Docker-status på svenska | Snabb orientering. |
| `add-command` | 🔧 Modifiera | Syntax för Mediator.SourceGenerator (attributes istället för MediatR-interfaces); Swedish error messages i `Error.X("CODE", "...")` | Klas-beslut #4 (Mediator). Se DEL 1.3.3. |
| `add-query` | 🔧 Modifiera | Samma som add-command | Samma. |
| `add-entity` | 🔧 Modifiera | JobbPilot-paths (`JobbPilot.Domain/Entities/<Context>/`); strongly-typed IDs som record struct | BUILD.md §5.2. |
| `add-endpoint` | 🔧 Modifiera | Rutter-prefix `/api/v1/<resource>` (BUILD.md §6.1); ETag/If-Match där aggregate-updates; Problem Details error-format | BUILD.md §6.1. |
| `update-maps` | 🔧 Modifiera | Andra filer: `.claude/cqrs-complete-map.md`, `entities-complete-map.md`, `endpoints-complete-map.md`, `aggregates-complete-map.md` (JobbPilot-specifik) | Delegeras till `docs-keeper`. |
| `update-docs` | ✅ Ja | Identisk användningsfall | — |
| `review` | 🔧 Modifiera | Invocerar `code-reviewer`-agenten (opus); rapport sparas i `docs/reviews/` | Klas-beslut #1 + DEL 5. |
| `add-chart` | ❌ Skippas v1 | JobbPilot v1 har 1 chart-sida — för lite för egen skill | Återinförs v2. |
| `add-component` (FE) | 🔧 Modifiera | Shadcn + DESIGN.md-tokens; ingen emoji/utropstecken-check in-prompt | DESIGN.md-compliance. |
| `add-form` (FE) | 🔧 Modifiera | RHF + Zod + next-intl + svensk error-text | CLAUDE.md §4.3. |
| `add-hook` (FE) | 🔧 Modifiera | TanStack Query-mönster från `dojo-future-fe/.claude/agents/api-integrator.md` | FE data-fetching-standard. |
| `add-page` (FE) | 🔧 Modifiera | Svenska rutter (`/ansokningar`); server-component default; civic-layout-templates | BUILD.md §10.1. |
| `create-feature` (FE) | ❌ Skippas | För högnivå; ersatt av `/new-feature`-command (DEL 3) | `/new-feature` orkestrerar agenter istället. |
| `create-page` (FE) | ❌ Skippas | Duplicerar `add-page` | — |
| `lint-check` (FE) | ❌ Skippas | Duplicerar `lint` | — |

### 2.2 Nya JobbPilot-skills

Per Klas-beslut #6 (JobbPilot-specifika tillägg). Noterat: vissa är semantiskt "rules" (always-on kunskap); jag markerar var de placeras.

| Skill | Placering | Syfte |
|-------|-----------|-------|
| `jobbpilot-domain` | `.claude/rules/domain.md` (rule, alltid laddad via `paths: ["src/JobbPilot.Domain/**/*.cs"]`) | Hur aggregates/VO/events skrivs: private setters, record struct IDs, domain events via `RaiseDomainEvent`, state machines via SmartEnum. |
| `jobbpilot-clean-arch` | `.claude/rules/clean-arch.md` (rule, alltid laddad) | Lager-beroenden: Domain→∅, Application→Domain, Infrastructure→Application+Domain, Api/Worker→Application+Infrastructure. Verifieras av NetArchTest. **v1-notis:** `NetArchTest.Rules` är formellt abandoned sedan 2022 men funkar för JobbPilots skala; noteras som tekniskt skuld — överväg `TngTech.ArchUnitNET` vid v2-refactor. |
| `jobbpilot-testing` | `.claude/rules/testing.md` (rule) | xUnit + Shouldly + NSubstitute + Testcontainers; namngivning `<Class>_<Scenario>_<Expected>`; coverage-mål per lager (Domain ≥ 90%, Application ≥ 80%). |
| `jobbpilot-design-system` | `.claude/rules/design-system.md` (rule, `paths: ["web/jobbpilot-web/**/*.{ts,tsx,css}"]`) | DESIGN.md §2–8 komprimerat: tokens, typografi, radius-max-6px, ingen emoji, svensk copy, inga gradients. |
| `jobbpilot-ai-prompts` | `.claude/skills/add-prompt/` + `.claude/skills/version-prompt/` | Skapa/versionera prompts i `/prompts/*.prompt.md`. Se agent `ai-prompt-engineer`. |
| `jobbpilot-gdpr` | `.claude/rules/gdpr.md` (rule, alltid laddad) | Checklista när ny PII hanteras: soft-delete, retention-period, export-path, audit-log-entry. |
| `jobbpilot-commit` | `.claude/skills/commit/SKILL.md` | Conventional commits (type(scope): beskrivning) på svenska eller engelska (konsekvent per PR); PR-template på svenska. |
| `jobbpilot-mediator` | `.claude/rules/mediator-pattern.md` (rule, `paths: ["src/JobbPilot.Application/**/*.cs"]`) | Mediator.SourceGenerator-syntax (`[Handler]`, `IRequestHandler<,>` attribut-baserat); INGA MediatR-kvarlevor; pipeline behaviors registration via `Mediator(opts => opts.PipelineBehaviors...)`. |

### 2.3 Skill-struktur (per skill)

Alla skills använder 2026-standardformatet `.claude/skills/<name>/SKILL.md` (inte `.claude/commands/<name>.md` även om det fortfarande fungerar — skills har precedence, och är rätt framtid enligt [SESSION-1-FINDINGS.md §1.1](./SESSION-1-FINDINGS.md)).

**YAML-frontmatter (engelska):**

```yaml
---
name: add-command
description: Scaffold a new Mediator.SourceGenerator command with handler, validator, and endpoint.
when_to_use: When the user asks to create a new command, handler, or CQRS write operation.
argument-hint: "<feature> <Action> [entity]"
allowed-tools: [Read, Write, Edit, Glob, Grep, Bash(dotnet build:*)]
model: sonnet
---
```

**Body (engelska, instruktionstecken):**

```markdown
# Add Command Skill

Scaffold a new command following JobbPilot CQRS conventions.

## Steps

1. Read `.claude/patterns/command-full-template.md`.
2. Read `.claude/rules/mediator-pattern.md`.
3. ...

## Reminders
- Error messages in Swedish, user-facing.
- Error codes: `ENTITY_ACTION` uppercase snake (e.g., `APPLICATION_NOT_FOUND`).
- Delete commands don't need validators (CLAUDE.md §3.4).

## Output to user
After scaffolding, report to Klas in Swedish:
"Skapade <N> filer under <path>. Nästa steg: `/test --write <path>` för att generera tester, sen `/review` för granskning."
```

---

## 3. Slash commands

Per Klas minimum-lista. Samtliga implementeras som skills (med `disable-model-invocation: false` för att också kunna auto-triggras av Claude).

| Command | Skill-path | Syfte | Input | Output |
|---------|-----------|-------|-------|--------|
| `/review` | `.claude/skills/review/` | Invocera `code-reviewer`-agenten mot staged changes (default) eller specifik diff | `[--staged \| --since <ref>]` | Svensk rapport i `docs/reviews/YYYY-MM-DD-HH-MM-<slug>.md` + inline-summering |
| `/test` | `.claude/skills/test/` | Kör test-suite med scope-flag | `[--scope domain\|application\|integration\|frontend\|e2e\|all]` | Test-resultat på svenska; lyfter fel först |
| `/migrate` | `.claude/skills/migrate/` | Skapa/applicera EF Core-migration via `db-migration-writer`-agenten | `<migration-name> [--apply]` | Migration-fil + SQL-preview; `--apply` kör `dotnet ef database update` mot dev |
| `/new-feature` | `.claude/skills/new-feature/` | Orkestrerar `dotnet-architect` + `nextjs-ui-engineer` + `test-writer` för en full feature enligt `.claude/templates/planning/STATE.md` | `<feature-name>` | Plan-fil + följande steg på svenska ("Skapa Phase 1? J/n") |
| `/commit` | `.claude/skills/commit/` | Skapa conventional commit med AI-genererat meddelande baserat på diff | `[--amend]` | Commit-meddelande föreslås (svenska eller engelska efter `.claude/rules/commit-language.md`); Klas godkänner innan `git commit` |
| `/session-start` | `.claude/skills/session-start/` | Ladda kontext från förra sessionen | — | Svensk summary av: aktiv branch, senaste commit, `docs/current-work.md`-innehåll, docker-status |
| `/session-end` | `.claude/skills/session-end/` | Spara session-state via `docs-keeper`-agenten | `[--reason <text>]` | Uppdaterad `docs/current-work.md` + ny `docs/sessions/YYYY-MM-DD-HHMM-<slug>.md` |
| `/adr` | `.claude/skills/adr/` | Skapa Architecture Decision Record | `<title>` | Ny `docs/decisions/NNNN-<slug>.md` med ADR-template (svenska rubriker, engelska tekniska termer) |
| `/audit-versions` | `.claude/skills/audit-versions/` | Kör versions-scan mot NuGet + npm för att upptäcka new stable releases | — | Rapport på svenska: "Följande paket har nya versioner: X, Y. Säkerhetsadvisories: Z." |

### 3.1 Implicit auto-triggering

Inga av dessa är `disable-model-invocation: true`. Claude Code kan autonomt invocera dem när beskrivningen matchar (ny 2026-semantik). Exempel: om Klas skriver "kör testerna" utan att uttala `/test`, ska Claude ändå trigga `/test`-skillen.

Blockerande `disable-model-invocation: true` används bara för `/commit` och `/migrate --apply` — operationer som skriver till persistent state utanför repo.

---

## 4. Hooks (kritiska för auto-triggers)

Per Klas-krav: code-reviewer auto-invocerad; tester hard gate före push.

Alla hooks ligger i `.claude/settings.json` (committed) + `.husky/`-scripts där Husky är rätt verktyg (pre-commit, pre-push på git-sidan).

### 4.1 SessionStart-hook

| Fält | Värde |
|------|-------|
| **Event** | `SessionStart` |
| **Plats** | `.claude/settings.json` → `hooks.SessionStart[0]` |
| **Kommando** | `bash .claude/hooks/session-start.sh` |
| **Timeout** | 10000 ms |
| **Matcher** | `""` (alla sessions) |
| **Error handling** | Exit 0 alltid (hookar får inte blockera start); fel → stderr, Claude visar varning |

**Skript-innehåll (`.claude/hooks/session-start.sh`):**

```bash
#!/usr/bin/env bash
# Kör vid session-start. All output visas för Klas.
set -u  # inte -e: enstaka fel får inte stoppa hooken

echo "[JobbPilot] Session-start-check ${PWD##*/} @ $(date +%H:%M)"

# 1. Docker-status
if ! docker info >/dev/null 2>&1; then
    echo "⚠ Docker körs inte. Starta Docker Desktop för att kunna köra tester."
elif ! docker compose ps --format json 2>/dev/null | grep -q '"State":"running"'; then
    echo "ℹ Docker Compose-tjänster är nere. Kör 'docker compose up -d' för dev-miljön."
else
    echo "✓ Docker Compose-tjänster uppe."
fi

# 2. .env-fil (Klas-beslut #8)
if [ ! -f .env ]; then
    echo "⚠ .env saknas i repo-roten. Kopiera från .env.example om du inte redan gjort det."
else
    echo "✓ .env finns."
fi

# 3. Uncommitted changes från förra sessionen
if ! git diff --quiet 2>/dev/null || ! git diff --cached --quiet 2>/dev/null; then
    echo "⚠ Oparsade ändringar finns från förra sessionen — kolla 'git status' innan du börjar."
    git status --short | head -10
fi

# 4. current-work.md
if [ -f docs/current-work.md ]; then
    echo ""
    echo "== docs/current-work.md (senaste session) =="
    head -40 docs/current-work.md
else
    echo "ℹ docs/current-work.md finns inte än — skapas vid första /session-end."
fi

exit 0
```

### 4.2 PreToolUse-hook — blockera ändring av spec-filer

| Fält | Värde |
|------|-------|
| **Event** | `PreToolUse` |
| **Matcher** | `Edit\|Write` |
| **Kommando** | `bash .claude/hooks/guard-spec-files.sh` |
| **Timeout** | 5000 ms |
| **Beteende vid match** | Exit 2 = blockera; stderr → Claude för self-correction |

**Skript (`.claude/hooks/guard-spec-files.sh`):**

```bash
#!/usr/bin/env bash
set -u
TOOL_INPUT="${CLAUDE_TOOL_INPUT:-$(cat)}"
FILE=$(echo "$TOOL_INPUT" | jq -r '.file_path // empty' 2>/dev/null)

if [ -z "$FILE" ]; then exit 0; fi

# Guard spec files: kräv explicit Klas-approval i prompten
case "$FILE" in
    *BUILD.md|*CLAUDE.md|*DESIGN.md)
        # Kolla USER_PROMPT för explicit approval
        LAST_PROMPT="${CLAUDE_USER_PROMPT:-}"
        if ! echo "$LAST_PROMPT" | grep -qiE '(godkänt|approved|uppdatera.*(build|claude|design)\.md|fixa.*(build|claude|design)\.md)'; then
            echo "[guard] $FILE är skyddad. Klas måste explicit godkänna i prompten ('Uppdatera BUILD.md med X'). Annars använd /adr för att spåra ändringar." >&2
            exit 2
        fi
        ;;
esac

exit 0
```

### 4.3 PreToolUse-hook — blockera farliga Bash

| Fält | Värde |
|------|-------|
| **Event** | `PreToolUse` |
| **Matcher** | `Bash` |
| **Kommando** | `bash .claude/hooks/guard-bash.sh` |
| **Timeout** | 5000 ms |
| **Blockerade mönster** | `rm -rf /`, `rm -rf ~`, `curl .* \| (bash\|sh)`, `sudo\s`, `chmod 777`, `.git/hooks`, `>\s*/dev/sd`, `dd if=` |

**Skript (`.claude/hooks/guard-bash.sh`):**

```bash
#!/usr/bin/env bash
set -u
TOOL_INPUT="${CLAUDE_TOOL_INPUT:-$(cat)}"
CMD=$(echo "$TOOL_INPUT" | jq -r '.command // empty' 2>/dev/null)

if [ -z "$CMD" ]; then exit 0; fi

# Farliga mönster
if echo "$CMD" | grep -qE '(rm -rf (/|~|\$HOME)|curl[^|]*\|[[:space:]]*(bash|sh)|^sudo[[:space:]]|chmod 777|\.git/hooks|dd if=)'; then
    echo "[guard] Farligt bash-mönster blockerat: $CMD" >&2
    echo "Om du verkligen menar detta: kör det själv i terminalen utanför Claude Code." >&2
    exit 2
fi

exit 0
```

### 4.4 PostToolUse-hook — .cs-ändringar

| Fält | Värde |
|------|-------|
| **Event** | `PostToolUse` |
| **Matcher** | `Edit\|Write` |
| **Kommando** | `bash .claude/hooks/post-cs-edit.sh` |
| **Timeout** | 30000 ms |
| **Beteende** | Icke-blockerande; varningar via stdout |

**Skript:**

```bash
#!/usr/bin/env bash
set -u
TOOL_INPUT="${CLAUDE_TOOL_INPUT:-$(cat)}"
FILE=$(echo "$TOOL_INPUT" | jq -r '.file_path // empty' 2>/dev/null)

if [ -z "$FILE" ] || [[ "$FILE" != *.cs && "$FILE" != *.csproj ]]; then
    exit 0
fi

# dotnet format på berörd fil
dotnet format --include "$FILE" --verify-no-changes >/dev/null 2>&1
if [ $? -ne 0 ]; then
    dotnet format --include "$FILE" >/dev/null 2>&1
    echo "ℹ Auto-formaterade $FILE (dotnet format)."
fi

# Verifiera att test-file finns för nya handlers/aggregates
if echo "$FILE" | grep -qE '(Handler|Aggregate|\.cs)$'; then
    REL=$(realpath --relative-to="$(pwd)" "$FILE" 2>/dev/null || echo "$FILE")
    # Derive expected test path (quick heuristic)
    TEST_CANDIDATE=$(echo "$REL" | sed 's|^src/|tests/|; s|\.cs$|Tests.cs|')
    if [ ! -f "$TEST_CANDIDATE" ]; then
        echo "ℹ Tips: testfil saknas för $REL. Kör /test --write $REL när implementationen är stabil."
    fi
fi

exit 0
```

### 4.5 PostToolUse-hook — .ts/.tsx-ändringar

| Fält | Värde |
|------|-------|
| **Event** | `PostToolUse` |
| **Matcher** | `Edit\|Write` |
| **Kommando** | `bash .claude/hooks/post-ts-edit.sh` |
| **Timeout** | 30000 ms |
| **Beteende** | Icke-blockerande |

**Skript:**

```bash
#!/usr/bin/env bash
set -u
TOOL_INPUT="${CLAUDE_TOOL_INPUT:-$(cat)}"
FILE=$(echo "$TOOL_INPUT" | jq -r '.file_path // empty' 2>/dev/null)

if [ -z "$FILE" ] || [[ "$FILE" != *.ts && "$FILE" != *.tsx ]]; then
    exit 0
fi

cd web/jobbpilot-web || exit 0

# eslint --fix på berörd fil
npx eslint --fix "$FILE" >/dev/null 2>&1 || true
npx prettier --write "$FILE" >/dev/null 2>&1 || true

# Typecheck hela FE (snabb när vi använder --incremental)
if ! npx tsc --noEmit --incremental >/dev/null 2>&1; then
    echo "⚠ TypeScript-fel efter ändring i $FILE. Kör 'pnpm tsc --noEmit' för detaljer."
fi

exit 0
```

### 4.6 PostToolUse-hook — code-reviewer auto-trigger

**Detta är Klas högsta-prio-hook.**

| Fält | Värde |
|------|-------|
| **Event** | `PostToolUse` |
| **Matcher** | `TodoWrite` |
| **Kommando** | `bash .claude/hooks/post-todo-review.sh` |
| **Timeout** | 5000 ms (skriptet är snabbt; själva review-kallelsen är en Claude-sida) |
| **Beteende** | Exit 0 + JSON `additionalContext` → Claude ser hintet och invocerar `code-reviewer`-agenten |

**Skript:**

```bash
#!/usr/bin/env bash
set -u
TOOL_INPUT="${CLAUDE_TOOL_INPUT:-$(cat)}"

# Extrahera todos som precis markerades completed
COMPLETED=$(echo "$TOOL_INPUT" | jq -r '.todos[]? | select(.status == "completed") | .content' 2>/dev/null || true)

if [ -z "$COMPLETED" ]; then exit 0; fi

# Kod-relaterade nyckelord (engelska + svenska)
CODE_KEYWORDS='implement|add|fix|refactor|create|update|bygg|lägg till|skriv|fixa|uppdatera|skapa'

if ! echo "$COMPLETED" | grep -qiE "$CODE_KEYWORDS"; then exit 0; fi

# Finns det osparade kod-ändringar?
CHANGED=$(git diff --name-only HEAD 2>/dev/null || true)
CHANGED_CACHED=$(git diff --cached --name-only 2>/dev/null || true)
ALL_CHANGED=$(printf '%s\n%s\n' "$CHANGED" "$CHANGED_CACHED" | sort -u | grep -vE '^$')

if [ -z "$ALL_CHANGED" ]; then exit 0; fi

# Endast kod-filer
if ! echo "$ALL_CHANGED" | grep -qE '\.(cs|ts|tsx|razor|cshtml)$'; then exit 0; fi

# Trigga code-reviewer via additionalContext
cat <<'EOF'
{
  "hookSpecificOutput": {
    "hookEventName": "PostToolUse",
    "additionalContext": "En kod-relaterad uppgift markerades just som slutförd och arbetsträdet har osparade ändringar i .cs/.ts/.tsx-filer. Invocera code-reviewer-agenten mot aktuell diff INNAN du fortsätter till nästa task eller markerar något annat som klart. Kommando: Agent med subagent_type='code-reviewer'."
  }
}
EOF

exit 0
```

**Varför additionalContext och inte exit 2:** vi vill inte blockera Claude, bara injicera en instruktion som huvud-Claude måste agera på. Om code-reviewer-kallelsen misslyckas separat hanteras det av Husky pre-commit som är en hård gate (se 4.8).

### 4.7 PreCompact-hook

| Fält | Värde |
|------|-------|
| **Event** | `PreCompact` |
| **Kommando** | `bash .claude/hooks/pre-compact-save.sh` |
| **Timeout** | 15000 ms |
| **Beteende** | Icke-blockerande |

**Skript:**

```bash
#!/usr/bin/env bash
set -u
TIMESTAMP=$(date +%Y-%m-%d-%H%M)
SLUG="precompact-${TIMESTAMP}"
mkdir -p docs/sessions

cat > "docs/sessions/${SLUG}.md" <<EOF
---
type: precompact-snapshot
created: $(date -Iseconds)
reason: Automatic session-state save before context compaction
---

# Session snapshot (pre-compact)

## Git state

\`\`\`
$(git status --short | head -30)
\`\`\`

## Senaste 10 commits

\`\`\`
$(git log --oneline -10)
\`\`\`

## current-work.md (copy)

\`\`\`
$(cat docs/current-work.md 2>/dev/null || echo "(saknas)")
\`\`\`

## Aktiva tasks (Claude Code TodoWrite)

(Fylls i av docs-keeper om triggat)
EOF

echo "✓ Session-snapshot sparad: docs/sessions/${SLUG}.md"
exit 0
```

### 4.8 Git pre-commit (Husky, `.husky/pre-commit`)

**Inte en Claude Code-hook** — en traditionell Husky-hook som gäller oavsett om Claude Code eller Klas committar manuellt.

```bash
#!/usr/bin/env sh
. "$(dirname -- "$0")/_/husky.sh"

set -e

echo "[pre-commit] Kör gates..."

# 1. dotnet format — verifiera inget oformatterat
dotnet format --verify-no-changes || {
    echo "✗ dotnet format fel. Kör 'dotnet format' och stega in igen." >&2
    exit 1
}

# 2. dotnet test — snabba lager (Domain + Application unit)
dotnet test tests/JobbPilot.Domain.UnitTests --no-build --verbosity minimal || exit 1
dotnet test tests/JobbPilot.Application.UnitTests --no-build --verbosity minimal || exit 1

# 3. Architecture tests (NetArchTest)
dotnet test tests/JobbPilot.Architecture.Tests --no-build --verbosity minimal || exit 1

# 4. Frontend typecheck + unit tests (om FE-filer i diff)
if git diff --cached --name-only | grep -qE '^web/jobbpilot-web/'; then
    (cd web/jobbpilot-web && pnpm tsc --noEmit) || exit 1
    (cd web/jobbpilot-web && pnpm test --run) || exit 1
fi

# 5. Code-reviewer via Claude Code CLI (om installerad)
if command -v claude >/dev/null 2>&1; then
    echo "[pre-commit] Kör code-reviewer..."
    REVIEW_OUTPUT=$(claude -p "Invocera code-reviewer-agenten mot 'git diff --cached'. Skriv rapporten till docs/reviews/pre-commit-$(date +%Y%m%d-%H%M%S).md. Om kritiska fynd, returnera exit 1." --dangerously-skip-permissions=false 2>&1) || {
        echo "✗ Code-reviewer hittade kritiska fynd. Se docs/reviews/ för rapport." >&2
        exit 1
    }
fi

echo "✓ Alla pre-commit-gates passerade."
exit 0
```

### 4.9 Git pre-push (Husky, `.husky/pre-push`)

```bash
#!/usr/bin/env sh
. "$(dirname -- "$0")/_/husky.sh"

set -e

echo "[pre-push] Kör full suite..."

# 1. Full test-suite inkl. integration
docker compose --profile test up -d --wait
trap "docker compose --profile test stop" EXIT

dotnet test --verbosity minimal || exit 1

# 2. Frontend full test
(cd web/jobbpilot-web && pnpm test --run) || exit 1

# 3. Architecture tests (redan i pre-commit men kör igen för säkerhet)
dotnet test tests/JobbPilot.Architecture.Tests --verbosity minimal || exit 1

# 4. Secrets scan
if command -v gitleaks >/dev/null 2>&1; then
    gitleaks detect --no-banner --redact || {
        echo "✗ Gitleaks hittade potentiella secrets." >&2
        exit 1
    }
fi

echo "✓ Alla pre-push-gates passerade."
exit 0
```

### 4.10 Hook-tabell (sammanfattning)

| Event | Fil | Blockerar? | Huvudsyfte |
|-------|-----|------------|------------|
| `SessionStart` | `.claude/hooks/session-start.sh` | Nej | Docker + .env + uncommitted + current-work-summary |
| `PreToolUse(Edit/Write)` | `.claude/hooks/guard-spec-files.sh` | Ja (spec-filer) | Blockera edit av BUILD/CLAUDE/DESIGN utan Klas approval |
| `PreToolUse(Bash)` | `.claude/hooks/guard-bash.sh` | Ja (farliga mönster) | rm -rf, curl\|sh, sudo etc. |
| `PostToolUse(Edit/Write)` — .cs | `.claude/hooks/post-cs-edit.sh` | Nej | dotnet format + test-file-saknas-varning |
| `PostToolUse(Edit/Write)` — .ts/.tsx | `.claude/hooks/post-ts-edit.sh` | Nej | eslint + prettier + tsc |
| `PostToolUse(TodoWrite)` | `.claude/hooks/post-todo-review.sh` | Nej (additionalContext) | Auto-trigga code-reviewer efter slutförd kod-task |
| `PreCompact` | `.claude/hooks/pre-compact-save.sh` | Nej | Spara session-snapshot |
| `pre-commit` (Husky) | `.husky/pre-commit` | Ja | Format + snabba tester + architecture + code-reviewer |
| `pre-push` (Husky) | `.husky/pre-push` | Ja | Full suite + integration + gitleaks |

---

## 5. Code-reviewer workflow (fokus)

Per Klas högsta prioritet.

### 5.1 Trigger-events (sammanfattning)

| Trigger | Hook/Mekanism | Blockerar? |
|---------|---------------|------------|
| Efter TodoWrite där kod-task blir completed + diff ej tom | PostToolUse-hook (4.6) | Nej (injicerar instruktion) |
| Git pre-commit | Husky (4.8) | Ja om kritiska fynd |
| Git pre-push | Husky (4.9) — inte code-reviewer, men `security-auditor` via gitleaks | Ja |
| Manuell | `/review`-command | — |
| PR skapas / uppdateras | GitHub Action `code-review.yml` (DEL 11) | Ja via required check |

### 5.2 Agent-prompt — checklist

Den fullständiga systempromten lagras i `.claude/agents/code-reviewer.md`. Skelett (engelska):

```markdown
You are the JobbPilot code reviewer.

## Before you start

Read these files:
- `BUILD.md` §5 (anti-patterns) and any sections touched by the diff.
- `CLAUDE.md` §2–5 (conventions).
- `DESIGN.md` §2–8 (only if frontend files are in diff).
- `.claude/rules/clean-arch.md`
- `.claude/rules/anti-patterns.md`
- `.claude/rules/gdpr.md`
- `.claude/rules/mediator-pattern.md`
- `.claude/rules/design-system.md` (only if frontend)

## Checklist

For each changed file, verify:

### 1. Clean Architecture boundaries (CLAUDE.md §2.1)
- [ ] `JobbPilot.Domain` imports nothing outside `System.*` or `JobbPilot.Domain.*`
- [ ] `JobbPilot.Application` imports only `JobbPilot.Domain` + framework-agnostic interfaces
- [ ] `JobbPilot.Infrastructure` implements `JobbPilot.Application` interfaces
- [ ] No cross-layer entity leaks through API

### 2. Anti-patterns (CLAUDE.md §5.1, §5.2, §5.3, §5.4)
- [ ] No Repository pattern on top of EF Core — use `IAppDbContext` directly
- [ ] No AutoMapper across Domain boundary
- [ ] No `DateTime.Now`/`DateTime.UtcNow` — use `IDateTimeProvider`
- [ ] No magic strings
- [ ] No "Service"-suffix generic names
- [ ] No primitive obsession (string as email, decimal as money)
- [ ] No static helpers holding state
- [ ] No `dynamic` in C#
- [ ] No catch-all `try/catch` without action
- [ ] No sensitive data in logs (CV content, BYOK keys, OAuth tokens)
- [ ] No hardcoded configuration — use `IOptions<T>`
- [ ] No synchronous I/O in request pipeline
- [ ] No unpaginated list fetches
- [ ] No `SELECT *` — project to DTOs

### 3. Design system (only if frontend files)
- [ ] No emojis in UI strings
- [ ] No exclamation marks in UI copy
- [ ] `border-radius` ≤ 6px except pills
- [ ] No gradients, no box-shadows beyond `shadow-sm`/`shadow-md`
- [ ] Colors from tokens in `globals.css`, no hardcoded hex
- [ ] Swedish copy, `du` (lowercase), quantified information first

### 4. Diff coverage (test hard gate)
- [ ] Every new domain class has ≥ 1 test for invariant
- [ ] Every new handler has ≥ 1 happy-path test and ≥ 1 validation-failure test
- [ ] New endpoints have integration test

### 5. Secrets scan (`git grep`)
- [ ] No `BEGIN PRIVATE KEY`
- [ ] No `sk-ant-*`, `AKIA*`, `AIza*`, `ghp_*`, `ghs_*`
- [ ] No `Bearer eyJ*` in strings
- [ ] No passwords or connection strings in `appsettings.json` (should be in Secrets Manager or `.env`)

### 6. GDPR (any new PII?)
- [ ] New PII field → soft-delete via `deleted_at`
- [ ] Retention policy documented in entity comment
- [ ] Export path covered by `/me/export`
- [ ] Audit-log entry for mutations

### 7. Mediator.SourceGenerator pattern (not MediatR remnants)
- [ ] `IRequest<T>` / `IRequestHandler<,>` used correctly
- [ ] Pipeline behaviors registered via `Mediator(opts => ...)`
- [ ] No `using MediatR;` imports

### 8. Commit message (if staged)
- [ ] Conventional Commits format: `<type>(<scope>): <description>`
- [ ] Scope valid: applications, resumes, ai, infra, web, etc.

## Output format (Swedish to Klas)

```markdown
# Code review — <YYYY-MM-DD HH:MM>

**Scope:** `<filer granskade>`
**Commit:** `<hash eller "ostaged">`

## Kritiska fynd (blockerar commit/push)

### 1. `<file>:<line>` — <rubrik>
**Fel:** <beskrivning på svenska>
**Regel:** CLAUDE.md §<X.Y> (<engelsk-rubrik>)
**Fix:** <förslag>

## Viktiga fynd (bör fixas innan merge)

(samma format)

## Nice-to-have

(samma format)

## Sammanfattning
- Kritiska: <N>
- Viktiga: <N>
- Nice-to-have: <N>

Status: <GODKÄND | BLOCKERAR>
```

## When to block

Exit 1 (blocking) if there is ≥ 1 Kritisk fynd. Otherwise exit 0.
```

### 5.3 Vid kritiska findings — auto-fix-försök

Claude Code har nu bättre agent-orchestration (SESSION-1-FINDINGS §1.2 — Opus 4.7). När `code-reviewer` returnerar kritiska fynd och trigger är pre-commit:

1. Claude läser rapporten.
2. För varje kritiskt fynd: försöker en auto-fix-iteration (max 3 iterationer totalt i samma session).
3. Efter varje försök: omkörning av `code-reviewer` mot ny diff.
4. Om 3 iterationer → fortfarande kritiska: eskalera till Klas med tydlig rapport "Dessa fynd kunde jag inte auto-fixa: X, Y, Z. Vänligen granska manuellt."

Detta loop:as i huvud-Claude-sessionen, inte i hooken. Hooken injicerar bara instruktionen.

### 5.4 Loggning

Alla review-körningar sparas i `docs/reviews/`. Namnmönster:

- Pre-commit: `pre-commit-YYYY-MM-DD-HHMMSS.md`
- Pre-push: `pre-push-YYYY-MM-DD-HHMMSS.md`
- Manuell `/review`: `manual-YYYY-MM-DD-HHMMSS.md`
- Auto-trigger efter TodoWrite: `auto-YYYY-MM-DD-HHMMSS-<task-slug>.md`
- GitHub Action: `pr-<N>-<commit-short>.md`

PR-beskrivningen (via GitHub Action) länkar till rapporten:
```
## Code review
[Se fullständig rapport](docs/reviews/pr-42-abc1234.md)
```

---

## 6. Test-strategi (hard gate)

### 6.1 Test-typer

| Typ | Ramverk | Location | Körs i |
|-----|---------|----------|--------|
| Domain unit | xUnit + Shouldly | `tests/JobbPilot.Domain.UnitTests/` | pre-commit, CI |
| Application unit | xUnit + Shouldly + NSubstitute | `tests/JobbPilot.Application.UnitTests/` | pre-commit, CI |
| Integration | xUnit + Testcontainers + Mvc.Testing + Shouldly | `tests/JobbPilot.Api.IntegrationTests/` | pre-push, CI |
| Architecture | xUnit + NetArchTest | `tests/JobbPilot.Architecture.Tests/` | pre-commit, CI |
| Frontend unit | Vitest + RTL + jsdom | `web/jobbpilot-web/src/__tests__/` | pre-commit (om FE-diff), CI |
| Frontend BDD | vitest-cucumber | `web/jobbpilot-web/src/__tests__/features/` | CI |
| E2E | Playwright | `web/jobbpilot-web/e2e/` | CI nightly mot staging |

### 6.2 Scope-mappning

| Command | Scope flag | Vad körs |
|---------|-----------|----------|
| `/test` (default) | — | Domain + Application unit + Architecture + FE unit |
| `/test --scope domain` | domain | Domain unit only |
| `/test --scope application` | application | Application unit only |
| `/test --scope integration` | integration | Integration (startar docker --profile test) |
| `/test --scope frontend` | frontend | FE unit |
| `/test --scope e2e` | e2e | Playwright (kräver dev + backend uppe) |
| `/test --scope all` | all | Allt inkl. Playwright |

### 6.3 Docker-beroenden

Per Klas-beslut #11 använder vi compose profiles. Testkörningar startar automatiskt rätt profile via `docker compose --profile test up -d --wait` innan första integration-test kör.

Testcontainers-usage: för integration-tester som behöver en tom db per test-klass, spinner Testcontainers-biblioteket upp ephemeral PG-containers ovanpå `postgres-test`-profilen — profilen används för `SqlServerTestFixture`-liknande shared state, inte för per-test-isolation.

### 6.4 Shouldly-exempel (för `.claude/rules/testing.md`)

```csharp
// Shouldly — ersätter FluentAssertions (licens-hinder 2025)
result.IsSuccess.ShouldBeTrue();
result.Value.ShouldNotBeNull();
result.Value.Email.Value.ShouldBe("klas@example.com");
result.Error.Code.ShouldBe("APPLICATION_NOT_FOUND");
result.Error.Type.ShouldBe(ErrorType.NotFound);

action.ShouldThrow<DomainException>()
    .Message.ShouldContain("Cannot log follow-up on a Rejected application");

collection.ShouldContain(x => x.Id == expectedId);
collection.Count.ShouldBe(3);
```

Mockar via **NSubstitute** (CLAUDE.md §17, inte FakeItEasy som LMS):

```csharp
var repo = Substitute.For<IApplicationRepository>();
repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(entity);

var handler = new SubmitApplicationHandler(repo, ...);
var result = await handler.Handle(command, CancellationToken.None);

await repo.Received(1).AddAsync(Arg.Any<Application>(), Arg.Any<CancellationToken>());
```

### 6.5 Diff-coverage över total

Per LMS-mönster men JobbPilot-anpassat: total coverage är mindre intressant än **diff-coverage** (täckning på nyligen tillagda rader). Verktyg: `coverlet` med `--filter "+[JobbPilot.Domain]*"` + jämförelse mot base-branch.

GitHub Action `ci.yml` genererar rapport: "Ny Domain-kod: 87% coverage (krav 80%). Ny Application-kod: 72% (krav 75% — **FAIL**)."

---

## 7. Settings

### 7.1 `.claude/settings.json` (committed)

```jsonc
{
  "$schema": "https://json.schemastore.org/claude-code-settings.json",
  "model": "claude-sonnet-4-6",
  "effortLevel": "high",
  "autoUpdatesChannel": "stable",
  "defaultShell": "bash",
  "respectGitignore": true,
  "autoMemoryDirectory": ".claude/auto-memory",
  "permissions": {
    "defaultMode": "default",
    "allow": [
      "Read",
      "Write",
      "Edit",
      "Glob",
      "Grep",
      "TodoWrite",
      "WebSearch",
      "WebFetch(domain:docs.claude.com)",
      "WebFetch(domain:code.claude.com)",
      "WebFetch(domain:platform.claude.com)",
      "WebFetch(domain:nuget.org)",
      "WebFetch(domain:www.npmjs.com)",
      "WebFetch(domain:docs.aws.amazon.com)",
      "WebFetch(domain:jobsearch.api.jobtechdev.se)",
      "WebFetch(domain:taxonomy.api.jobtechdev.se)",
      "WebFetch(domain:api.scb.se)",
      "Bash(dotnet build:*)",
      "Bash(dotnet test:*)",
      "Bash(dotnet run:*)",
      "Bash(dotnet restore:*)",
      "Bash(dotnet format:*)",
      "Bash(dotnet ef migrations:*)",
      "Bash(dotnet ef database update:*)",
      "Bash(dotnet new:*)",
      "Bash(dotnet sln:*)",
      "Bash(dotnet add:*)",
      "Bash(dotnet list package:*)",
      "Bash(pnpm:*)",
      "Bash(pnpm run:*)",
      "Bash(pnpm test:*)",
      "Bash(pnpm add:*)",
      "Bash(pnpm install:*)",
      "Bash(npx:*)",
      "Bash(corepack:*)",
      "Bash(git status:*)",
      "Bash(git diff:*)",
      "Bash(git log:*)",
      "Bash(git branch:*)",
      "Bash(git checkout:*)",
      "Bash(git add:*)",
      "Bash(git commit:*)",
      "Bash(git pull:*)",
      "Bash(git stash:*)",
      "Bash(git fetch:*)",
      "Bash(docker:*)",
      "Bash(docker compose:*)",
      "Bash(ls:*)",
      "Bash(tree:*)",
      "Bash(jq:*)",
      "Bash(gh:*)",
      "Bash(aws:*)",
      "Bash(terraform:*)",
      "Bash(psql:*)",
      "Bash(gitleaks detect:*)",
      "Agent(code-reviewer)",
      "Agent(security-auditor)",
      "Agent(dotnet-architect)",
      "Agent(nextjs-ui-engineer)",
      "Agent(test-writer)",
      "Agent(db-migration-writer)",
      "Agent(ai-prompt-engineer)",
      "Agent(docs-keeper)",
      "Agent(Explore)",
      "Agent(Plan)",
      "Agent(general-purpose)",
      "Skill(*)"
    ],
    "deny": [
      "Bash(rm -rf /*)",
      "Bash(rm -rf ~*)",
      "Bash(rm -rf $HOME*)",
      "Bash(sudo *)",
      "Bash(chmod 777*)",
      "Bash(curl * | bash*)",
      "Bash(curl * | sh*)",
      "Bash(wget * | bash*)",
      "Bash(dd if=*)",
      "Bash(* > /dev/sd*)",
      "Bash(git push --force *)",
      "Bash(git push -f *)",
      "Bash(git reset --hard origin/*)",
      "Bash(dotnet ef database drop*)",
      "Bash(terraform destroy*)",
      "Bash(aws * delete-*)",
      "Write(BUILD.md)",
      "Write(CLAUDE.md)",
      "Write(DESIGN.md)",
      "Edit(BUILD.md)",
      "Edit(CLAUDE.md)",
      "Edit(DESIGN.md)"
    ],
    "ask": [
      "Bash(git push:*)",
      "Bash(aws *)",
      "Bash(terraform apply*)",
      "Bash(dotnet ef database update*)"
    ]
  },
  "hooks": {
    "SessionStart": [{
      "matcher": "",
      "hooks": [{ "type": "command", "command": "bash .claude/hooks/session-start.sh", "timeout": 10000 }]
    }],
    "PreToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [{ "type": "command", "command": "bash .claude/hooks/guard-spec-files.sh", "timeout": 5000 }]
      },
      {
        "matcher": "Bash",
        "hooks": [{ "type": "command", "command": "bash .claude/hooks/guard-bash.sh", "timeout": 5000 }]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "Edit|Write",
        "hooks": [
          { "type": "command", "command": "bash .claude/hooks/post-cs-edit.sh", "timeout": 30000 },
          { "type": "command", "command": "bash .claude/hooks/post-ts-edit.sh", "timeout": 30000 }
        ]
      },
      {
        "matcher": "TodoWrite",
        "hooks": [{ "type": "command", "command": "bash .claude/hooks/post-todo-review.sh", "timeout": 5000 }]
      }
    ],
    "PreCompact": [{
      "matcher": "",
      "hooks": [{ "type": "command", "command": "bash .claude/hooks/pre-compact-save.sh", "timeout": 15000 }]
    }]
  }
}
```

**Anm. om `model`-fältet:** Session-1-subagenten verifierade att Opus 4.7 är släppt men Sonnet är rätt default per Klas-beslut #2. Modell-IDn att använda:
- `claude-sonnet-4-6` (session default, alla agenter utom opus-agenter)
- `claude-opus-4-7` (code-reviewer, security-auditor)

**Osäkerhet:** jag har inte dubbel-verifierat `claude-sonnet-4-6` som exakt accepterat modell-ID av Claude Code-klienten (jfr. `claude-sonnet-4-5-20250929` snapshot-alias). Verifiering krävs i session 3 — kör `claude /config` eller inspektera `claude --version`-accepterade värden. Alternativ: använd alias-strängarna `sonnet` och `opus` i agent-frontmatter, som alltid pekar till senaste stable variant.

### 7.2 `.claude/settings.local.json` (gitignored)

Mall för Klas att kopiera till egen fil:

```jsonc
{
  "permissions": {
    "allow": [
      "Bash(ssh klas@staging-bastion:*)",
      "Bash(docker exec:*)",
      "Read(//c/Users/zebac/.claude/**)"
    ]
  },
  "env": {
    "AWS_PROFILE": "jobbpilot-dev",
    "POSTHOG_API_KEY": "phc_local_noop"
  }
}
```

---

## 8. CLI-verktyg (Windows + WSL)

Session 3 steg 4 installerar och verifierar. Runbook: `docs/runbooks/local-dev-setup.md`.

| Verktyg | Install (Windows native) | Install (WSL/Linux) | Verifiering | Krav |
|---------|--------------------------|---------------------|-------------|------|
| GitHub CLI | `winget install GitHub.cli` | `sudo apt install gh` | `gh auth status` visar inloggad | v2.x |
| Docker + Compose | Docker Desktop-installer | `sudo apt install docker.io docker-compose-plugin` | `docker version` + `docker compose version` | Compose v2.x |
| .NET 10 SDK | `winget install Microsoft.DotNet.SDK.10` | `curl https://dot.net/v1/dotnet-install.sh -o- \| bash -s -- --channel 10.0` | `dotnet --version` ≥ 10.0.0 | LTS |
| pnpm | `corepack enable && corepack prepare pnpm@latest --activate` | same | `pnpm --version` ≥ 10.0 | Via corepack |
| Node.js | `winget install OpenJS.NodeJS.LTS` | `nvm install --lts` | `node --version` ≥ 22 LTS | för pnpm + Playwright |
| AWS CLI v2 | `winget install Amazon.AWSCLI` | MSI-installer | `aws --version` ≥ 2.x + `aws configure sso` efter Klas kört runbook | Klas-beslut #9 |
| Terraform | `winget install HashiCorp.Terraform` | `apt install terraform` | `terraform -version` ≥ 1.14 | Uppdatering från BUILD.md 1.9 |
| psql | Inkluderat i Docker Desktop; eller `winget install PostgreSQL.PostgreSQL.17` | `apt install postgresql-client-18` | `psql --version` | För manuell db-debug |
| jq | `winget install jqlang.jq` | `apt install jq` | `jq --version` | Hooks-scripts |
| gitleaks | `winget install gitleaks.gitleaks` alt. `scoop install gitleaks` | `brew install gitleaks` / download-release | `gitleaks version` | Pre-push scan |
| husky | `pnpm add -D husky` (per repo, inte global) | same | `pnpm husky` | Git hooks |
| Claude Code CLI | Per [code.claude.com install](https://code.claude.com/install) | same | `claude --version` | > 2.1.63 för Agent-alias |

---

## 9. Docker för lokal dev + test

Per Klas-beslut #11. Fil: `docker-compose.yml` i repo-roten.

```yaml
# OBS: Compose Specification v5 — INGEN top-level 'version:' nyckel.

name: jobbpilot

services:
  postgres-dev:
    image: postgres:18.3
    profiles: []                          # default = alltid upp
    environment:
      POSTGRES_DB: jobbpilot_dev
      POSTGRES_USER: jobbpilot
      POSTGRES_PASSWORD: dev
    ports:
      - "5432:5432"
    volumes:
      - postgres-dev-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U jobbpilot -d jobbpilot_dev"]
      interval: 5s
      timeout: 3s
      retries: 10

  redis-dev:
    image: redis:8.6-alpine
    profiles: []
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 10

  seq:
    image: datalust/seq:latest
    profiles: []
    environment:
      ACCEPT_EULA: "Y"
    ports:
      - "5341:80"
    volumes:
      - seq-data:/data

  postgres-test:
    image: postgres:18.3
    profiles: ["test", "full"]
    environment:
      POSTGRES_DB: jobbpilot_test
      POSTGRES_USER: jobbpilot
      POSTGRES_PASSWORD: test
    ports:
      - "5433:5432"
    tmpfs:
      - /var/lib/postgresql/data           # ephemeral för snabbhet
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U jobbpilot -d jobbpilot_test"]
      interval: 3s
      timeout: 2s
      retries: 15

  redis-test:
    image: redis:8.6-alpine
    profiles: ["test", "full"]
    ports:
      - "6380:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 3s
      timeout: 2s
      retries: 15

  # Frontend-container (valfri) för full E2E-debugging
  web-dev:
    profiles: ["full"]
    build:
      context: ./web/jobbpilot-web
      target: dev
    ports:
      - "3000:3000"
    depends_on:
      postgres-dev:
        condition: service_healthy

volumes:
  postgres-dev-data:
  seq-data:
```

**Kommandon:**

```bash
# Dagligt dev
docker compose up -d

# Kör integration-tester
docker compose --profile test up -d --wait
dotnet test tests/JobbPilot.Api.IntegrationTests
docker compose --profile test stop

# Full E2E
docker compose --profile full up -d
pnpm --filter jobbpilot-web run test:e2e

# Stäng ner allt
docker compose --profile full down
```

---

## 10. Docs-struktur

Per Klas-beslut #3 kring session-protokoll. **Blockerande fråga (DEL 16 fråga 1):** placering av `current-work.md`. Min rekommendation: `.claude/current-work.md` (som LMS), med en `docs/current-work.md`-symlänk eller kopia om Klas föredrar att se den i `docs/`.

**Föreslagen struktur** (förutsätter .claude/-placering):

```
docs/
├── current-work.md                    (symlänk till .claude/current-work.md ELLER separat fil enligt Klas-val)
├── sessions/
│   ├── 2026-04-18-0931-session-1-research.md
│   ├── 2026-04-18-1030-session-2-plan.md
│   └── YYYY-MM-DD-HHMM-<slug>.md
├── decisions/                         (ADRs, Klas-beslut #1 underförstått)
│   ├── 0001-clean-architecture.md
│   ├── 0002-mediator-source-generator-over-mediatr.md
│   ├── 0003-shouldly-over-fluentassertions.md
│   ├── 0004-github-flow-over-gitflow.md
│   ├── 0005-dotnet-10-lts.md
│   ├── 0006-postgresql-18-with-swedish-fts.md
│   ├── 0007-bedrock-eu-via-inference-profiles.md
│   └── NNNN-<slug>.md
├── reviews/
│   ├── pre-commit-YYYY-MM-DD-HHMMSS.md
│   ├── pre-push-YYYY-MM-DD-HHMMSS.md
│   ├── manual-YYYY-MM-DD-HHMMSS.md
│   ├── auto-YYYY-MM-DD-HHMMSS-<slug>.md
│   ├── pr-<N>-<commit-short>.md
│   └── security-YYYY-MM-DD-<slug>.md
├── test-reports/                      (coverlet-output, diff-coverage-summaries)
├── research/
│   ├── SESSION-1-FINDINGS.md          (finns)
│   ├── SESSION-1-VERSION-AUDIT.md     (finns)
│   ├── SESSION-2-PLAN.md              (denna fil)
│   └── bedrock-inference-profiles.md  (skapas session 3 steg 3)
├── runbooks/
│   ├── aws-setup.md                   (skapas session 2 som del av DEL 12)
│   ├── github-setup.md                (skapas session 2)
│   └── local-dev-setup.md             (skapas session 2)
├── api/                               (OpenAPI-export; inte i v1 session 2, senare)
└── prompts/                           (JobbPilots AI-prompts — "egen" AI, inte Claude Code)
    ├── cv-parse.prompt.md
    ├── cv-tailor.prompt.md
    ├── cover-letter-generate.prompt.md
    ├── match-deep.prompt.md
    ├── research-brief.prompt.md
    └── cliche-detect.prompt.md
```

Notera: `prompts/` ligger under `docs/` eller repo-roten? BUILD.md §3.3 säger repo-root `/prompts/`. Jag behåller det — min skiss ovan var fel. Rätt:

```
/prompts/                   (AI-prompts för JobbPilot-appen, per BUILD.md §3.3)
docs/
├── current-work.md
├── sessions/
├── decisions/
├── reviews/
├── test-reports/
├── research/
├── runbooks/
└── api/
```

### 10.1 `.claude/`-maps (uppdateras av `docs-keeper`)

Dessa är **hoist:ade från BUILD.md + kod**, inte handrullade:

```
.claude/
├── current-work.md                    (granular session tracker)
├── cqrs-complete-map.md               (Commands + Queries per feature)
├── aggregates-complete-map.md         (Aggregate roots, invariants, events)
├── entities-complete-map.md           (alla entities med FK-behavior)
├── endpoints-complete-map.md          (alla endpoints + auth)
├── prompts-map.md                     (alla /prompts/*.prompt.md med version)
├── feature-status.md                  (BE+FE feature-matrix)
└── refactoring-backlog.md             (upptäckta problem att städa)
```

---

## 11. GitHub-integration

Per Klas-beslut #10: GitHub Flow, ingen `develop`-branch. Staging är env.

### 11.1 Branches + protection

- `main` = production-ready, **protected**:
  - Require PR review (1 godkännande — i solo-dev-fas godkänner Klas sig själv via self-review + agent-rapport; när fler utvecklare tillkommer kräv en riktig human review)
  - Require status checks: `ci-build`, `ci-test`, `code-reviewer`, `security-auditor`
  - Require branches up-to-date before merging
  - Require conversation resolution
  - No force push
  - No branch deletion
- Feature-branches: `feat/<scope>-<beskrivning>`, `fix/<beskrivning>`, `chore/<beskrivning>`
- Squash-merge till `main` (ren historia, Klas-beslut #10)

### 11.2 Templates

**`.github/PULL_REQUEST_TEMPLATE.md`** (svenska):

```markdown
## Varför

<!-- Motivering: vilket problem löser denna PR? Länka till issue om relevant. -->

## Vad

<!-- Kort sammanfattning av vad som ändras. -->

- [ ] Ändring 1
- [ ] Ändring 2

## Hur testat

<!-- Vilka tester, i vilken miljö. -->

- [ ] `dotnet test` passerar lokalt
- [ ] `pnpm test` passerar (om FE-ändringar)
- [ ] Manuellt testat mot dev-miljön
- [ ] Integrationstester gröna

## GDPR / säkerhet

<!-- Endast fyllt i om PR:n rör PII, auth, eller AI-layer. -->

- [ ] N/A
- [ ] Ny PII lagras soft-delete-bar
- [ ] Audit-log-event finns
- [ ] BYOK-kodflöde reviewad

## Code review

<!-- Länkas automatiskt av github-action code-review.yml efter bygget. -->

🤖 Genererad med [Claude Code](https://code.claude.com)
Co-Authored-By: Claude Opus 4.7 <noreply@anthropic.com>
```

**`.github/ISSUE_TEMPLATE/bug-report.md`** (svenska, kort):

```markdown
---
name: Buggrapport
about: Rapportera ett fel i JobbPilot
labels: bug
---

## Beskrivning

<!-- Vad händer? -->

## Förväntat beteende

## Steg för att reproducera

1.
2.
3.

## Miljö
- Miljö: dev / staging / prod
- Browser + OS (om FE)
```

**`.github/CODEOWNERS`** (v1 — Klas på allt):

```
*       @klasolsson
```

När team växer: split per lager.

### 11.3 GitHub Actions workflows

Per Klas-beslut #7: CI/CD läggs till EFTER Claude Code-setup är validerat. Session 4-leverans, inte session 2/3.

**Planerade workflows (skrivs i session 4):**

| Fil | Trigger | Jobb |
|-----|---------|------|
| `.github/workflows/ci.yml` | PR + push till main | `build`, `test` (unit + architecture), `typecheck`, `lint` |
| `.github/workflows/code-review.yml` | PR opened / synchronize | Kör code-reviewer-agenten mot PR-diff, postar kommentar, skapar `docs/reviews/pr-<N>-<commit>.md` |
| `.github/workflows/security.yml` | PR + weekly cron | `security-auditor` + `dotnet list package --vulnerable` + `pnpm audit` + gitleaks |
| `.github/workflows/deploy-staging.yml` | Tagged push (`v*-rc*`) | Docker build + push till ECR + ECS deploy till staging — session 5+ |
| `.github/workflows/deploy-prod.yml` | Tagged push (`v*` not `-rc`) | Same mot prod — session 5+ |

### 11.4 Planimplikation: BUILD.md §6.1 och §18

BUILD.md §6.1 nämner `develop = dev-miljö` och §18 säger "Auto på merge till `develop`" / "Auto på merge till `staging`". Dessa måste uppdateras i session 3 steg 1:

| Sektion | Nuvarande | Ny |
|---------|-----------|-----|
| §6.1 | `develop = dev-miljö, ev. skyddad` | *(rad tas bort)* |
| §6.1 | `main = produktion` | `main = production-ready, skyddad, alla ändringar via squash-merge PR` |
| §3.3 miljö-tabell | dev: `Auto på merge till develop` | dev: `Deploy via tag v*-dev på main` |
| §3.3 | staging: `Auto på merge till staging` | staging: `Deploy via tag v*-rc* på main` |
| §3.3 | prod: `Manuell approval på main` | prod: `Deploy via tag v* (not -rc) på main med manuell approval` |

Se även DEL 13 (version-patch-lista).

---

## 12. AWS-setup

Per Klas-beslut #9: separat AWS-konto, MFA + IAM från start. Runbook skrivs i session 2 steg N men körs av Klas mellan session 2 och session 3.

### 12.1 Runbook — `docs/runbooks/aws-setup.md`

**Innehåll (på svenska):**

1. **Skapa nytt AWS-konto**
   - Om Klas redan har AWS Organizations: skapa nytt konto i organization via "Add account" → "Create new account", rooten får en alias som `jobbpilot+root@<klas-domän>`.
   - Annars: fristående konto på aws.amazon.com/signup. Root-email: ej Klas personliga primär; använd `jobbpilot+aws@<domän>` om möjligt (låter andra admins ta över).
   - Konto-alias: `jobbpilot-prod` (ja — redan i första kontot, för när vi skalar upp).

2. **Aktivera MFA på root OMEDELBART**
   - AWS Console → IAM → "Your Security Credentials" → "Multi-factor authentication".
   - Använd hårdvarunyckel (YubiKey) om tillgänglig; annars virtuell MFA i Authenticator.
   - Efter: **logga ut och logga aldrig in som root igen utom nödfall**.

3. **Skapa IAM Identity Center (SSO)** — ersätter klassiska IAM-users.
   - AWS Console → IAM Identity Center → Enable.
   - Skapa permission set `JobbPilotAdmin` (inledningsvis bred: `AdministratorAccess`; tightas i session 5+).
   - Tilldela Klas som enda user i "Users"-tabben.

4. **Konfigurera AWS CLI lokalt**
   ```bash
   aws configure sso
   # SSO Session name: jobbpilot
   # SSO start URL: https://<alias>.awsapps.com/start
   # SSO region: eu-north-1
   # Account: jobbpilot-prod
   # Role: JobbPilotAdmin
   # CLI profile: jobbpilot-dev
   ```

5. **Aktivera Bedrock-regioner**
   - AWS Console (region: eu-central-1) → Bedrock → Model access → Request access to all Anthropic models.
   - Samma i eu-west-1 som fallback.
   - Samma i eu-north-1 om listad (EU cross-region inference profiles kräver att åtminstone en källregion har access).
   - Approval kan ta några timmar. Klas får mail när klart.

6. **Aktivera CloudTrail + Cost Explorer**
   - CloudTrail → Create trail "jobbpilot-audit" → multi-region, log file validation på.
   - Billing → Cost Explorer → Enable.

7. **Sätt budget-alert**
   - Billing → Budgets → Create: "JobbPilot dev monthly" → $50 USD → email Klas vid 80%, 100%, forecast > 100%.

8. **Spara credentials-summary till password-manager**
   - Root email + MFA-backup-codes.
   - SSO start-URL.
   - Account ID.

### 12.2 Stoppunkt i session 3

Session 3 startar med att verifiera att Klas kört runbooken:

```bash
aws --profile jobbpilot-dev sts get-caller-identity
# Om OK → Account: <id>, Arn: arn:aws:iam::<id>:role/AWSReservedSSO_JobbPilotAdmin_*
```

Om fel → session 3 pausar, väntar på Klas.

---

## 13. Version-patch av BUILD.md (planering)

Tabell med exakt diff. **Skrivs INTE i denna session** — se session 3 steg 1.

| Sektion | Nuvarande | Föreslagen ändring | Motivering | Källa |
|---------|-----------|---------------------|------------|-------|
| §3.1 | `.NET 9.0 (migrera till 10.0 vid GA ~nov 2026)` | `.NET 10 (LTS till 2028-11)` | Redan GA sedan 2025-11-11 | [version-audit §1 rad 1](./SESSION-1-VERSION-AUDIT.md) |
| §3.1 | `C# 13 (12 fallback)` | `C# 14` | Följer .NET 10 | Audit rad 2 |
| §3.1 | `ASP.NET Core 9` | `ASP.NET Core 10` | Följer .NET 10 | Audit rad 3 |
| §3.1 | `EF Core 9` | `EF Core 10` | Följer .NET 10 | Audit rad 4 |
| §3.1 | `MediatR 12.x` | `Mediator.SourceGenerator 3.x (MIT, source-generators, AOT-kompatibelt)` | Klas-beslut #4 + licens-hedge | Audit rad 5 + Klas-beslut #4 |
| §3.1 | *(saknas — tester-sektionen refererar FluentAssertions implicit)* | **Lägg till rad:** `Test-assertions | Shouldly | 4.3.x | MIT, ersätter commercial FluentAssertions` | Klas-beslut FINDINGS §7.4 | [FINDINGS §7.4](./SESSION-1-FINDINGS.md) |
| §3.1 | `Mapster 7.x` | `Mapster 10.x` | Senaste stable | Audit rad 7 |
| §3.1 | `OpenTelemetry 1.10+` | `OpenTelemetry 1.15+` | Senaste stable | Audit rad 11 |
| §3.1 | `QuestPDF 2024.x | Community-license (free för JobbPilot)` | `QuestPDF 2026.2.x | Community MIT free under USD 1M revenue; set QuestPDF.Settings.License = LicenseType.Community` | License runtime-enforced | Audit rad 14 |
| §3.1 | `AWSSDK.BedrockRuntime 3.7.x+` | `AWSSDK.BedrockRuntime 4.x (Converse API)` | SDK v4 är aktuell | Audit rad 15 |
| §3.1 | `Anthropic.SDK 5.x+ (community)` | `Anthropic 12.x (officiell NuGet-paket)` | Officiell finns nu | Audit rad 16 + FINDINGS §7.3 |
| §3.1 | `Refit 7.x` | `Refit 10.x` | Senaste stable | Audit rad 17 |
| §3.1 | `PostgreSQL 17.x | RDS, Sweden region` | `PostgreSQL 18.3 | RDS eu-north-1` | Senaste stable + RDS-support | Audit rad 18 |
| §3.1 | `Redis 7.4` | `Redis 8.6` | Senaste stable (tri-license, vi använder extern → OK) | Audit rad 19 |
| §3.1 | `Next.js 15 (App Router)` | `Next.js 16.2 (App Router)` | Next.js 16 är GA | [FE-audit rad 1](./SESSION-1-VERSION-AUDIT.md) |
| §3.1 | `TypeScript 5.6+` | `TypeScript 6.0` | TS 6 är GA 2026-04-16 | FE-audit rad 3 |
| §3.1 | `Lucide React latest` | `Lucide React ^1.8` | Lucide 1.0+ publicerat | FE-audit rad 12 |
| §3.2 | `Compute (worker) eu-north-1` | *(oförändrat)* | Redan korrekt | — |
| §3.2 | `AI inferens (systemnyckel) eu-central-1 (Frankfurt) eller eu-west-1 (Irland)` | `AI inferens (systemnyckel) EU cross-region inference profile, callable från eu-north-1; default source eu-north-1` | EU-profil täcker eu-north-1 | FE-audit rad 15 |
| §3.2 | `PostgreSQL 17.x` | `PostgreSQL 18.3` | Samma som §3.1 | FE-audit rad 17 |
| §3.2 | `Terraform 1.9+` | `Terraform 1.14+` | Senaste stable | Infra rad 19 |
| §6.1 | `develop = dev-miljö, ev. skyddad` | *(rad raderas)* | Klas-beslut #10 (GitHub Flow) | — |
| §6.1 | `main = produktion, skyddad` | `main = production-ready, skyddad, alla ändringar via squash-merge PR` | Klas-beslut #10 | — |
| §3.3 | dev-rad: `Auto på merge till develop` + staging-rad: `Auto på merge till staging` | Ersätts: dev deploy via tag `v*-dev`, staging via `v*-rc*`, prod via `v*` med manuell approval | Klas-beslut #10 | — |
| §8.2 | `eu.anthropic.claude-haiku-4-5-<date>` placeholder + `eu.anthropic.claude-sonnet-4-6-<date>` placeholder | Sätts efter session 3 steg 3 (Bedrock-verifiering); tillfälligt sätta till `eu.anthropic.claude-haiku-4-5-20251001-v1:0` + `eu.anthropic.claude-sonnet-4-6` med kommentar `// Verifieras mot aws bedrock list-inference-profiles innan deploy` | FINDINGS §7.1 | — |
| §13.2 | *(saknas — hur secrets hanteras i dev)* | **Lägg till:** `Dev: .env-fil (gitignored); staging+prod: AWS Secrets Manager; BYOK: alltid KMS envelope` | Klas-beslut #8 | — |
| §17 | FluentAssertions-implicit refereras i sektionen | Skriv om: `xUnit + Shouldly (assertions) + NSubstitute (mocks)` | Klas-beslut FINDINGS §7.4 | — |
| §18 | `develop`/`staging`-branch-diskussion | Ta bort; notera att staging är env, inte branch | Klas-beslut #10 | — |

---

## 14. Bedrock inference profile-verifiering

Per [FINDINGS §7.1](./SESSION-1-FINDINGS.md) — exakta inference profile-ARNs varierar per källregion och får INTE cachas från docs.

### 14.1 Verifieringsplan (session 3 steg 3)

**Förkrav:** Klas har kört AWS-runbook (DEL 12); `aws configure sso`-profile `jobbpilot-dev` är aktiv; Bedrock model-access är approvad.

**Kommando:**
```bash
aws bedrock list-inference-profiles \
    --region eu-central-1 \
    --query 'inferenceProfileSummaries[?contains(inferenceProfileId, `anthropic`)]' \
    --output table \
  | tee docs/research/bedrock-inference-profiles-eu-central-1.md

aws bedrock list-inference-profiles \
    --region eu-north-1 \
    --query 'inferenceProfileSummaries[?contains(inferenceProfileId, `anthropic`)]' \
    --output table \
  | tee docs/research/bedrock-inference-profiles-eu-north-1.md
```

**Fall-logik:**
- Om `eu-north-1` listar Anthropic-profiler → primär region. Spara ARNs → BUILD.md §8.2.
- Om endast `eu-central-1` → primär. Cross-region från backend i eu-north-1.
- Om ingendera → `eu-west-1` fallback, dokumentera varför.

**Output-fil:** `docs/research/bedrock-inference-profiles.md` slås ihop från de två region-listningarna, innehåller tabell:

| Modell | Profile ID | Source regions | Latens från eu-north-1 (rtt ms) |
|--------|------------|----------------|----------------------------------|
| Haiku 4.5 | `eu.anthropic.claude-haiku-4-5-*` | *(auto)* | *(mät)* |
| Sonnet 4.6 | `eu.anthropic.claude-sonnet-4-6` | *(auto)* | *(mät)* |
| Opus 4.7 | `eu.anthropic.claude-opus-4-7-*` | *(om tillgänglig)* | *(mät)* |

### 14.2 Latens-mätning

Snabbt PoC-skript kör ett minimal `Converse`-anrop från eu-north-1 EC2 / lokalt och mäter round-trip:

```bash
time aws bedrock-runtime converse \
    --region eu-north-1 \
    --model-id "eu.anthropic.claude-sonnet-4-6" \
    --messages '[{"role":"user","content":[{"text":"hi"}]}]'
```

Tre mätningar per modell, median tas. Spara i samma fil.

---

## 15. Implementationsordning för session 3

Föreslagen ordning. Varje steg har tydliga förkrav och stoppunkter.

| # | Steg | Fristående? | Förkrav | Stoppunkt |
|---|------|-------------|---------|-----------|
| 1 | **BUILD.md version-patch** — applicera DEL 13-tabellen | Ja | — | Klas reviewar diffen innan commit |
| 2 | **AWS-runbook verifiering** — Klas har kört DEL 12 | Nej | Klas körd runbook | `aws sts get-caller-identity` passerar |
| 3 | **Bedrock inference profile-verifiering** — kör kommandon i DEL 14 | Nej | Steg 2 passerar + Bedrock model-access approvad | `docs/research/bedrock-inference-profiles.md` skapad + BUILD.md §8.2 uppdaterad med verifierade ID:n |
| 4 | **Docker + CLI-verifiering** — DEL 8 tabell | Ja | — | Alla verify-kommandon passerar |
| 5 | **Skapa tom `.claude/`-struktur** | Ja | — | Directories existerar |
| 6 | **`.claude/settings.json` skriven (utan hooks)** | Ja | Steg 5 | `claude /config` laddar utan fel |
| 7 | **Rules-filer skrivna** — `.claude/rules/*.md` (clean-arch, design-system, gdpr, testing, mediator-pattern, domain, anti-patterns) | Ja | Steg 5 | — |
| 8 | **Patterns-filer skrivna** — `.claude/patterns/` (command-full-template, query-full-template, entity-full-template, aggregate-root-template, page-template, component-template, query-hook-template, ai-prompt-template) | Ja | Steg 5 | — |
| 9 | **Planning-templates** — `.claude/templates/planning/` (STATE.md, PHASE.md, SUMMARY.md) | Ja | Steg 5 | — |
| 10 | **Agenter** — en i taget (DEL 1.3): code-reviewer → security-auditor → dotnet-architect → nextjs-ui-engineer → test-writer → db-migration-writer → ai-prompt-engineer → docs-keeper | Ja | Steg 6+7 | Varje agent: `claude /agents` listar den |
| 11 | **Skills** — en i taget (DEL 2) | Ja | Steg 10 | `claude /help` listar skill; test-invocation av `/status` fungerar |
| 12 | **Slash commands** — alla från DEL 3 (många är skills, så överlappar med 11) | Ja | Steg 11 | `/session-start` fungerar utan fel |
| 13 | **Hooks** — en i taget, farligast först: `guard-bash.sh` → `guard-spec-files.sh` → `session-start.sh` → `post-cs-edit.sh` → `post-ts-edit.sh` → `post-todo-review.sh` → `pre-compact-save.sh` | Nej | Steg 12 | Testa varje hook manuellt: `bash .claude/hooks/<name>.sh` med mock-input |
| 14 | **Code-reviewer auto-trigger workflow** — validera att `TodoWrite` med completed → invocation händer | Nej | Steg 13 | Manuell test: skapa dummy-kod-task, markera completed, verifiera att code-reviewer körs |
| 15 | **Husky setup** — `pnpm dlx husky install` + `.husky/pre-commit` + `.husky/pre-push` | Nej | Steg 4 (pnpm) | Manuell test: dummy-commit utan format fångas; dummy-push utan integration-test fångas |
| 16 | **GitHub-integration** — PR/Issue templates, CODEOWNERS, branch protection (ingen workflow än) | Ja | Repo på GitHub | `gh pr template list` visar template |
| 17 | **Docs-struktur** — skapa `docs/current-work.md`, `docs/sessions/`, `docs/decisions/` | Ja | — | `docs/current-work.md` har initial-innehåll |
| 18 | **Första ADR** — `0001-clean-architecture.md` | Ja | Steg 17 | ADR-filen finns med Klas-godkänd text |
| 19 | **Första session-logg** — `docs/sessions/2026-04-<dd>-<hhmm>-session-3.md` | Ja | Steg 17 | Fylld med session 3-sammanfattning |
| 20 | **CLAUDE.md-uppdatering** — lägg till "Session Protocol" + "Docs-struktur"-sektioner som LMS CLAUDE.md har | Nej | Alla ovan | Klas reviewar diff |
| 21 | **End-to-end smoke test** — skapa en tom feature via `/new-feature jobb-save-demo`, `/test`, `/review`, `/commit` | Nej | Alla ovan | Flödet fungerar utan manuell intervention |

**Tidsuppskattning session 3:** ~4-6 timmar om inget oväntat uppstår. Steg 2-3 kan ta ytterligare timmar om Bedrock-approval dröjer. Kan delas över två sessioner om behövligt.

---

## 16. Blockerande/viktiga frågor till Klas

Max 5 frågor. Grupperat efter risk.

### 16.1 BLOCKERANDE — måste lösas innan session 3 börjar

**Fråga 1 — Placering av `current-work.md`:**
LMS använder `.claude/current-work.md` (automatiskt läst av session-start). Klas-beslut #3 nämnde `docs/current-work.md`. Föreslår: primärfil i `.claude/current-work.md`, symlänk eller generated copy till `docs/current-work.md` för synlighet. **Godkänner du primär i `.claude/`?**

**Fråga 2 — Opus 4.7 modell-ID för agent-frontmatter:**
Ska `model: opus` (alias, följer senaste stable) eller hårdkodat `model: claude-opus-4-7` användas i `.claude/agents/code-reviewer.md` och `.claude/agents/security-auditor.md`? Alias är framtidssäkert men kopplar oss till automatisk modell-uppgradering. **Alias eller explicit?**

### 16.2 VIKTIG — helst innan, kan lösas under session 3

**Fråga 3 — GitHub-repo placering:**
Klas personliga GitHub (`@klasolsson`) eller org? Påverkar CODEOWNERS, branch protection, Actions-minuter (org får fler). Rekommendation: personligt i fas 0, flytta till org när >1 dev. **Personligt konto OK för start?**

**Fråga 4 — AWS Organizations:**
Har Klas befintlig AWS Organization? Om ja, vill du att JobbPilot läggs där (får central billing/SSO); om nej, fristående konto. Rekommendation: fristående om inget befintligt org finns. **Befintlig org eller nytt fristående?**

### 16.3 NICE-TO-HAVE — kan skjutas

**Fråga 5 — Discord/Slack-integration:**
LMS har CI → Discord-notification per workflow (färg-kodad embed). Värdefullt eller brus? Skippar som default i fas 0. Lägg till senare om Klas vill ha realtid-feedback från CI. **OK att skippa fas 0?**

---

## Bilaga A — Sammanställning av kontrakt mellan sektioner

| Kontrakt | Mellan | Innehåll |
|----------|--------|----------|
| Agent → Rule-filer | Alla agenter läser `.claude/rules/*.md` vid start | Engelska tekniska kontrakt |
| Agent → User | Alla agenter rapporterar på svenska | Klas läsvänligt |
| Skill → Agent | Skills delegerar till agenter där komplex logik krävs | Skill är tunnare än agent |
| Hook → Claude main | Hooks injicerar `additionalContext` eller blockerar (exit 2) | Klar ansvarsuppdelning |
| Husky → Git | Husky pre-commit/pre-push blockerar git-operationer | Oavsett om Claude Code kör eller Klas manuellt |
| `current-work.md` → Session | Session-start läser; session-end skriver via `docs-keeper` | Enda sanningen mellan sessioner |

---

## Bilaga B — Avvikelser från LMS (sammanfattning)

| Område | LMS-val | JobbPilot-val | Motivering |
|--------|---------|----------------|------------|
| Mock-ramverk | FakeItEasy | NSubstitute | CLAUDE.md §17, community-konvention i .NET |
| Assertions | Shouldly | Shouldly | **Samma — behåll** |
| Agent-uppdelning BE | feature-scaffolder monolit | `dotnet-architect` + `test-writer` + `db-migration-writer` | Tydligare ansvar |
| Agent-uppdelning FE | 3 agenter (feature-scaffolder, page-builder, api-integrator) | 1 agent (`nextjs-ui-engineer`) | Mindre FE-yta i v1 |
| chart-builder-agent | Finns | Skippas | JobbPilot v1 har 1 chart-sida |
| Mediator | MediatR 12 + FluentValidation | Mediator.SourceGenerator + FluentValidation | Klas-beslut #4 |
| Slash-command-struktur | `.claude/commands/<name>.md` | `.claude/skills/<name>/SKILL.md` (2026-form) | Future-proof, FINDINGS §1.1 |
| GitHub Project Board-sync | Hårdkodade field-IDn | Ingen board i v1 | Klas-beslut implicit — läggs senare |
| Discord-notifier | Color-coded embeds | Skippas v1 | DEL 16 fråga 5 |
| Docs-struktur | `docs/claude/SESSION_HISTORY.md` | `docs/sessions/<timestamp>-<slug>.md` | Tydligare timestamp-sortering |
| `.claude/current-work.md` | Plats standard | **Bekräftas i DEL 16 fråga 1** | Klas-beslut |
| Session-start via prompt | Nemo kör `/session-start` manuellt | SessionStart-hook + skill, båda finns | Klas vill auto-trigger |
| Code-reviewer auto-trigger | Inte satt i LMS | Hook på TodoWrite + Husky pre-commit | Klas högsta prio |
| Language i agents | Engelska genomgående | Engelska instruktioner + svensk output | Klas-beslut #5 |

---

## 17. Godkännande från Klas

**Datum:** 2026-04-18
**Status:** Planen godkänd. Session 3 startar i ny Claude Code-session.

### 17.1 Svar på DEL 16-frågor

1. **`current-work.md`-placering → `docs/current-work.md`**
   Motivering: `.claude/` är för Claude Code-konfiguration, inte projekt-state. `docs/` är där människor (och framtida klasskamrater) letar projekt-status.
   **Planimplikation:** DEL 10-strukturen justeras i session 3 steg 17 — `docs/current-work.md` är primärfil; ingen symlänk eller kopia till `.claude/` behövs. LMS-agent-filerna (som i session 1 motiverades med LMS-placering) uppdateras att läsa `docs/current-work.md`.

2. **Agent-modell → explicit model-ID:n, INTE aliases**
   - `claude-opus-4-7` för `code-reviewer` + `security-auditor`
   - `claude-sonnet-4-6` för övriga 6 agenter (`dotnet-architect`, `nextjs-ui-engineer`, `test-writer`, `db-migration-writer`, `ai-prompt-engineer`, `docs-keeper`)
   Motivering: aliases (`opus`, `sonnet`) pekar på "senaste stable" vilket ändras tyst när Anthropic släpper nya modeller. Explicit ID:n ger reproducerbart beteende och spårbarhet vid regression.
   **Planimplikation:** DEL 1.3 uppdateras i session 3 steg 10 med explicit `model:`-frontmatter-värden. Beslutet dokumenteras i **ADR 0002** (stub skapas i denna commit, fylls i under session 3).

3. **GitHub-repo → `github.com/klasolsson81/jobbpilot`**
   Personligt konto. Privat repo tills klass-launch.
   **Planimplikation:** DEL 11 CODEOWNERS-raden uppdateras till `@klasolsson81` (inte `@klasolsson`) i session 3 steg 16.

4. **AWS Organization → ingen**
   Fristående konto per DEL 12-runbook.
   **Planimplikation:** Runbook-steg "Om Klas redan har AWS Organizations..." i DEL 12.1 kan tas bort i session 3 steg 2; endast fristående-pathen gäller.

5. **Discord-notifier → skippa i fas 0**
   Email via AWS SES räcker.
   **Planimplikation:** DEL 11 workflow-tabellen (för session 4) tar inte med Discord-integration. Lämnas som framtida förbättring.

### 17.2 Små justeringar (båda tillämpade i denna commit)

**A. Code-reviewer trigger-policy** — DEL 1.3.1 "Auto-trigger"-cellen uppdaterad. Trigger är **inte** per `Edit`/`Write`-operation (blir spammigt vid 15 edits/task). Trigger är exakt PostToolUse(TodoWrite) enligt §4.6: task markeras `completed` OCH `git diff` visar kodändringar sedan senaste review.

> Not om referens: Klas beslutsdokument nämnde "§4.3" men §4.3 i denna plan är `guard-bash.sh`. Det semantiskt avsedda är §4.6 (PostToolUse(TodoWrite)-hooken). Uppdateringen pekar nu explicit på §4.6.

**B. Arkitekturtester — NetArchTest-notis** — DEL 2.2 `jobbpilot-clean-arch`-raden uppdaterad. `NetArchTest.Rules` är formellt abandoned sedan 2022 men funkar för JobbPilots skala i v1. Noterad som tekniskt skuld; `TngTech.ArchUnitNET` övervägs vid v2-refactor. Denna notis kodifieras i `.claude/rules/clean-arch.md` och `.claude/rules/testing.md` när de skrivs i session 3 steg 7.

### 17.3 Nästa session

**Session 3** startas i en ny Claude Code-session och följer DEL 15-ordningen (21 steg).

Förkrav som är uppfyllda eller levereras nu:
- ✅ `docs/decisions/0001-clean-architecture.md` skapad som tom stub (fylls i session 3 under lämpligt steg).
- ✅ `docs/decisions/0002-explicit-model-versions.md` skapad som tom stub (fylls i session 3 steg 10 när agent-modellerna skrivs).
- ✅ `docs/research/SESSION-2-PLAN.md` godkänd och uppdaterad (denna fil).
- 🔲 Commit + push till `main` på `github.com/klasolsson81/jobbpilot` — sker som sista steg i session 2.
- 🔲 AWS-runbook körd av Klas mellan session 2 och session 3 — stoppunkt i session 3 steg 2 verifierar.

---

**Slut på SESSION-2-PLAN.md.** Systerdokument:
- [`SESSION-1-FINDINGS.md`](./SESSION-1-FINDINGS.md) — research-underlag
- [`SESSION-1-VERSION-AUDIT.md`](./SESSION-1-VERSION-AUDIT.md) — konkreta version-ändringar för BUILD.md (implementeras session 3 steg 1)
- [`../decisions/0001-clean-architecture.md`](../decisions/0001-clean-architecture.md) — ADR-stub
- [`../decisions/0002-explicit-model-versions.md`](../decisions/0002-explicit-model-versions.md) — ADR-stub
