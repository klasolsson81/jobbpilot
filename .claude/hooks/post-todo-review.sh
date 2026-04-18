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
UNTRACKED=$(git ls-files --others --exclude-standard 2>/dev/null || true)
ALL_CHANGED=$(printf '%s\n%s\n%s\n' "$CHANGED" "$CHANGED_CACHED" "$UNTRACKED" | sort -u | grep -vE '^$')

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
