# Issue: Tailwind 4.2 konfigurationsansats — spec-konflikt

**Datum:** 2026-04-18
**Prioritet:** Måste lösas innan FE-implementation börjar (Fas 1)
**Påverkar:** nextjs-ui-engineer.md, DESIGN.md, BUILD.md

## Konflikten

Två dokument pekar åt olika håll för hur Tailwind 4.2 konfigureras:

### BUILD.md §3 (tech-stack-tabell)
```
| Styling | Tailwind CSS | 4.2 | v4 config i `tailwind.config.ts` |
```
Indikerar: **hybrid-läge** — JS/TS config-fil (Tailwind v3-mönstret,
fortfarande stödat i v4).

### DESIGN.md §12 (token-konfiguration)
Visar token-konfiguration via `@theme`-block i `globals.css`.
Indikerar: **CSS-first** — Tailwind v4:s rekommenderade approach.

### Vad Tailwind 4.2 faktiskt rekommenderar
CSS-first med `@theme`-block i CSS:

```css
@import "tailwindcss";

@theme {
  --font-sans: "Hanken Grotesk", sans-serif;
  --color-primary: oklch(40% 0.15 250);
  --color-background: oklch(98% 0 0);
  /* ... */
}
```

JS config (`tailwind.config.ts`) är fortfarande stödat men är legacy i v4.

## Konsekvens av oklarhet

nextjs-ui-engineer.md refererar `tailwind.config.ts` (BUILD.md-version).
Om projektet faktiskt kör `@theme`-block (DESIGN.md-version):
- Token-klasser (text-h1, bg-background) definieras i globals.css, inte i TS-filen
- nextjs-ui-engineer.md:s "Token-krav"-rad ska referera globals.css istället
- Ingen praktisk runtime-skillnad — designsystemet fungerar samma

## Beslut krävs från Klas

**Välj en approach:**

### Alternativ A: CSS-first (`@theme` i globals.css)
- Rekommenderas av Tailwind-teamet för v4
- Tokens definieras i `web/jobbpilot-web/styles/globals.css`
- Ingen `tailwind.config.ts` behövs (eller minimal config)
- Uppdatering krävs: BUILD.md §3 kolumn 4

### Alternativ B: Hybrid (behåll tailwind.config.ts)
- Bakåtkompatibelt mönster, välbekant från v3
- Tokens definieras i `tailwind.config.ts`
- CSS i globals.css importerar Tailwind via `@import "tailwindcss"`
- Uppdatering krävs: DESIGN.md §12 (om den visar @theme-block)

## Påverkade filer när beslut fattas

- `BUILD.md` §3 — uppdatera Tailwind-rad
- `DESIGN.md` §12 — uppdatera token-konfigurationsexempel
- `nextjs-ui-engineer.md` — "Design token configuration"-sektionen
- Ev. `tailwind.config.ts` eller `styles/globals.css` skapas/uppdateras

## Status

OPEN — väntar på Klas-beslut innan Fas 1 FE-arbete börjar.
