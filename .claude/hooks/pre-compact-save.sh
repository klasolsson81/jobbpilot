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
