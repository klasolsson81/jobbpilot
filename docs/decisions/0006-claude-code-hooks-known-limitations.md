# ADR 0006 — Claude Code hooks: kända begränsningar i VS Code-extensionen

**Status:** Accepted
**Datum:** 2026-04-18
**Kontext:** STEG 7.6 (end-to-end smoke-test av hooks-infrastrukturen)
**Beslutsfattare:** Klas Olsson

## Kontext

STEG 7.1-7.5 implementerade 7 Claude Code-hooks + 2 Husky-hooks per SESSION-2-PLAN §4. I STEG 7.6 kördes end-to-end smoke-test med Claude Code i VS Code-extensionen (inte terminal-CLI). Testet avslöjade tre observationer om hur VS Code-extensionen hanterar hook-output som skiljer sig från dokumenterat beteende i Anthropics hook-API.

## Observationer

### 1. SessionStart-output osynlig i chat-UI

Hook `session-start.sh` körs korrekt vid ny session (verifierat via temporär log-fil i `/tmp/`). Stdout från hooken visas dock **inte** i chat-tråden eller någon output-panel i VS Code-extensionen.

**Påverkan:** Klas får inte automatisk påminnelse om Docker-status, .env, uncommitted changes eller current-work.md-sammanfattning vid sessionstart. Hooken har fortfarande värde som data till framtida loggning/audit men primär UX-funktion är borttagen i VS Code.

**Mitigering:** Manuellt: `bash .claude/hooks/session-start.sh` kan köras i terminal för att få samma info. Långsiktigt: flytta session-start-UX till en slash command `/status` som är invocable från chatten.

### 2. PostToolUse(TodoWrite) `additionalContext` propageras inte till agent-loopen

Hook `post-todo-review.sh` **triggas korrekt** i VS Code-extensionen (empiriskt
verifierat 2026-04-18 via log-fil-instrumentering — hooken exekverades två
gånger under en TodoWrite-sekvens, vid `pending` och vid `completed`). Hooken
producerar korrekt JSON med `hookSpecificOutput.additionalContext` innehållande
instruktionen att invocera code-reviewer-agenten.

**Men:** VS Code-extensionen propagerar inte `additionalContext` till huvud-
Claudes nästa turn — additionalContext-mekanismen verkar tyst droppas av
extensionens agent-loop. Resultat: code-reviewer auto-invoceras inte efter
task-completion.

**Ej samma sak som GitHub issue #21736:** Issue #21736 rapporterar att
Claude Code-hooks generellt inte triggar i VS Code-extensionen (aktiv bug,
januari 2026). Våra tester visar att *våra* hooks triggar — problemet är
smalare (bara additionalContext-propagering). Skillnaden kan bero på
Claude Code-version, vilken typ av hook (SessionStart fungerar, PostToolUse
triggar men propagerar inte), eller specifikt VS Code-extensionens UI-läge.
Issue #21736 kvarstår oavsett som potentiell framtida regression att
övervaka.

**Påverkan:** Auto-trigger av code-reviewer efter task-completion fungerar
inte. Varje review måste invokas manuellt eller via Husky pre-commit (som
för närvarande har test-gates kommenterade tills Fas 0/1-scaffold finns).
Hooken har fortfarande sidovärde: den loggas/triggas för alla TodoWrite-calls,
och kan användas för framtida audit/telemetry även om additionalContext inte
propagerar.

**Mitigering:**
- Kortsiktigt: manuell invocation av `/review` eller direkt Task-tool med
  `subagent_type='code-reviewer'`.
- Medelsiktigt: aktivera Husky pre-commit-hookens code-reviewer-rad när
  Fas 0 har nog scaffold för att det ska vara meningsfullt. Pre-commit är
  hård gate (blockerar commit), vilket gör saknad auto-trigger till en
  "convenience-miss" snarare än säkerhetshål.
- Långsiktigt: återbesök när Anthropic uppdaterar VS Code-extensionen
  (issue #21736 stängs), eller när Klas byter till terminal-CLI-läge (där
  additionalContext-propagering sannolikt fungerar).

### 3. Code-reviewer sparar inte rapport i `docs/reviews/`

Agent-spec `.claude/agents/code-reviewer.md` säger att varje review ska sparas som markdown-fil i `docs/reviews/YYYY-MM-DD-HH-MM-<slug>.md`. I STEG 7.6 del 3 producerade code-reviewer en strukturerad svensk rapport i chatten men skapade ingen fil.

**Påverkan:** Ingen historik över reviews sparas automatiskt. Svårare att spåra återkommande fynd eller diff:a reviews mellan iterationer.

