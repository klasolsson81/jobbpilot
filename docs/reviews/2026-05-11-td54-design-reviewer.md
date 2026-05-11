---
review: design-reviewer
datum: 2026-05-11
commit: 8cfbde4
td: TD-54
status: APPROVE
blockers: 0
major: 0
minor: 4
nits: 2
---

# Design-review: TD-54 a11y-kontrast-fix (commit 8cfbde4)

**Status:** APPROVE
**Granskat:** 2026-05-11
**Reviewer:** design-reviewer
**Auktoritet:** DESIGN.md §2 (tokens), §9 (a11y),
`.claude/skills/jobbpilot-design-a11y/SKILL.md` §4 (contrast),
`.claude/skills/jobbpilot-design-tokens/references/contrast-table.md`
**Scope:** 8 frontend-filer. CSS-class-substitueringar
(`text-text-tertiary` → `text-text-secondary`) på funktionell text.
Ingen ny komponent, ingen ny layout, ingen ny dependency.

---

## Sammanfattning

10 ändringar i 8 filer. Alla är kontrast-uplifts från
`text-text-tertiary` (#8A8A85, 3.7:1 på surface-secondary — **FAILS AA**)
till `text-text-secondary` (#5A5A5A, 7.2:1 på surface-secondary — **AA**).
Dekorativa separatorer (5 träffar) korrekt utelämnade. Inga
contrast-regressions, inga civic-utility-aestetik-brott, inga nya
hårdkodade värden.

| Krav | Resultat |
|---|---|
| WCAG 2.1 AA 1.4.3 | ✓ Uppfyllt netto (alla 10 träffar uppgraderade) |
| Token-disciplin | ✓ Endast semantiska tokens används |
| Civic-utility-aestetik | ✓ Bibehållen (ingen gradient/glow/emoji/shadow) |
| Mappingsregel-konsistens | ✓ Funktionell → secondary, dekorativ → tertiary |
| Visuell hierarki | ✓ text-primary (16.5:1) → text-secondary (7.2:1), 2.3x differential |
| Server Components-pattern | ✓ Bibehållet (ingen "use client" introducerad) |
| Tester | ✓ Vitest 150/150 oförändrade |

---

## Blockers

Inga.

---

## Major

Inga.

---

## Minor

### M1. Kontrast-värde i commit-meddelandet felcitat

- **Plats:** Commit-message rad 4, "text-text-secondary (#5A5A5A) ≈ 6.0:1"
- **Faktiskt värde:** 7.2:1 per `contrast-table.md` rad 41
- **Påverkan:** Ingen funktionell konsekvens — verkligt värde är ännu
  bättre än det citerade. Men contrast-table är source of truth och
  commit-message bör matcha den.
- **Föreslagen åtgärd:** Ingen (commit redan gjord, 7.2:1 > 4.5:1 oavsett).
  Nästa fix-pass: citera contrast-table.md direkt.

### M2. Audit-log "system"-actor — typografisk distinktion saknas

- **Plats:** `audit-log-table.tsx:63`
- **Observation:** Real user-IDs renderar som `font-mono text-xs` (UUID-fragment),
  "system"-fallback också `font-mono text-xs` — bara färg-differential
  (#5A5A5A vs #1A1A1A, 2.3x).
- **Bedömning:** Färg-differentialet räcker för "annan kategori", semantisk
  skillnad ("system" som ord vs UUID) bär huvudinformationen. OK för nuvarande
  scope.
- **Föreslagen åtgärd:** Ingen för denna PR. Eventuell framtida iteration:
  italic eller chip-treatment. Scope-creep — inte värt egen TD.

### M3. Pagination disabled-state — sighted-user-affordance

- **Plats:** `audit-log-pagination.tsx:42-48, 58-64`
- **Verifikation:** Screen reader täcks av `aria-disabled="true"`. Sighted
  user får hierarki via text-color-differential + surface-differential
  + frånvaro av hover-effekt.
- **Bedömning:** Hierarkin bevarad. Optionellt: `cursor-not-allowed`
  förstärker affordansen utan att bryta civic-utility.
- **Föreslagen åtgärd:** Fix N1 nedan (in-block, 4h-regel).

### M4. ` · ` separator i audit-log-table

- **Plats:** `audit-log-table.tsx:69`
- **Observation:** Separator på `text-text-tertiary` (#8A8A85) på
  `surface-primary` (#FFFFFF) = 3.9:1 — Borderline AA för body text,
  men separator är dekorativ punkt (3:1 UI-komponent-krav uppfylls).
- **Bedömning:** ✓ Korrekt klassificering, ingen åtgärd.

---

## Nits

### N1. `cursor-not-allowed` på pagination disabled-span (5-min in-block-fix)

- **Plats:** `audit-log-pagination.tsx:42-48, 58-64`
- **Föreslaget:** Lägg till `cursor-not-allowed` på disabled-span.
- **Status:** Fixat in-block i follow-up-commit per 4h-regel.

### N2. Em-dash placeholder i userAgent-cell

- **Plats:** `audit-log-table.tsx:78-79`
- **Observation:** Parent `<td>` har `text-text-secondary`. Em-dash
  `text-text-tertiary` är bara 1.5x lighter än parent — svag visuell
  distinktion.
- **Bedömning:** Em-dash så kort + dekorativ att det räcker. Ingen åtgärd.

---

## Bra gjort

- Mappingsregeln applicerad konsekvent över alla 10 träffar
- Decorative-undantaget korrekt applicerat på 5 kvarvarande tertiary-träffar
- `aria-disabled="true"` redan på plats på pagination — kontrast-fixen
  bryter inte semantik
- Server Components-pattern bibehållet
- Token-disciplin: inga hex-värden i className, inga Tailwind-defaults
- Civic-utility-ton bevarad: ingen emoji, inga utropstecken, ingen gradient,
  ingen rounded-xl, inga shadows
- Commit-message-disciplin: scope `web`, type `fix`, ärendenummer TD-54
  citerat, WCAG-kriterium specifikt namngivet (1.4.3), mappingsregeln
  dokumenterad i body

---

## Sammanfattning av åtgärder

- **Blockers att fixa innan merge:** Inga.
- **In-block-fixar tillämpade (4h-regel):** N1 (`cursor-not-allowed` på
  pagination disabled-span).
- **Inga åtgärder krävs för:** M1, M2, M3 (samma som N1), M4, N2.

TD-54 är **stängd och korrekt levererad**.
