#!/usr/bin/env bash
#
# guard-spec-files.sh — PreToolUse hook
#
# Blocks Edit/Write/Bash operations on spec files (BUILD.md, CLAUDE.md, DESIGN.md)
# unless the user prompt contains an explicit approval phrase.
#
# Bash-native JSON parsing (no jq dependency) — see ADR 0006 §4 for rationale.
#
# Exit codes:
#   0 = allow (file is not protected, OR approval phrase found)
#   2 = block (protected file, no approval phrase, OR JSON parse failure on
#       protected-file path)

set -u

TOOL_INPUT="${CLAUDE_TOOL_INPUT:-$(cat)}"

# If TOOL_INPUT is empty, nothing to check — allow
if [ -z "$TOOL_INPUT" ]; then
    exit 0
fi

# Bash-native file_path extraction (no jq).
# Handles both formats:
#   {"file_path": "..."}                                       (flat)
#   {"tool_input": {"file_path": "..."}, ...}                  (Claude Code real)
#
# Limitations (acceptable for our use case):
#   - Does not handle escaped quotes in values (filesystems disallow " anyway)
#   - Takes first file_path occurrence (sufficient for both supported formats)
extract_file_path() {
    local json="$1"
    echo "$json" \
        | grep -oE '"file_path"[[:space:]]*:[[:space:]]*"[^"]+"' \
        | head -1 \
        | sed -E 's/.*"file_path"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/'
}

# Bash-native command extraction for Bash tool inputs (e.g. sed -i CLAUDE.md).
extract_command() {
    local json="$1"
    echo "$json" \
        | grep -oE '"command"[[:space:]]*:[[:space:]]*"[^"]+"' \
        | head -1 \
        | sed -E 's/.*"command"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/'
}

FILE=$(extract_file_path "$TOOL_INPUT")
COMMAND=$(extract_command "$TOOL_INPUT")

# Determine target — file_path takes precedence; fall back to command for Bash tool
TARGET=""
if [ -n "$FILE" ]; then
    TARGET="$FILE"
elif [ -n "$COMMAND" ]; then
    # For Bash tool: check if command mentions a spec file
    if echo "$COMMAND" | grep -qE '(BUILD|CLAUDE|DESIGN)\.md'; then
        TARGET="$COMMAND"
    fi
fi

# If we extracted neither file_path nor relevant command, nothing to guard
if [ -z "$TARGET" ]; then
    exit 0
fi

# Check if target references a protected spec file
case "$TARGET" in
    *BUILD.md*|*CLAUDE.md*|*DESIGN.md*)
        # Protected — require approval phrase in user prompt
        LAST_PROMPT="${CLAUDE_USER_PROMPT:-}"
        if echo "$LAST_PROMPT" | grep -qiE \
            '(godkänt|approved|uppdatera.*(build|claude|design)\.md|fixa.*(build|claude|design)\.md|STEG [0-9]+.*(BUILD|CLAUDE|DESIGN)\.md)'; then
            # Approval phrase found — allow
            exit 0
        fi
        # No approval — block
        echo "[guard-spec-files] BLOCKED: $TARGET is a protected spec file." >&2
        echo "[guard-spec-files] User prompt must contain approval phrase:" >&2
        echo "[guard-spec-files]   - 'godkänt', 'approved'" >&2
        echo "[guard-spec-files]   - 'uppdatera/fixa BUILD.md|CLAUDE.md|DESIGN.md'" >&2
        echo "[guard-spec-files]   - 'STEG <n> ... BUILD/CLAUDE/DESIGN.md'" >&2
        exit 2
        ;;
esac

# Not a protected file — allow
exit 0
