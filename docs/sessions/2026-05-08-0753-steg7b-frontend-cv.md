---
session: "2026-05-08 — STEG 7b: Frontend för manuell CV-hantering (/cv)"
datum: 2026-05-08
slug: steg7b-frontend-cv
status: KLAR
commits:
  - sha: a880671
    msg: "feat(web): STEG 7b — frontend för manuell CV-hantering (/cv)"
---

## Mål för sessionen

STEG 7b — frontend-spegel av STEG 7a-backend. Manuell CV-redigering på `/cv`
med pipeline-mönstret från STEG 6 (`/ansokningar`).

## Vad som genomfördes

### Delegering till nextjs-ui-engineer

Hela frontend-implementationen delegerades till `nextjs-ui-engineer` med thorough brief:

- Backend-endpoints, DTOs, valideringsregler
- Etablerade mönster från STEG 6 (paths till förlagor)
- Fil-struktur som skulle skapas
- Sid-detaljer, design-krav, validerings-strategi
- 5 kvalitetskrav som måste vara gröna

Agenten levererade:

- 14 nya filer (types, API-klient, schemas, actions, content-utils, 4 komponenter, 3 sidor, 1 E2E-spec)
- 2 modifierade (middleware + (app)/layout)
- 37 Vitest-tester + 6 Playwright E2E-tester
- pnpm tsc, lint, test, build, playwright — alla gröna

### Field arrays-implementation

`ResumeContentForm` använder React Hook Form `useFieldArray` för Experiences,
Educations och Skills. Designval: **manuell `safeParse` i onSubmit istället
för `zodResolver`** p.g.a. typkonflikt mellan formulärlagrets string
(number-input) och schemats `number | null`-output. RHF hanterar field-state
och fält-arrays, Zod körs en gång på submit via `toRawPayload()` → `safeParse()`.

Tradeoff: ingen per-field aria-invalid-binding. Dokumenterat som TD-15.

### Designmönster

- `role="alert"` för error vs `role="status"` för success
- `<fieldset>` + `<legend className="sr-only">` på field-array-items
- `aria-label="Ta bort erfarenhet 1"` på remove-knappar (indexerat för screenreader-tydlighet)
- Destruktiv DELETE → bekräftelsedialog via shadcn `Dialog` (motsv. `transition-form` i STEG 6)
- Loading-state: "Sparar..." ersätter knapptext, `disabled={isPending}`
- Civic-utility-tonen genomgående: ingen emoji, inga utropstecken, "du"-tilltal

### Reviews

**design-reviewer:** 0 Blocker, 1 Major (M1 path-baserad fält-error-koppling), 4 Minor (Mi4 knapp-storlek). Mi4 fixad. M1 dokumenterad som TD-15 — reviewer själv föreslog detta som rimlig kompromiss eftersom RHF-integrerad error-binding kräver lösning på zodResolver-typkonflikten.

**code-reviewer:** Approved. 0 Blocker, 0 Major, 6 Minor (alla informationella, ingen åtgärd krävd).

### Tech-debt registrerat

- TD-15 — Resume-formulär: koppla Zod-issue path till `aria-invalid` per fält (a11y-pass)

## Tekniska beslut

- **RHF + manuell `safeParse`** istället för `zodResolver` (typkonflikt löst genom uppdelning av form-shape och wire-shape vid submit)
- **`yearsExperience` som string i form-state**, `parseInt` vid submit (HTML number-input ger string)
- **HTML `min`/`max` borttaget** på number-input — Zod är single source of truth (native validation skulle blockera onSubmit)
- **`type="email"`, `type="tel"`, `type="date"`** för semantiskt korrekta mobil-tangentbord
- **`cache: "no-store"`** på alla list/detail-fetches — säkerställer att sessionsdata aldrig delas över användare via Next.js fetch-cache
- **Tailored-versioner exponeras inte i UI ännu** — kommer i Fas 4

## Nästa session

STEG 7 är klart. Nästa STEG behöver beslutas — kandidater per BUILD.md §18 Fas 1:

- **Hangfire-setup + GhostedDetectionJob** (Fas 1 polish som blockades i favör för CV)
- **Audit log-infrastruktur** (BUILD.md §5.5 — `IApplicationAuditLogger` mot `application_audit_log`-tabell, kopplar till TD-9)
- **Steg mot Fas 0-stängning** (deploy till dev.jobbpilot.se, GitHub Actions CI/CD verifierad — BUILD.md §18)

Förväntad HEAD efter 7b: `a880671`

Diskutera nästa STEG med Klas innan kod skrivs.
