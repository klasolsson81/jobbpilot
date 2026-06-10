# design-reviewer — Platsbanken sök-paritet Fas E1a (/jobb-hero "Papperskontoret")

**Datum:** 2026-06-10
**Agent:** design-reviewer (VETO-mandat)
**Branch:** `feat/sok-paritet-fe-hero-e1a`
**Scope:** E1a hero-färgidentitet + regel-1-fixar + microcopy (E2-funktioner uttryckligen utanför scope)
**Slutverdikt:** ✓ APPROVED (efter VETO→åtgärd→re-review)

---

## Runda 1 — VETO (Changes requested)

Riktning A "Papperskontoret" **korrekt träffad**: varm canvas #FAF9F6, navy endast på Sök-knapp (en accentfärg, regel 5), drop-shadow → border (regel 1 "papper inte glas"), 40px→28px H1, token-flipp fullständig (inga kvarvarande hårdkodade hex i hero-barnen), `.jp-pagehero` orörd, microcopy civic + ingen Platsbanken-verbatim.

**2 Blockers (placeholder-kontrast under WCAG AA 4.5:1):**
1. **Dark** (`.jp-hero__input::placeholder` dark-override): `--jp-ink-3` (#8DA0BD) mot ljust dark-fält #F0F4FB = **2.41:1**. NY (introducerad av dark-overriden).
2. **Light** (`.jp-hero__input::placeholder`): `--jp-ink-3` (#7C8AA0) mot #FFFFFF = **3.5:1**. Pre-existing (tokeniserad).

design-reviewer-not: `.jp-input`-precedensen (rad ~1325, ~2782) bär samma latenta fel — spegla inte ett trasigt mönster. Rekommenderad rotfix: dedikerad `--jp-placeholder`-token.

1 Minor: placeholder-text bryter copy-skillens "rena fält"-regel — men mockup A är Klas-LÅST 2026-06-10 → nyare Klas-direktiv overrider (`feedback_v3_designspec_veto_scope`), ingen åtgärd.

## Åtgärd (in-block)

Ny token `--jp-placeholder: #626B78` (tema-oberoende — input-fältet är ljust i båda teman). Verifierad kontrast (Python WCAG-formel): **5.39:1** på #FFFFFF, **4.89:1** på #F0F4FB — båda ≥4.5:1. Sitter mellan ink-2 (7.83:1) och ink-3 (3.5:1) → läser som placeholder. `.jp-hero__input::placeholder` → `var(--jp-placeholder)`; dark-overriden borttagen (token funkar tema-oberoende på det ljusa dark-fältet).

`.jp-input`-instanserna (rad ~1325/~2782) lämnas — pre-existing, utanför E1a-hero-scope; token finns nu tillgänglig för separat a11y-touch.

## Runda 2 — RE-REVIEW: ✓ APPROVED

Båda Blockers verifierat lösta (design-reviewer körde egen WCAG-luminans-formel: 5.39 light / 4.89 dark — matchar). Ifylld text 15.83:1 vs placeholder 4.89:1 → tydlig semantisk separation. Scope-avgränsning av .jp-input accepterad (ingen FAS-DEFERRAL-MANIFEST behövs — godkännande med accepterad avgränsning, ej kvarstående rendered-veto). **0 Blockers / 0 Major / 0 Minor. VETO hävt.**

## Kvarstående gate

design-reviewer-mandatet (renderad-VETO på a11y/ton/regel-efterlevnad) är uppfyllt. **Klas-GO på renderad UI kvarstår** (ADR 0067 Beslut 7 rad 104 — Fas E kräver design-reviewer VETO + Klas-GO). Vercel-preview = Klas rendered-review-källa.
