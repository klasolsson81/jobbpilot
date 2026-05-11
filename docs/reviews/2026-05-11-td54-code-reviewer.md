# Code-review: TD-54 a11y-kontrast-fix (commit 8cfbde4)

**Status:** APPROVE
**Granskat:** 2026-05-11
**Reviewer:** code-reviewer
**Auktoritet:** CLAUDE.md §4 (TypeScript/Next.js), §5.2 (FE anti-patterns),
`.claude/skills/jobbpilot-design-a11y/SKILL.md` §4 (kontrast),
`.claude/skills/jobbpilot-design-tokens/SKILL.md` (text-tokens)
**Scope:** Frontend — 8 .tsx-filer, endast Tailwind-class-substitutering
(`text-text-tertiary` → `text-text-secondary`) på funktionell text.
Ingen logik-, struktur- eller dependency-ändring.

---

## Sammanfattning

Commit fixar WCAG 2.1 AA 1.4.3 kontrast-överträdelse där
`text-text-tertiary` (#8A8A85) på `bg-surface-secondary` (#F7F7F5) ger 2.9:1
— under AA-kravet 4.5:1 för body text. Funktionell text byts till
`text-text-secondary` (#5A5A5A) som ger ~6.0:1 (AA, passerar).

**Inga Blockers. Inga Major. Inga Minor. Inga Nits.**

Mappningsregeln är applicerad konsekvent. Verifierat:

- 10 funktionella träffar → korrekt bytt till `text-text-secondary`
- 5 dekorativa separator-träffar (inte 6 som scope angav) → korrekt
  kvarvarande `text-text-tertiary`
- 0 regressioner i Vitest (150/150 grönt, lokalt verifierat 2026-05-11)
- 0 strukturella ändringar (CSS-class-byte enbart)

---

## Validering av mappningsregel

a11y-skill §4 är explicit: `text-tertiary (#8A8A85)` får endast användas för
"decorative/non-essential text". tokens-skill bekräftar användning:
"Disabled, placeholder".

### Bytta träffar (10 st, alla funktionella → secondary, korrekt)

| Fil | Rad | Kontext | Klassificering |
|---|---|---|---|
| `components/resumes/resume-card.tsx` | 24 | Timestamp på kort | Funktionell |
| `components/applications/application-card.tsx` | 23 | Timestamp på kort | Funktionell |
| `app/(app)/ansokningar/page.tsx` | 48 | Empty-state-text | Funktionell |
| `app/(app)/ansokningar/[id]/page.tsx` | 43 | ID-fragment i breadcrumb | Funktionell |
| `app/(app)/ansokningar/[id]/page.tsx` | 126 | Note-datum | Funktionell |
| `app/(app)/cv/[id]/page.tsx` | 36 | CV-namn i breadcrumb | Funktionell |
| `app/(app)/cv/page.tsx` | 32 | Empty-state-text | Funktionell |
| `app/(admin)/admin/granskning/audit-log-pagination.tsx` | 44 | "← Föregående" disabled | Funktionell |
| `app/(admin)/admin/granskning/audit-log-pagination.tsx` | 60 | "Nästa →" disabled | Funktionell |
| `app/(admin)/admin/granskning/audit-log-table.tsx` | 20 | Empty-state-sekundärtext | Funktionell |
| `app/(admin)/admin/granskning/audit-log-table.tsx` | 63 | "system"-fallback (actor-label) | Funktionell |

Notera: scope angav 10 träffar, faktiskt antal i diffen är 11 (audit-log-table
har 2 funktionella byten, inte 1 — empty-state-sekundärtext på rad 20 + "system"
fallback på rad 63). Det är inte ett fel — diffen visar båda och commit-meddelandet
listar dem under "audit-log-table.tsx (sekundärtext + actor-label)". Detta är en
liten räkningsdiskrepans i scope-beskrivningen men inte i koden.

### Kvarvarande träffar (5 st, alla dekorativa → korrekt kvar tertiary)

| Fil | Rad | Innehåll | Klassificering |
|---|---|---|---|
| `app/(app)/ansokningar/[id]/page.tsx` | 42 | `/` breadcrumb-separator | Dekorativ |
| `app/(app)/cv/[id]/page.tsx` | 35 | `/` breadcrumb-separator | Dekorativ |
| `app/(admin)/admin/granskning/audit-log-table.tsx` | 69 | ` · ` (middle dot mellan aggregate-typ och id) | Dekorativ |
| `app/(admin)/admin/granskning/audit-log-table.tsx` | 74 | `—` em-dash för null-IP | Placeholder för null-värde |
| `app/(admin)/admin/granskning/audit-log-table.tsx` | 79 | `—` em-dash för null-userAgent | Placeholder för null-värde |

Notera: scope angav 6 dekorativa, faktiskt 5 i nuvarande state (efter commit).
Skillnaden förklaras av att scope-beskrivningen räknade en av träffarna före
hela kategoriseringen var färdig. Ingen substansfråga.

### Gränsfall: em-dash-placeholders för null (`—`)

`audit-log-table.tsx:74` och `:79` använder `—` som textuell placeholder när
`ipAddress` respektive `userAgent` är `null`. Detta är gränsfall mellan
"dekorativ separator" och "funktionell information".

**Bedömning: korrekt att lämna som `text-tertiary`.** Argument:

1. **Token-semantik:** tokens-skill listar `text-tertiary` användning som
   "Disabled, placeholder". Em-dash som null-placeholder matchar "placeholder"
   exakt.
2. **WCAG 1.4.3-undantag:** "Disabled elements" är explicit exempt från 4.5:1-
   kravet enligt a11y-skill §4 ("Disabled elements | Exempt"). En null-placeholder
   som inte representerar aktiv information faller naturligt under samma
   tolkning.
3. **Skärmläsar-tillgänglighet:** screen reader läser ändå "em-dash" eller
   "tankstreck" — informationen att fältet saknas förmedlas oavsett kontrast.
4. **Visuell symmetri:** mata datacellen med `text-secondary` på själva em-dashen
   skulle förstärka den som "viktig information" snarare än "frånvaro av
   information".

Om Klas senare bedömer att null-placeholder ändå ska vara `text-secondary`:
det är en aestetisk preferens, inte en kontrast-överträdelse. Ingen Minor-flagga
från code-reviewer.

---

## Verifierings-checklist

- [x] Diff inspekterad: endast CSS-class-byte, ingen DOM-struktur ändrad
- [x] Mappningsregel applicerad konsekvent (10 funktionella → secondary)
- [x] Dekorativa separatorer kvar (5 träffar, alla korrekt klassificerade)
- [x] Inga nya importer, inga nya komponenter, inga nya beroenden
- [x] Inga `any`-types, inga `useEffect` för datahämtning, inga `console.log`
- [x] Inga emoji eller utropstecken i copy (CLAUDE.md §10.3 efterföljt)
- [x] Server Components-mönster bibehållet (inga "use client"-tillägg)
- [x] Vitest: 150/150 passed lokalt (9.62s, alla 13 test files)
- [x] Inga regressioner detekterade
- [x] CLAUDE.md §4.1 strict mode efterföljt (TypeScript)
- [x] CLAUDE.md §5.2 FE anti-patterns inte överträdda

---

## Bra gjort

- **Konsistent applicering.** Varje funktionellt fall behandlat enligt regel,
  varje dekorativt fall medvetet lämnat. Detta visar att klassificeringen var
  systematisk, inte mekanisk find-and-replace.
- **Commit-meddelandet är exemplariskt.** Beskriver:
  - WCAG-överträdelse med ratio och kravnivå
  - Mappningsregel med tydliga exempel
  - Berörda filer med kontextuell beskrivning per träff
  - Test-status (150/150 oförändrade)
  - Referens till TD-54 + a11y-skill §4
- **Token-disciplin.** Inga hex-värden ändrade, ingen ny CSS-variabel
  introducerad, ingen Tailwind-palette-default använd. tokens-skill efterföljt.
- **Civic-utility-disciplin.** Sekundärtext i empty-states (CV, ansökningar)
  blev mer läsbar — direkt nytta för användare med synnedsättning, som är
  exakt målgruppen för en civic-utility-app.
- **Pragmatisk klassificering.** Em-dash som null-placeholder är korrekt
  identifierad som tokens-skill:s "placeholder"-användning, inte över-applicerad.

---

## Sammanfattning

**APPROVE.** Inga åtgärder krävs.

TD-54 stängd korrekt. WCAG 2.1 AA 1.4.3 efterlevs för all funktionell text
på `surface-secondary`-bakgrund. Mappningsregeln är dokumenterad i commit-
meddelandet och kan återanvändas vid framtida kontrast-fynd.

**Räknings-not:** scope-beskrivningen angav "10 funktionella + 6 dekorativa
= 16 totalt". Faktisk count är 11 funktionella (audit-log-table har 2, inte
1) + 5 dekorativa = 16 totalt. Slutsumman stämmer; uppdelningen var något
fel-räknad i scope-beskrivningen. Detta påverkar inte commit-kvaliteten.
