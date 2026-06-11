---
session: E2i — spegel-sökfält
datum: 2026-06-12
slug: e2i-spegel-sokfalt
status: PR öppen (automerge)
commits:
  - feat(web): Platsbanken sök-paritet Fas E2i — spegel-sökfält
  - docs: ADR 0067 impl-notat E2i + agent-reviews + current-work + session-logg
---

# Session E2i — spegel-sökfält (Klas rendered-feedback på E2h)

## Bakgrund
Klas testade E2h (#52) renderat: chips-i-fältet wrappade fult (inputen som egen
vit låda) och fritext-taggen ("Systemutvecklare" = q-ord, ej exakt yrkesgrupp-
label) syntes inte i filter-raden (E2e-designen visade bara dimensioner).
Klas-val via AskUserQuestion: **"Normal ruta som speglar söket"** + alla taggar
i filter-raden + **"Rensa allt inkl. sökorden"**. Sessionen avbröts på
sessionsgränsen mitt i reviews 2026-06-11 → återupptagen "fortsätt".

## Beslut
- architect: **Variant C′** (text = buffert, URL = sanning, I1: parse(text) ⊆
  state som DELMÄNGD, delta-parse, kirurgisk ×-edit, gated serialize) — ren
  A/B/C avvisade med rundtripps-inventering (ambiguösa labels, komma-labels,
  operator-ord, cross-boundary-capture).
- CTO: VAL 1=C′ · VAL 2=greedy longest-match (dependency av rundtrippen) ·
  VAL 3=caret-segment-exkludering · VAL 4a-d (popover ej i text; Title-append;
  rivning; q-tagg-design till design-reviewer).
- CTO-addendum efter code-review (3 Major): BESLUT 1 recentCommits-lista
  (singel-detektorn race:ade vid två commits i flykt) + sort/pageSize-adoption;
  BESLUT 2 enforceClaims-I1-pass (per-län-norm/field-removal får inte släcka
  text-anspråk); BESLUT 3 lastCommitted-ersätter-useOptimistic ACK.

## Levererat
tokenize.ts omskriven till parse/serialize-paret (buildLabelIndex multi-ord,
parseSearchText med run-gränser + caret-exkludering, applyClaimsDelta +
enforceClaims, isTextRepresentable, serializeSearchText med holistisk verify,
updateTextForStateChange, getTokenRange); chip-composition Title-append;
JobbHeroSearch-rewrite (text-buffert, recentCommits, delta-commit-punkter,
insert-gated förslags-val, suggestQuery=caret-token); typeahead suggestQuery +
onChange-caret (onEmptyBackspace/inputRef rivna); toolbar includeQ + Search-
ikon-q-taggar + "Rensa sökord och filter" + role=group; ChipSearchField +
chipfield-CSS rivna; hit-area-CSS 32px.

## Reviews
- code-reviewer: Changes requested (0 Block/3 Major/4 Minor) → CTO-triage →
  alla åtgärdade → **re-review Approved** (1 ny Minor sortBy-adoption → fixad
  in-block).
- design-reviewer: 0 Blockers/2 Major (hit-area, rensa-copy)/4 Minor — M1/M2/
  Mi1–Mi3 åtgärdade ordagrant (re-review ej nödvändig per domen); Mi4 + R1–R3
  → Klas rendered-pass.
- Gates: tsc/eslint rena, **830 vitest**, build grön.

## Nästa session
- Klas rendered-test: spegel-flödet (skriv "göteborg volvo " → text kvar,
  taggar i filter-raden), R1 feedback-närhet, R2 delmängds-spegeln, R3 lång
  text; "Rensa sökord och filter".
- Minus-operatorn (NOT) fortfarande Klas-pending (backend-fas).
- Pending sedan tidigare: spec-edit-hooken, de-grönings-domar, zod-drift-
  triage, re-ingest Klass 2.
