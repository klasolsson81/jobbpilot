# Design-review: Fas 1 Block A (sub-block A1) — TD-15 a11y-fix

**Granskat:** 2026-05-10
**Auktoritet:** WCAG 2.1 AA (SC 3.3.1 Error Identification, SC 4.1.3 Status Messages, SC 2.4.3 Focus Order), `jobbpilot-design-a11y` §5 + §10, DESIGN.md §9
**Skill konsulterad:** `jobbpilot-design-a11y` (triggers: aria-invalid, aria-describedby, form, error)
**Fil:** `web/jobbpilot-web/src/components/resumes/resume-content-form.tsx`

---

## Sammanfattning

Variant 1-fixen (helper `fieldA11y(path)` + `FieldError`-typ + `id={ERROR_ID}`
på error-`<p>`) **uppfyller WCAG 2.1 AA SC 3.3.1 + SC 4.1.3** för det
common-case som spec:n täcker. Skärmläsare får nu programmatisk koppling
mellan fältet som triggade felet och själva felmeddelandet via
`aria-describedby` — det är minimumkravet som saknades.

A11y-paritet uppnådd. Fixen är civic-utility-konsekvent (inga visuella
nymodigheter, inga tokens berörda, inga emojis/utropstecken). Två gap kvarstår
men är inte WCAG-blockerande för Fas 1.

---

## Fynd

### Major M1 — Fokushantering saknas (skill `jobbpilot-design-a11y` §10, punkt 4)

Skill-spec:n säger: *"On submit failure: focus moves to first error field"* och
listar fokus till första fel-fältet som ett av fyra a11y-krav vid
formulärsubmission-fel. Implementationen flaggar fältet med `aria-invalid`
men flyttar **inte** fokus dit — användare med skärmläsare hör att en
`role="alert"` inträffat men måste sedan tabba sig nedåt för att hitta
fältet.

WCAG-bedömning: SC 3.3.1 är tekniskt uppfyllt (felet är identifierat och
kopplat till fältet). SC 2.4.3 (Focus Order) bryts inte direkt eftersom inget
fokus har flyttats fel. **Det är dock under JobbPilots egen a11y-skill-spec.**

För ett 20+-fälts-formulär där felet kan ligga i fält 17 är detta mer än
kosmetiskt. Den blir blocker när vi har en konkret real-world användare som
kör skärmläsare; för Fas 1 dev kan den medvetet skjutas till TD med spårning.

**Föreslagen åtgärd (in-block):** lägg `id`-attribut byggt från path
(`document.getElementById(\`field-\${path}\`)?.focus()`) eller använd ref-map
och anropa `.focus()` i `onSubmit` när `setServerError` sätts. Behöver inte
strukturell refactor — 10 raders tillägg.

### Minor m1 — Strikt path-equality missar parent-paths

Helpern använder `serverError?.path === path` (strikt likhet). Schemat i
`resume-schemas.ts` använder `.refine(... path: ["endDate"])` på array-element-
nivå, vilket genererar paths som `experiences.0.endDate` — barn-path. **Inga
av nuvarande refines lägger fel på array-parent** (`experiences.0` utan
fält-suffix), så strikt match räcker för dagens schema.

Risk uppstår först när framtida `.refine()` på `z.object()` lämnar path tomt
eller pekar på array-rot. Då hamnar felet på toppnivå-`<p>` med "(fält:
experiences.0)" som textuell breadcrumb men inget aria-invalid-flaggat fält.

**Bedömning:** acceptabelt för Fas 1. Strikt match är förutsägbart och
matchar schemats nuvarande output exakt. Prefix-match riskerar overshoot
(om `experiences.0` flaggas, ska *alla* fält i exp 0 få aria-invalid?
otydligt UX). Behåll strikt; lägg till regression-test som bevakar att inga
nya refines kommer in på parent-path utan barn-path.

### Minor m2 — Single-error-display

Endast `parsed.error.issues[0]` visas. För Fas 1 är detta acceptabelt och
**vanlig praxis** för enklare formulär. Skill-spec:n §10 visar dock ett
"error summary"-mönster (lista alla fel med ankarlänkar) som skalar bättre
för 20+-fälts-formulär. Avvägningen: error-summary kräver multi-path-state
och anchorlinkar — mer kod, mer testyta.

**Bedömning:** bevara nuvarande beteende i Fas 1. Lägg till TD-rad om vi får
faktisk användarsignal att flera fel samtidigt orsakar friktion.

### Nit n1 — Redundant "(fält: ...)"-suffix

Felmeddelandets text innehåller fortfarande `(fält: experiences.0.endDate)`
som textuell breadcrumb. Nu när `aria-describedby` programmatiskt kopplar
felet till fältet, är denna hint mest användbar för seende sighted-användare.
Path är dock i kod-syntax (`experiences.0.endDate`) snarare än civic-svenska
("Erfarenhet 1 → Slutdatum"). Inkonsekvent med övrig svensk copy-ton.

**Föreslagen åtgärd (in-block):** mappa kända path-prefix till svenska
etiketter, eller — enklare — släpp suffixet helt nu när `aria-describedby`
gör det maskinläsbart. `role="alert"` läser ändå upp meddelandet
("Slutdatum kan inte vara före startdatum.") och fokus-på-fält (M1) gör
suffixet ännu mer redundant.

---

## Civic-utility-paritet

Inga avvikelser. Fixen är osynlig visuellt — inga tokens berörda, inga nya
färger, inga rundningar, inga skuggor. `text-danger-600` (befintlig token)
oförändrad. Svensk copy-ton bevarad. Inga emojis eller utropstecken.

---

## Approve-status

**APPROVE-WITH-FIXES**

WCAG 2.1 AA SC 3.3.1 + SC 4.1.3 är uppfyllda. Fixen blockerar inte merge.
M1 (fokushantering) bör addresseras innan Fas 1 prod-deploy — antingen i
denna PR (rekommenderat, 10 rader) eller via ny TD-rad spårad mot Fas 1
a11y-pass-completion. Övriga fynd är minor/nit och kan vänta.

---

## Föreslagna in-block-fixar (prioritetsordning)

1. **M1 (rekommenderat nu):** lägg fokus-flytt på första fel-fält i
   `onSubmit`-fail-grenen. Använd path-baserad ID-konvention
   (t.ex. `pi-fullName` finns redan; för field-arrays: `exp-0-company`).
   Bygg en path→element-id-mappning eller skifta till ref-map.

2. **n1 (rekommenderat nu):** ta bort `(fält: ${serverError.path})`-suffixet
   i `<p id={ERROR_ID}>` — `aria-describedby` + fokus-flytt (M1) gör det
   redundant. Behåll endast `serverError.message`.

3. **m1 + m2:** registrera som TD-rader om de inte addresseras direkt.
   TD-15 kan stängas när M1+n1 är inne; m1+m2 blir TD-uppföljare med
   tydlig scope ("error-summary för stora formulär", "prefix-match vid
   parent-path-refines").
