# Design-reviewer — F-Pre Punkt 5 "Utforska som gäst"

**Datum:** 2026-05-24
**Agent:** design-reviewer (agentId `a073ba1e9735d9811`)
**Status (initial):** CHANGES REQUESTED (NEEDS_REWORK)
**Status (post-fix):** APPROVED — alla blockers + majors in-block-fixade

**FAS-DEFERRAL-MANIFEST:** Rond utförd PRE-screenshot (visual-verify körs post-deploy av Klas — `dev-test-creds` utanför repo). Rendered-veto F4-F7-disciplin EJ tillämplig på denna initial rond — alla fynd är kod-pariteter mot DESIGN.md/skills som verifieras statiskt. Post-deploy kan rendered-rond triggas separat.

---

## Summary

**Initial:** 2 Blockers / 6 Major / 3 Minor / 11 Praise
**Efter in-block-fix:** 0 Block / 0 Major / 3 Minor (informativa)

## Fynd & resolution

### B1 — GuestShell saknar `aria-current="page"` (WCAG 2.4.8 Location)
**Resolved:** GuestShell konverterad till `"use client"` + `usePathname()` + `isActive()`-helper (paritet med `app-shell.tsx:305`). `.jp-nav__link[aria-current="page"]`-aktiv-stripe träffar nu korrekt i guest-tree.

### B2 — Två identiska "Utforska"-knappar (WCAG 2.4.6 + DESIGN.md §6)
**Resolved:** EN primärknapp "Börja utforska". Modal stänger via knapp, X eller Escape. Klas-direktiv "Okej eller Utforska" uppfyllt (en knapp räcker).

### M1 — Inline-styles i GuestCvPage (DESIGN.md §3 tokens)
**Resolved:** `.jp-guest-resume*`-klasser tillagda i `globals.css`. Inline `style={{...}}`-objekt borta.

### M2 — Inline-style tom-state i GuestAnsokningarPage
**Resolved:** `.jp-guest-applist__empty`-klass.

### M3 — Hex-färger i LandingHeroSection
**Resolved:** `#fff` → `var(--jp-surface)`, `#0A2647` → `var(--jp-navy-800)`, `#FFFFFF` → `var(--jp-ink-inverse)`.

### M4 — DEMO-banner CTA-länk fokusring otydlig
**Resolved:** Explicit `.jp-demo-banner__cta:focus-visible`-regel: 2px solid #fff, offset 3px, lätt bg-kontrast, 3px radius.

### M5 — Hårdkodat `46_000` i GuestOversiktPage
**Resolved:** Flyttat till `GUEST_MOCK.activeJobAdsTotal` (single source of truth).

### M6 — `style={{ height: 36 }}` på GuestShell-knappar
**Resolved:** `.jp-btn--sm`-klass (existerande modifier).

### m1, m2, m3 (informativa)
- **m1** (DEMO-pill-ikon): icke-krav, polish-deferrad
- **m2** (rubrik-affordans i modal): icke-krav
- **m3** (Search-ikon på "Jag har konto"): icke-krav, kan polish:as senare

## Praise (utvalda)

- Inga emoji, inga utropstecken, inga gradients/glow
- Border-radius ≤ 6px överallt
- Welcome-modal-copy = rakt, du-tilltal, 1177/Digg-ton
- GuestOversiktPage notice-list återanvänder `.jp-notice--*`-mönster (paritet)
- GuestShell header vit bg paritet med AppShell, ingen HeaderStats (CTO Beslut 2)
- Skip-link `<a href="#main">` korrekt
- DialogContent ger inbyggd sr-only "Stäng" X-knapp
- DEMO-banner kontrast AAA: vit text på navy-700 ≈ 8.0-10.4:1
- CTO-dom följd troget — Variant A, cookie över localStorage, import-återexport, Alt 2 för /jobb
- Header-stats label-paritet ("aktiva annonser") matchar landing-topbar
- Civic-utility-disciplin mekanisk verifiering i vitest (emoji/utropstecken-regex)

## Post-screenshot follow-up

Design-reviewer redo för rendered-rond med FAS-DEFERRAL-MANIFEST-prefix om visual-verify post-deploy avslöjar rendering-avvikelse från statisk analys.
