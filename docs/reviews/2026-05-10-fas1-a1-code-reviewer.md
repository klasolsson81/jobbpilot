# Code-review: TD-15 — `aria-invalid`-koppling i ResumeContentForm

**Status:** Approve med två minor-noteringar
**Granskat:** 2026-05-10
**Auktoritet:** CLAUDE.md §4 (TS/Next.js), §5.2 (FE anti-patterns), §9.2 (scope-disciplin)
**Scope:** Frontend — `web/jobbpilot-web/src/components/resumes/resume-content-form.tsx` (Fas 1 Block A1)

## Sammanfattning

A1 löser TD-15:s a11y-gap: fältet vars Zod-issue path triggade felet får
nu `aria-invalid="true"` + `aria-describedby` som pekar mot felmeddelandet
(WCAG 2.1 AA SC 3.3.1, 4.1.3). Implementationen är minimal, ren och håller
sig inom scope. Inga Critical eller Major-fynd. Två Minor + en Nit nedan.

## Fynd

### Minor 1 — `FieldError` lokal-typ är OK för nuvarande scope

`type FieldError = { path: string | null; message: string }` är deklarerad
i samma fil där den används. Per §4.4 är detta acceptabelt — typen är
endast en intern state-shape för en specifik komponent och har inget
kors-fil-beroende. Att lyfta till `resumes.types.ts` skulle vara
överingenjörering tills minst en till komponent behöver samma form.
Behåll lokalt. Vid framtida återanvändning (t.ex. Application-formuläret)
— extrahera då.

### Minor 2 — `fieldA11y` återskapas per render

Helpern är deklarerad i komponentkroppen och får ny referens vid varje
render. Det är harmlöst eftersom den spreadar primitiva attribut, inte
används som dependency, och spreadat objekt själv är lokalt per call.
Att memoisera (`useCallback`) skulle vara overhead utan vinst. Behåll.

### Nit 1 — Edge case 6b: Zod-path utan matchande fält

Om `resumeContentSchema.skills` skulle få en parent-level `.refine()` som
emitterar `path: ["skills"]` skulle ingen `Input` ha `path === "skills"`
och inget fält flaggas — endast top-level `<p role="alert">` visar
meddelandet. Beteendet idag: graceful degradation, inte regression mot
pre-A1-state. Ingen åtgärd krävs i A1-scope. Notera dock att om sådan
parent-refine läggs till senare (Fas 1 schema-utvidgning) bör en
`section`-nivå-`aria-describedby` övervägas. Lämpligen TD-uppslag, inte
A1-blocker.

### Edge case 6a — `path: null` (server-action-fel) ger ingen fält-flagga

Korrekt. `result.error` från server-action är inte fält-bundet (typiskt
"network failure", "konflikt", "obehörig"). Top-level `role="alert"`
annonserar fortfarande felet. Beteendet är medvetet och rätt.

### Type-safety — `as const` vid spread

```ts
return serverError?.path === path
  ? ({ "aria-invalid": true, "aria-describedby": ERROR_ID } as const)
  : {};
```

Returtypen smalnar till `{ readonly "aria-invalid": true; readonly
"aria-describedby": "content-form-error" } | {}`. När den spreadas på
`<Input>` ger TS rätt narrowing — `aria-invalid` blir `true | undefined`
i resulterande JSX, vilket React DOM accepterar utan koercion. `as const`
behövs här för att TS inte ska wide:a `true → boolean` och därmed bryta
HTML attribute-type. Korrekt mönster. Klar.

### §5.2 anti-patterns — alla rena

- Inget `any` ✓
- Ingen `localStorage` ✓
- Inget `console.log` ✓
- Ingen DOM-manipulation ✓
- Ingen emoji eller utropstecken i copy ✓
- Felmeddelandet behåller civic-utility-ton ("Ogiltiga uppgifter.") ✓

## Approve-status

**Approved** för commit. TD-15 stängningskandidat. Vitest 65/65 grön
+ TS-check ren räcker som gate för en a11y-only-ändring utan ny
forretnings-logik.

## Föreslagna in-block-fixar

Inga. Behåll PR som den är. Vid stängning av TD-15 i `docs/tech-debt.md`
— notera att den strukturella alternativ-fixen (display-shape →
wire-shape via `zodResolver`) inte valdes; nuvarande lösning är path-
matchning som är proportionell mot Fas 1-scope.
