# Code-review: Frontend-scaffold (Fas 0, steg 4a)

**Datum:** 2026-05-06
**Granskad av:** code-reviewer agent
**Status:** GODKÄND — 0 blockers, 0 major, 3 minor

---

## Per-fil resultat

| Fil | Status | Noteringar |
|---|---|---|
| `src/app/layout.tsx` | PASS | Server Component, korrekt `lang="sv"`, semantiska tokens |
| `src/app/globals.css` | PASS | Tokens centraliserade, dark-mode bortagen med motivering, radius låst |
| `src/app/(marketing)/page.tsx` | PASS | Server Component, all copy svensk, semantiska tokens |
| `src/components/ui/button.tsx` | PASS (minor) | Behåller `dark:`-varianter trots att dark-mode är borttagen |
| `src/components/ui/input.tsx` | PASS (minor) | Samma `dark:`-residuer som button |
| `src/components/ui/card.tsx` | PASS | Inga hårdkodade färger, `rounded-lg` = 6px |
| `src/lib/utils.ts` | PASS | Minimal `cn`-helper, idiomatisk |
| `next.config.ts` | PASS | Minimal turbopack-root |
| `tsconfig.json` | PASS | `strict: true`, `noUncheckedIndexedAccess: true`, korrekt path-alias |

---

## Fokuspunkter — alla godkända

1. **Server vs Client Component:** Inga `"use client"` var-helst. Korrekt.
2. **TypeScript strict:** Inga `any`. `noUncheckedIndexedAccess` korrekt hanterad.
3. **Svenska user-facing strings:** All copy i `(marketing)/page.tsx` och layout-metadata är svenska.
4. **Token-konsekvens:** Inga `text-zinc-*`/`text-gray-*`/`bg-slate-*`/oklch i komponenter. Hex-värden lever enbart i `globals.css`.
5. **Onödig komplexitet:** Minimal scaffold, inga premature abstractions.
6. **Anti-patterns §5.2:** Inga `localStorage`, `document.getElementById`, `console.log`, emoji eller utropstecken.

---

## Minor (inget krav på åtgärd innan merge)

1. **Dead `dark:`-varianter** i `button.tsx` och `input.tsx` — inerta utan `.dark`-klass, men förorenar diff-läsbarhet. Rensas när dark-mode-ADR skrivs.
2. **`page.tsx` — "Ghost"** är engelska/teknisk term i demo. OK för internt scaffold, byt vid extern exponering.
3. **`tsconfig.json` `target: "ES2017"`** — lågt för modern browser-baseline. Framtida förbättring.

---

## Bra gjort

- Token-arkitektur ren: `@theme` + `@theme inline` + `:root` med kommentarer vid varje icke-trivialt val
- Reduced-motion globalt
- Global focus-ring via `*:focus-visible`
- Minimal scaffold utan onödiga abstraktioner
- `lang="sv"` i layout — kritiskt för screenreader
- Svensk metadata, civic ton
