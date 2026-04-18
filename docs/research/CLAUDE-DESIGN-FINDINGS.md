# Claude Design & design-skills — research

> **Status:** Research only. Ingen implementation i denna session.
> **Datum:** 2026-04-18
> **Scope:** Claude Design (Anthropic Labs, lanserad 2026-04-17) + community-mönster för design-skills i Claude Code + Figma MCP-läget.
> **Systerdokument:** [`SESSION-2-PLAN.md`](./SESSION-2-PLAN.md) (uppdateras i samma commit som denna fil).

---

## 1. Claude Design (produkten)

### 1.1 Kort översikt

Claude Design är en Anthropic Labs-produkt lanserad **2026-04-17** (dagen innan detta skrivs). Den beskrivs av Anthropic som "a new Anthropic Labs product that lets you collaborate with Claude to create polished visual work like designs, prototypes, slides, one-pagers, and more" ([anthropic.com/news/claude-design](https://www.anthropic.com/news/claude-design-anthropic-labs)).

**Underliggande modell:** Claude Opus 4.7.

**Inputs:**
- Natural-language-prompts
- Uppladdade bilder + dokument (DOCX, PPTX, XLSX)
- Pointers till din codebase
- Web-capture-verktyg: "grab elements directly from your website"
- Under onboarding läser produkten codebase + design-filer och bygger ett design-system (colors, typography, components) som sedan appliceras på efterföljande projekt ([VentureBeat](https://venturebeat.com/technology/anthropic-just-launched-claude-design-an-ai-tool-that-turns-prompts-into-prototypes-and-challenges-figma), [Muzli](https://muz.li/blog/getting-started-with-claude-design-a-collaborator-for-your-design-workflow/))

**Outputs:**
- Interaktiva prototyper / wireframes ("with voice, video, shaders, 3D and built-in AI")
- Pitch decks
- Marketing collateral
- Landing pages
- Exports till: Canva, PDF, PPTX, standalone HTML, folder/URL + **Claude Code handoff**

**Claude Code-integration:**
Anthropic säger: *"When a design is ready to build, Claude packages everything into a handoff bundle that you can pass to Claude Code with a single instruction."* Tredjeparts-täckning ([vibecoding.app](https://vibecoding.app/blog/claude-design-review), [banani.co](https://www.banani.co/blog/claude-design-review)) beskriver bundle-innehållet: **components, design tokens, copy, interaction notes**. Handoff-modalen renderar en terminal-stylad prompt med copy-button; kommandot refererar designen via en API-URL som Claude Code sedan fetchar.

⚠️ **Exakt on-disk-schema för bundle är INTE publikt dokumenterat** — oklart om det är JSON, mapp med MD-filer, eller proprietärt format. Behandlas som "black-box URL-handoff" tills vi testar empiriskt.

**Pris:** Inkluderat i Pro, Max, Team, Enterprise. Extra usage säljs ovanpå. Enterprise-admins måste enable:a i org-inställningar.

**Positionering:** *"Built for people who aren't starting from a design tool and need to get from an idea to something visual quickly"* — PMs, devs, founders, marketers. **Ej multiplayer** ([VentureBeat](https://venturebeat.com/technology/anthropic-just-launched-claude-design-an-ai-tool-that-turns-prompts-into-prototypes-and-challenges-figma)) — ersätter inte Figma för team-pixel-arbete.

### 1.2 Relevans för JobbPilot

#### v1 produkt-UI — **Nej.**

Motivering:
- JobbPilots v1-UI är medvetet civic-utility (Digg/1177-ton).
- DESIGN.md §2 har **låsta tokens** (myndighetsblå, radius ≤ 6px, inga gradients).
- CLAUDE.md §5.2 **förbjuder** glow, drop shadows > `shadow-sm`, glasmorfism — allt som Claude Design är byggt att excellera på.
- Claude Designs värde är "rapid visual exploration" och "on-brand deck in minutes" — det är brus när briefen är "look like a government portal".
- Att mata in existerande codebase → få bundle tillbaka skulle regenerera det vi redan har.

#### Marketing / kommunikation — **Ja, villkorligt.**

Relevant för:
- **LIA-pitch deck** till Infinet / Klas handledare (fas 7/8)
- **Klass-launch-landing-variationer** (fas 8)
- **Pitch-deck till investor one-pager** om det blir aktuellt
- **Sommar-demo-material** för klasskamrater

Export till PPTX + PDF passar exakt denna workflow. Värt ett 30-minuters trial på LIA-presentationen specifikt. **Ska INTE röra produkt-kod-pathen.**

### 1.3 Beslut för JobbPilot v1

- **Skippa Claude Design för produkt-UI.** Inkludera inte i `.claude/settings.json` MCP-serverlista.
- **Omvärdera efter klass-launch** för marknadsmaterial.
- Dokumenteras i **SESSION-2-PLAN §18** (ny sektion — framtida möjligheter).

---

## 2. Community-mönster för design-skills i Claude Code

### 2.1 Taxonomier (de tre mest citerade)

| Upphovsperson | Dimension | Struktur | Källa |
|---------------|-----------|----------|-------|
| **Katherine Yeh** (Medium/Bootcamp, mar 2026) | Dependency | 3 lager: *Reference Skills* (principles, tokens, components, content, motion) → *Capability Skills* (standalone / reference-dependent / MCP-dependent workflows) → *Connectors* (MCPs). Kebab-case `verb-noun`. | [medium.com — Yeh](https://medium.com/design-bootcamp/a-designers-guide-to-organizing-ai-skills-and-tools-in-claude-code-f87477c35b82) |
| **Julian Oczkowski** (Medium, mar 2026) | Process-fas | 7 skills per designprocess-fas: Grill Me → Design Brief → IA → Design Tokens → Brief to Tasks → Frontend Design → Design Review. | [medium.com — Oczkowski](https://medium.com/@julian.oczkowski/7-claude-code-design-skills-that-follow-a-real-design-process-b871b8673d05) |
| **Marie Claire Dean** (Substack) | Practice-area | 63 skills i 8 plugins: research, systems, strategy, UI, interaction design, prototyping & testing, design ops, designer-toolkit. Kedjade via `/discover`, `/strategize`, `/handoff`. | [marieclairedean.substack.com](https://marieclairedean.substack.com/p/i-built-63-design-skills-for-claude) |
| **Snyk** (artikelserie) | Concern | 8 skills grupperade i: aesthetic direction, design intelligence, engineering patterns, compliance/quality gates. | [snyk.io — top claude skills](https://snyk.io/articles/top-claude-skills-ui-ux-engineers/) |

### 2.2 Observation för JobbPilot-kontext

Klas föreslog i session 2.5-prompten axeln **principles / tokens / components / copy / a11y** (+ ev. motion). Det är **inte** den dominanta community-splittet på top-nivå — Yehs dependency-axel och Oczkowskis processaxel dominerar.

**MEN:** principles / tokens / components / copy / a11y är exakt vad Yehs **Layer 1 (Reference Skills)** innehåller som items. För JobbPilot — som har:
- **Låst design-system** (inget "generate-design"-behov)
- **Liten komponentyta** (v1: shadcn-bas + ~10 egna komponenter)
- **Civic-utility-tvång** (färre val, fler regler)

— behöver vi främst referens-skills, inte capability-skills. Klas taxonomi är alltså en *avsmalning* av Yehs Layer 1, anpassad efter att JobbPilot saknar "design-new-thing"-workflows (det jobbet gör `nextjs-ui-engineer`-agenten scaffolda, och `design-reviewer`-agenten kontrollera).

**Slutsats:** Klas föreslagna 5-delning passar JobbPilots v1-behov. Vi lägger INTE till capability-skills (som `generate-design` eller `identify-ux-problems`) eftersom de inte löser ett verkligt problem för civic-utility-UI med låsta tokens.

### 2.3 SKILL.md-format — community vs officiell konvention

**Officiell Anthropic-guidance** ([github.com/anthropics/skills — skill-creator](https://github.com/anthropics/skills/blob/main/skills/skill-creator/SKILL.md), [platform.claude.com — skills best practices](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/best-practices)):

- YAML-frontmatter med **två obligatoriska fält**: `name` (≤64 chars, lowercase-alphanumeric-hyphens) och `description` (≤1024 chars).
- Optional: `model`, `version`, `disable-model-invocation`, `compatibility`.
- **Anthropic sägs explicit**: *"All 'when to use' info goes here [in description], not in the body."* → det finns alltså **inget separat `when_to_use`-fält** i officiella konventionen.
- Rekommendation: "pushy" description för att motverka under-triggering.

**Claude Code 2026-docs** (per vår SESSION-1-FINDINGS §1.1): listar `when_to_use` som accepterat frontmatter-fält. Dvs Claude Code *stödjer* det, men Anthropics skill-creator-mall *packar in det i description*.

**Konsekvens för SESSION-2-PLAN:**
Vår existerande plan använder båda (`description` + `when_to_use`). Det är **icke-standard men giltigt**. För konsekvens med Anthropics officiella pattern kan vi i session 3 välja en av två stilar:

1. Behåll både fält (nuvarande plan) — mer explicit, lite mer text att läsa.
2. Slå ihop `when_to_use` in i `description:` (Anthropic-official).

**Rekommendation:** behåll båda fält för nu — läsbarhet för människor > filstorlek. Revisit om Claude misslyckas trigga skills (dvs undertriggering — då bör vi göra descriptions mer "pushy" per Anthropic-rekommendationen).

### 2.4 Referens-filer & progressive disclosure

Officiell layout:

```
<skill-name>/
├── SKILL.md                 # trigger + core rules (lätt)
├── references/              # laddas on-demand vid djupdykning
│   ├── tokens-full.md
│   └── component-specs.md
├── scripts/                 # exekverbara (valfritt)
└── assets/                  # templates, icons, fonts
```

**Progressive disclosure:** SKILL.md är lättvikt (~100-300 rader); tunga specs ligger i `references/*.md` som Claude läser *om* de behövs. Detta sparar context och är Anthropic-rekommenderat.

Design-specifikt för JobbPilot:
- `jobbpilot-design-tokens/SKILL.md` = kort regelsamling ("använd `--brand-600` för primär, inga hårdkodade hex") + `references/tokens-full.md` (full 50-rads tokens-tabell).
- `jobbpilot-design-components/SKILL.md` = "följ shadcn + DESIGN.md" + `references/component-specs.md` (Button/Input/Card/Table full spec).
- Samma för övriga.

### 2.5 Verbatim-exempel från communityn

**Anthropics officiella style** (skill-creator):
> *"How to build a simple fast dashboard to display internal Anthropic data. Make sure to use this skill whenever the user mentions dashboards, data visualization, internal metrics, or wants to display any kind of company data, even if they don't explicitly ask for a 'dashboard.'"*

**Anthropics strukturpattern:**
> *"This is the primary triggering mechanism - include both what the skill does AND specific contexts for when to use it."*

**Nick Babichs regel 1 från "7 Rules"** ([uxplanet.org](https://uxplanet.org/7-rules-for-creating-an-effective-claude-code-skill-2d81f61fc7cd)):
> *"One skill, one job is the most important rule for crafting a skill — don't build 'mega-skills' that try to accomplish a few different things because mega-skills typically have lower accuracy and composability."*

---

## 3. Figma MCP

### 3.1 Status

- **Beta, officiell Figma-produkt.** Figma help-center: *"We're quickly improving how Figma supports AI agents. This will eventually be a usage-based paid feature, but is currently available for free during the beta period."* ([help.figma.com](https://help.figma.com/hc/en-us/articles/32132100833559-Guide-to-the-Figma-MCP-server))
- **Claude Code stöds officiellt.** Install via `claude mcp add --transport sse figma-dev-mode-mcp-server http://127.0.0.1:3845/sse` (lokal desktop) eller `claude plugin install figma@claude-plugins-official`.
- Februari 2026: Figma shippade **"Code to Canvas"** specifikt för att stänga loopen med Claude Code ([figma.com/blog](https://www.figma.com/blog/introducing-claude-code-to-figma/)).

### 3.2 Relevans för JobbPilot

**Skip.** Motivering:
- JobbPilot använder inte Figma som design-verktyg.
- DESIGN.md är single-source-of-truth för tokens.
- v1-UI är medvetet begränsat.

Omvärdera bara om en designer joinar teamet eller vi behöver exportera pitch/brand-artefakt från Figma.

---

## 4. Rekommendation för JobbPilot

### 4.1 Split DESIGN.md till 5 skills

**Behåll Klas taxonomi** (principles / tokens / components / copy / a11y) — passar JobbPilots v1-behov. Ingen ytterligare capability/workflow-skill behövs eftersom:
- Scaffolding görs av `nextjs-ui-engineer`-agenten
- Verification görs av **ny `design-reviewer`-agent** (se SESSION-2-PLAN §1.3.9)

### 4.2 Skill-struktur

Per skill:
```
.claude/skills/jobbpilot-design-<area>/
├── SKILL.md                 # lättvikt: trigger + kärnregler
└── references/              # djupa specs, laddas on-demand
    └── <area>-full.md
```

### 4.3 DESIGN.md → index-fil

DESIGN.md behålls som fil i repo-roten men **förvandlas till ren index**:
- Kort filosofi (§1 behålls)
- Länkar till varje skill-mapp (`.claude/skills/jobbpilot-design-*`)
- Ingen duplicering av tokens / komponent-specs / copy-regler

Människor som läser repot får en ingång; Claude Code läser rätt skill när relevant.

### 4.4 Claude Design — skippa för v1, omvärdera efter klass-launch

Dokumenteras i SESSION-2-PLAN §18.

### 4.5 Figma MCP — skippa

Ingen integration, ingen rad i settings.json.

---

## 5. Öppna frågor

1. **`when_to_use`-fält vs pack-in-description?** Nuvarande SESSION-2-PLAN använder båda. Anthropic-officiell konvention är bara `description:`. Rekommendation: behåll båda för läsbarhet, revisit om undertriggering sker i praktiken.
2. **Ska `references/`-filer versionerade i git?** Ja — de är del av `.claude/`-strukturen och committas. Gitignored:as inte.
3. **Ska `design-reviewer` ha egen skill-invocation (`/design-review`) eller bara auto-trigger?** Rekommendation: båda. Auto-trigger via hook + manuell `/design-review` via skill.

---

**Slut på CLAUDE-DESIGN-FINDINGS.md.** Konkret planåtgärder i [`SESSION-2-PLAN.md`](./SESSION-2-PLAN.md) §1 (ny agent), §2 (ersatt design-rad), §18 (ny sektion). ADR i [`../decisions/0003-design-as-skills.md`](../decisions/0003-design-as-skills.md).
