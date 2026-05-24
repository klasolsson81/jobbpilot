# Code-reviewer — F-Pre Punkt 5 "Utforska som gäst"

**Datum:** 2026-05-24
**Agent:** code-reviewer (agentId `aceb185e97c67f577`)
**Status:** Approved with Minor — alla in-block-fixade

---

## Summary

**0 Block / 0 Critical / 0 Major / 1 Medium / 4 Minor + 9 Praise**

## Fynd & resolution

### M1 — Två identiska "Utforska"-knappar i welcome-modal
**Resolved:** EN primärknapp "Börja utforska" (per design-reviewer B2 + WCAG 2.4.6 + DESIGN.md §6). Klas-direktiv var "Okej eller Utforska — du kan bestämma något lämpligt" — single CTA accepterar direktivet utan att bryta a11y.

### m1 — Duplicerad `GUEST_WELCOMED_MAX_AGE`-konstant
**Resolved:** Borttagen från `guest-mode.ts`, kvar i `guest-mode-actions.ts`.

### m2 — `"use server"` vs `"server-only"`-separation
**Resolved:** Informativ, ingen åtgärd. Bra design.

### m3 — Inline-styles i guest-cv-page + guest-ansokningar-page
**Resolved:** In-block-fix per design-reviewer M1+M2 — ersatta med `.jp-guest-resume*` + `.jp-guest-applist__empty`-klasser.

### m4 — Saknar regression-test mot OVERSIKT_MOCK-import
**Resolved:** Tillagt i `mock-data.test.ts` ("OVERSIKT_MOCK re-export"-suite).

## Praise (utvalda)

- Clean separation `"use server"` vs `"server-only"`
- `__Host-`-prefix + `secure: true` + `path: "/"` paritet med session
- Mockdata härledd (inte hårdkodad) — single source of truth, synk-disciplin via summary-aggregering
- `ReadonlyArray<T>` + `readonly` genomgående
- Civic-utility-disciplin mekaniskt verifierad i tester (emoji + utropstecken-regex)
- `Promise.all` för parallell stats + session
- CTO Beslut 3 Alt 2 implementerat med graceful URL-fallback
- Inga `any`, inga `console.log`, inga `useEffect` för datahämtning

## CTO-rekommendation som inte adresserades

CTO Beslut 1 §"Cross-pollination-skydd" rad 80: *"Login-flow på /logga-in rensar __Host-jobbpilot_guest_welcomed vid lyckad inloggning."* — **inte implementerad** i denna batch. Låg konsekvens (cookien är icke-säkerhetskritisk UX-state, security-auditor M-1 har nu satt `httpOnly: true` så XSS-vektorn är borta). Kan adresseras opportunistiskt nästa gång `loginAction` touchas — inte lyft som TD per §9.6 fas-regeln.
