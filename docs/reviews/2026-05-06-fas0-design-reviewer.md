# Design-review: Fas 0 frontend scaffold

**Datum:** 2026-05-06
**Granskad av:** design-reviewer agent
**Filer:** `globals.css`, `button.tsx`, `input.tsx`, `card.tsx`
**Slutstatus efter åtgärder:** GODKÄND

---

## Ursprunglig status: BLOCKERAD

Design-reviewer identifierade 2 blockers + 2 majors + 3 minors i den initiala scaffolden. Alla blockers och majors åtgärdades direkt av nextjs-ui-engineer innan STOPP 3-rapporten skickades.

---

## globals.css — åtgärdat

### Ursprungliga problem

**Major (åtgärdad):** `.dark`-blocket från shadcn nova-preset innehöll `oklch(0.488 0.243 264.376)` på `--sidebar-primary` — en mättad indigo/lila-färg som bryter CLAUDE.md §5.2 (indigo-violet-lila förbjudet). Hela `.dark`-blocket borttaget och ersatt med kommentar + TODO.

**Minor (åtgärdad):** `--radius-xl: 8px` öppnade för `rounded-xl` att rendera > civic max 6px. Klampad till `6px` (= `--radius-lg`).

**Minor (noterad, ej blockande):** Kommentar vid `--radius: 0.25rem` var vilseledande. Utökad.

### Status efter åtgärd: PASS

**Bra genomfört:**
- Civic tokens i `@theme` korrekt: `brand-600 = #0B5CAD`, `text-primary = #1A1A1A`, `surface-primary = #FFFFFF`, `border-default = #D8D6D0` — matchar DESIGN.md §3 exakt
- `:root` shadcn-bridge mappar korrekt till civic-värden — `--primary: #0B5CAD`, inga oklch-värden i ljus mode
- Globalt focus-ring (`*:focus-visible`) implementerat
- `prefers-reduced-motion` respekterad
- Inga gradients, `backdrop-blur`, glow, glassmorfism

---

## button.tsx — åtgärdat

### Ursprungliga problem

**Blocker (åtgärdad):** Storlekar `xs`, `sm`, `icon-xs`, `icon-sm` använde `rounded-[min(var(--radius-md),10px)]` och `rounded-[min(var(--radius-md),12px)]` — shadcn nova-arv som fungerade av slump. Bytta till `rounded-md` rakt av. `in-data-[slot=button-group]:rounded-lg`-override borttagen.

**Major (åtgärdad):** Default-varianten hade `[a]:hover:bg-primary/80` (gäller bara `<a>`-element) istället för `hover:bg-primary/90`. Hover-stat saknades på `<button>`-element. Bytt till `hover:bg-primary/90` konsekvent med övriga varianter.

**Minor (noterad):** `dark:`-prefixerade klasser är inerta i Fas 0 (ingen `.dark`-klass aktiv). Återbesöks vid dark-mode-ADR.

### Status efter åtgärd: PASS

**Bra genomfört:**
- Grundklass `rounded-md` (4px) korrekt
- Färger via tokens (`bg-primary`, `bg-secondary`, `bg-muted`, `text-destructive`)
- Inga gradients, glow, shadows > shadow-md
- `aria-invalid`-stöd inbyggt

---

## input.tsx — PASS (inga ändringar krävdes)

**Bra genomfört:**
- `rounded-sm` (2px) korrekt för inputs
- Tokens genomgående (`border-input`, `text-foreground`, `placeholder:text-muted-foreground`)
- Focus-state med synlig fokusring
- `aria-invalid`-styling integrerad
- `text-base` + `md:text-sm` — iOS zoom-prevention-mönster, acceptabelt

---

## card.tsx — åtgärdat

### Ursprungliga problem

**Blocker (åtgärdad):** `ring-1 ring-foreground/10` (opacity-baserad ring) bytt mot `border border-border`. Säkerställer token-konsekvens och civic "djup via border, inte shadow"-princip.

**Minor (åtgärdad):** `bg-muted/50` (opacity-based bakgrund) bytt mot `bg-muted` (solid). Civic-design föredrar solida färger.

**Minor (verifierad):** `rounded-lg` renderar 6px via vår `@theme inline` → `--radius-lg: 6px`. Ligger inom civic-max.

### Status efter åtgärd: PASS

**Bra genomfört:**
- `rounded-lg` (6px max) genomgående — inga `rounded-xl` kvar
- `bg-card`, `text-card-foreground` token-baserat
- Inga gradients, glow, shadow > shadow-md
- `font-heading` på CardTitle (korrekt font-token)

---

## Sammanfattning: alla blockers och majors åtgärdade

| # | Kategori | Beskrivning | Status |
|---|---|---|---|
| 1 | Blocker | Card: `ring-1 ring-foreground/10` → `border border-border` | ✓ Åtgärdad |
| 2 | Blocker | Button xs/sm/icon-xs/icon-sm: `rounded-[min(...)]` → `rounded-md` | ✓ Åtgärdad |
| 3 | Major | globals.css `.dark`: indigo `oklch(0.488 0.243 264.376)` — blocket borttaget | ✓ Åtgärdad |
| 4 | Major | Button default: `[a]:hover:bg-primary/80` → `hover:bg-primary/90` | ✓ Åtgärdad |
| 5 | Minor | globals.css `--radius-xl: 8px` → klampad till 6px | ✓ Åtgärdad |
| 6 | Minor | Card `bg-muted/50` → `bg-muted` | ✓ Åtgärdad |
| 7 | Minor | Button `dark:`-klasser: inerta, återbesöks vid dark-mode-ADR | Noterat |

**Slutsats:** Civic tokens implementerade korrekt. Inga forbidden patterns återstår. Komponenterna följer DESIGN.md radius-spec (max 6px). Godkänt för STOPP 3.
