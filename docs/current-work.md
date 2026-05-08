# Current work — JobbPilot

**Status:** STEG 7 KLAR (a + b). Fas 1-milstolpe ("Du kan skapa CV manuellt") uppfylld. Nästa: STEG 8 — kräver beslut (kandidater i steg-tracker.md §6).
**Senast uppdaterad:** 2026-05-08
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu

**STEG 7 klar.** Hela CV-stacken (backend + frontend) implementerad och pushad.

### STEG 7a — Backend (Resume-aggregat)

- Domain: `Resume` AR + `ResumeVersion` entity + `ResumeContent` VO + sub-VOs (PersonalInfo, Experience, Education, Skill) + 5 domain events
- EF Core: konfiguration med JSONB via `HasConversion` + System.Text.Json (avvek från `OwnsOne+ToJson` p.g.a. EF Core 10-inkompatibilitet med `init`-only `IReadOnlyList<T>`)
- Migration `AddResumeAggregate` skapad och applicerad mot dev-DB (port 5435)
- Application: 5 commands (Create, Rename, UpdateMasterContent, Delete, DeleteVersion) + 2 queries (GetResumes, GetResumeById) + DbSet i IAppDbContext
- API: 7 endpoints under `/api/v1/resumes` registrerade
- Tester: +98 (39 domain + 36 handler + 23 integration) — alla gröna

### STEG 7b — Frontend (`/cv`)

- Types + server-only API-klient + Zod v4-schemas + 5 Server Actions
- Komponenter: `ResumeCard`, `ResumeContentForm` (RHF + `useFieldArray` för Experiences/Educations/Skills), `RenameResumeForm`, `DeleteResumeDialog`
- Pages: `/cv` (lista), `/cv/ny` (skapa), `/cv/[id]` (detalj + redigera Master)
- Nav-integration i `(app)/layout.tsx` + `/cv` i middleware PROTECTED_PREFIXES
- Tester: +43 (37 Vitest + 6 Playwright E2E) — alla gröna

### Reviews genomförda

- **dotnet-architect** (innan kod): design-godkänd, 6 specifika feedbacks applicerade
- **code-reviewer 7a:** 1 Major (M1 dead-code auth) + 11 Minor — M1, N7, N9, Mi2 fixade
- **security-auditor 7a:** 1 Major (TODO-paritet på content-kolumn) — fixat
- **design-reviewer 7b:** 1 Major (M1 aria-invalid-koppling → TD-15) + 4 Minor (Mi4 fixad)
- **code-reviewer 7b:** Approved, 0 Major, 6 Minor (informationella)

### Viktiga tekniska beslut

- **Master-mutation över versionering** för Fas 1 — ADR 0021 dokumenterar
- **JSONB via HasConversion+System.Text.Json** istället för OwnsOne+ToJson
- **`isReferencedByOpenApplication = false` hårdkodat** i DeleteResumeVersion-handler med TODO för Fas 4 (Application-aggregatet har ännu ingen `ResumeVersionId`-referens)
- **CA1716 type-level suppression** på `Resume`-klassen (VB-keyword men domänspråk)
- **RHF + manuell `safeParse`** i onSubmit istället för `zodResolver` (typkonflikt mellan form-shape och wire-shape — TD-15)
- **Plan-design via CC istället för webb-Claude** för STEG 7 (etablerade mönster från STEG 5+6 räcker som granskningsspärr)

## Senaste commits

| SHA | Beskrivning |
|-----|-------------|
| a880671 | feat(web): STEG 7b — frontend för manuell CV-hantering (/cv) |
| 46a0ad5 | feat(resumes): STEG 7a — Resume-aggregat backend (domain + EF + CQRS + API) |
| 38189fe | chore(claude): använd "opus"-alias för model i settings.json |
| 4ddc083 | docs: discovery STEG 5 — bekräftat klar, steg-tracker uppdaterad |
| 135837d | docs(sessions): uppdatera SHA i session-logg STEG 6 |

## Open follow-ups

Tre nya tech-debt-poster i `docs/tech-debt.md`:

- **TD-13** — Encryption av PII-kolumner i Fas 2 (paritet med cover_letter)
- **TD-14** — DeleteResumeVersion VersionInUse-check aktiveras i Fas 4
- **TD-15** — Resume-formulär: koppla Zod-issue path till `aria-invalid` per fält (a11y-pass)

## Tester totalt

- **Backend:** 378 (134 Domain + 146 Application + 6 Architecture + 92 Integration)
- **Frontend:** 65 Vitest + 19 Playwright E2E

## När nästa session startar

1. Kör `git log --oneline -10` — verifiera HEAD = `a880671` eller senare docs-commits
2. Verifiera backend-tester: kör test-exen direkt under `tests/*/bin/Debug/net10.0/` (`dotnet test` på solution-nivå är trasigt — xunit.v3.mtp-v2 platform-issue)
3. Verifiera frontend: `cd web/jobbpilot-web && pnpm test`
4. Verifiera API kan starta: `cd src/JobbPilot.Api && dotnet run` (kräver postgres-dev på port 5435)
5. Läs `docs/steg-tracker.md` §6 för STEG 8-kandidater
6. Läs senaste session-loggar (STEG 7a + 7b) för detaljer

## Kända begränsningar / quirks

- **postgres-dev** på port **5435** (inte 5432) — `appsettings.Local.json` måste ha rätt port + lösenord (`c0a9e3b838afc9584511ba4d53defc1c` från `.env`)
- **`dotnet ef`** plockar inte upp `appsettings.Local.json` — använd `export ConnectionStrings__Postgres=...` innan migration-kommandon
- **`dotnet test`** på solution-nivå returnerar "Zero tests ran" (xunit.v3.mtp-v2 issue) — kör test-exen direkt
- **API kräver `ASPNETCORE_ENVIRONMENT=Development`** för att läsa Redis-connstring från appsettings.Development.json
- **Middleware-deprecation-varning** i Next.js: `The "middleware" file convention is deprecated. Please use "proxy" instead.` — kvar från STEG 6
