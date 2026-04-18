#!/usr/bin/env bash
set -u
TOOL_INPUT="${CLAUDE_TOOL_INPUT:-$(cat)}"
FILE=$(echo "$TOOL_INPUT" | jq -r '.file_path // empty' 2>/dev/null)

if [ -z "$FILE" ] || [[ "$FILE" != *.cs && "$FILE" != *.csproj ]]; then
    exit 0
fi

# Scaffold-gate: om ingen .sln finns, exit tyst (projekt ej scaffoldat än)
SLN=$(find . -maxdepth 2 -name "*.sln" 2>/dev/null | head -1)
if [ -z "$SLN" ]; then
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
    TEST_CANDIDATE=$(echo "$REL" | sed 's|^src/|tests/|; s|\.cs$|Tests.cs|')
    if [ ! -f "$TEST_CANDIDATE" ]; then
        echo "ℹ Tips: testfil saknas för $REL. Kör /test --write $REL när implementationen är stabil."
    fi
fi

exit 0