**Mitigering:** Uppdatera code-reviewer-promten i `.claude/agents/code-reviewer.md` när första "riktiga" reviewen sker i Fas 0. Enkelt fix: lägg till explicit steg "Efter du producerat rapporten, spara den till `docs/reviews/...` via Write-tool innan du returnerar kontroll."

### 4. Silent dependency failures i hooks

**Symptom:** `guard-spec-files.sh` triggade men släppte igenom alla Edit/Write
på spec-filer (BUILD.md, CLAUDE.md, DESIGN.md) utan approval-prompt under
STEG 10 (commit `bda9f72`).

**Rotorsak:** Hooken använde `jq` för att extrahera `file_path` från JSON-input.
`jq` saknades i Claude Code-spawn-context (samma WinGet PATH-propagerings-
problem som med `gitleaks` i STEG 8 followup). `2>/dev/null` på `jq`-anropet
dolde felet. Resultatet blev tom `FILE`-variabel → early-exit på rad
`if [ -z "$FILE" ]; then exit 0; fi` → silent allow.

**Mönster:** Detta var tredje incident i samma kategori under session 4:
1. STEG 8.3: `jq` saknades i `gh api`-verifiering — fix: använd `gh api --jq`
2. STEG 8 followup: `gitleaks` PATH-propagering broken — fix: trestegs
   fallback-lookup i Husky pre-push
3. STEG 10 followup (denna): `jq` saknas → guard-spec-files dead — fix:
   bash-native parsing utan externa beroenden

**Fix (commit `1879b4b`):**
- `guard-spec-files.sh` skrivet om med bash-native JSON-parsing (`grep -oE` +
  `sed -E`)
- Inget `2>/dev/null` — alla fel syns i stderr
- Loud failure: exit 2 om protected spec-fil utan approval-fras (tidigare
  silent exit 0)
- Stöd för Bash-tool också (fångar `sed -i CLAUDE.md`)
- Stöd för båda JSON-format (flat + wrapped)

**Lessons learned (gäller alla framtida hooks):**

1. **Undvik externa CLI-beroenden i hook-skript.** `jq`, `python`, andra
   tools failar tyst i Claude Code-spawn-context på Windows. Bash-native
   parsing (`grep`, `sed`, `awk`) finns alltid.
2. **Aldrig `2>/dev/null` på kommandon hookens säkerhet beror på.** Tyst
   fel = osäker hook utan att någon vet om det.
3. **Loud failure som default.** Hooks ska blockera när något oklart
   händer, inte tillåta. För spec-filer specifikt: fail-closed är rätt
   default — hellre falskt blockad legitim edit än tyst tillåten skadlig.
4. **Verifiera hooks empiriskt, inte bara konfigurationsmässigt.** Att
   settings.json refererar en hook betyder inte att hooken faktiskt
   skyddar något. Kör mock-tester (se test-suite i denna fix-commit).

## Beslut

**Acceptera de tre ursprungliga begränsningarna i Fas 0.** Ingen av dem blockerar
utveckling. Begränsning 4 är åtgärdad per commit som följer denna ADR-uppdatering.

- Begränsning 1 (SessionStart): kosmetisk miss, mitigation trivial.
- Begränsning 2 (auto-trigger): funktionell miss men Husky pre-commit är reell fallback-gate i Fas 0+.
- Begränsning 3 (reviews-persistering): fix planerad vid första Fas 0-review, inte brådskande.
- Begränsning 4 (silent dependency failures): **fixad** — bash-native parsing, inga externa beroenden.

Hooks-infrastrukturen är i övrigt komplett och verifierad isolerat (25/25 tester + smoke-test-del 3 godkänt).

## Konsekvenser

**Positivt:**
- STEG 7 stängs, STEG 8 kan börja.
- Begränsningarna är dokumenterade → inte dolda tekniska skulder.
- Alla tre har konkreta mitigationsvägar.

**Negativt:**
- Code-reviewer kräver manuell invocation i nuvarande setup. Klas måste komma ihåg `/review` eller motsvarande.
- Saknad auto-historik i `docs/reviews/` fram tills vi fixat agent-prompten.

## Öppna frågor

- Ska `/review` slash-commandot (STEG där slash commands skapas — troligen STEG 8 eller 9) prioriteras för att kompensera för begränsning 2? *(Skjuts till dess vi kommer dit.)*
- Ska session-start-UX flyttas till slash command `/status` redan nu? *(Nej, Fas 0 är tidigare.)*

## Relaterade ADRs

- ADR 0002 (agent-modell-ID:n) — code-reviewer kör Opus 4.7
- ADR 0005 (Fas 2-prereq) — ingen direkt koppling men samma "skjut till senare"-mönster
