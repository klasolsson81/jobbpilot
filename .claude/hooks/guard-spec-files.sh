#!/usr/bin/env bash
set -u
TOOL_INPUT="${CLAUDE_TOOL_INPUT:-$(cat)}"
FILE=$(echo "$TOOL_INPUT" | jq -r '.file_path // empty' 2>/dev/null)

if [ -z "$FILE" ]; then exit 0; fi

# Guard spec files: kräv explicit Klas-approval i prompten
case "$FILE" in
    *BUILD.md|*CLAUDE.md|*DESIGN.md)
        LAST_PROMPT="${CLAUDE_USER_PROMPT:-}"
        if ! echo "$LAST_PROMPT" | grep -qiE '(godkänt|approved|uppdatera.*(build|claude|design)\.md|fixa.*(build|claude|design)\.md)'; then
            echo "[guard] $FILE är skyddad. Klas måste explicit godkänna i prompten ('Uppdatera BUILD.md med X'). Annars använd /adr för att spåra ändringar." >&2
            exit 2
        fi
        ;;
esac

exit 0
