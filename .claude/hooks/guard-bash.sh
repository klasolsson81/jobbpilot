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
