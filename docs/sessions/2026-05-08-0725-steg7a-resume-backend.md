---
session: "2026-05-08 — STEG 7a: Resume-aggregat backend (domain + EF + CQRS + API)"
datum: 2026-05-08
slug: steg7a-resume-backend
status: KLAR
commits:
  - sha: 38189fe
    msg: 'chore(claude): använd "opus"-alias för model i settings.json'
  - sha: 46a0ad5
    msg: "feat(resumes): STEG 7a — Resume-aggregat backend (domain + EF + CQRS + API)"
---

## Mål för sessionen

STEG 7a — backend-implementation av Resume-aggregatet per BUILD.md §18 Fas 1
("Du kan skapa CV manuellt"). Ingen plan-design via webb-Claude — CC drog
plan-förslag direkt baserat på BUILD.md §5.3, §5.6, §6.2 och §7. Klas
godkände Alt A (Resume) över Alt B (Hangfire/Ghosted).

## Vad som genomfördes

### Plan-design utan webb-Claude

CC läste BUILD.md §5.1–5.6 + §18 + §7 och föreslog konkret STEG 7a:

- Avgränsning: bara Master-version (Tailored = Fas 4)
- Skip:a PDF/DOCX-uppladdning, AI-tailoring, export — allt Fas 4
- Vägval: mutera Master direkt vid edits (KISS) → ADR 0021 dokumenterar
- API: 7 endpoints under `/api/v1/resumes`
- Två STEG: 7a backend, 7b frontend

Klas: "kör din rek". Plan-design via chat ersatte webb-Claude-rundan.

### Domain-validering (dotnet-architect)

Innan kod skrevs invokerades `dotnet-architect` för design-validering. Stark
feedback i 6 specifika frågor + 11 övriga fynd. Justeringar som applicerades
innan implementation:

- `IReadOnlyList<T>` i record-VO bryter value-equality + JSONB-deserialisering
  → explicit constructor med default `[]`, dokumentera reference-equality
- `Resume.Create` kräver `fullName` för giltig initial Master
- `DeleteVersion(bool isReferencedByOpenApplication)` med separata felmeddelanden
  för Master-fall vs användnings-fall
- Domain event-suffix `DomainEvent` (inte `Event`) + `ResumeDeletedDomainEvent`
- Skill.YearsExperience valideras 0–70 i `ResumeContent`-validering
- Resume.Name validering matchar JobSeeker.DisplayName (max 200, trim)
- AI-fält (tailoredForJobAdId etc) helt utelämnade tills Fas 4

### Domain-implementation

`src/JobbPilot.Domain/Resumes/`:

- `Resume.cs` (AR) — invarianter, factory, mutation-metoder
- `ResumeVersion.cs` (entity) — internal factory, Content-mutation
- `ResumeContent.cs` (record VO) — explicit ctor, `Empty(fullName)`-factory, equality-doc
- `PersonalInfo.cs`, `Experience.cs`, `Education.cs`, `Skill.cs` (sub-VOs)
- `ResumeVersionKind.cs` (SmartEnum: Master/Tailored)
- `ResumeId.cs`, `ResumeVersionId.cs` (record struct + `New()`)
- 5 domain events: Created, VersionCreated, ContentUpdated, VersionDeleted, Deleted

CA1716 triggade på `Resume`-klassen (VB-keyword) — type-level `SuppressMessage`
istället för globalt undantag.

### EF-konfiguration + migration (db-migration-writer)

Avvek från `OwnsOne+ToJson`-mönstret i förlagen `JobSeekerConfiguration` —
EF Core 10 hanterar inte `init`-only `IReadOnlyList<T>` med constructor-args
tillförlitligt vid materialisering. Lösning: `HasConversion<ResumeContent, string>`
med `System.Text.Json` + JSON-baserad ValueComparer. Kolumntypen `jsonb` bevaras.

Migration `20260508014955_AddResumeAggregate`:
- Tabell `resumes` (id, job_seeker_id, name, timestamps, xmin)
- Tabell `resume_versions` (id, kind, content jsonb, resume_id FK CASCADE, timestamps)
- Index på `job_seeker_id` och `resume_id`
- Global query filter `DeletedAt == null`

### Application-lager

5 commands + 2 queries med `Mediator.SourceGenerator`-pattern:

- `CreateResumeCommand` (returnerar Guid)
- `RenameResumeCommand`
- `UpdateMasterContentCommand` (tar `ResumeContentDto`, mappar till domain)
- `DeleteResumeCommand`
- `DeleteResumeVersionCommand` (med `isReferencedByOpenApplication = false`-TODO för Fas 4)
- `GetResumesQuery` (paginerad)
- `GetResumeByIdQuery` (med versions)

Cross-tenant-skydd via `r.JobSeekerId == jobSeekerId`-filter i alla handlers.
DbSet&lt;Resume&gt; tillagt i `IAppDbContext`.

### API-endpoints

7 endpoints under `/api/v1/resumes` registrerade i `Program.cs`:

- GET `/`, GET `/{id}`, POST `/`, PATCH `/{id}`, PUT `/{id}/master`,
  DELETE `/{id}`, DELETE `/{id}/versions/{versionId}`
- Alla `RequireAuthorization()`
- Returnerar Problem Details vid fel

### Tester (test-writer × 2)

Domain unit tests (39 nya) — täcker alla validerings-grenar, mutations och events.
Handler unit tests (36 nya) — täcker auth, cross-tenant, NotFound, domain-fel-propagering.
Integration tests (23 nya) — komplett livscykel mot Postgres via Testcontainers.

`dotnet test` på solution-nivå är trasigt (xunit.v3.mtp-v2 platform-issue).
Workaround: kör test-exen direkt under `tests/*/bin/Debug/net10.0/`.

**Total testtäckning:** 378 tester, alla gröna (134 Domain + 146 Application + 6 Architecture + 92 Integration).

### Reviews (code-reviewer + security-auditor)

**code-reviewer:** 0 Blocker, 1 Major (M1: dead-code auth-check i CreateResumeCommandHandler), 11 Minor (N1–N11). Major fixad — använder `throw UnauthorizedException` konsekvent.

**security-auditor:** 0 Critical, 1 Major (M1: saknad TODO(GDPR) på `resume_versions.content`), 3 Minor. Major fixad — TODO-kommentar tillagd matching cover_letter-kolumnen.

Övriga åtgärdade fynd:
- N7: Round-trip integration-test för full `ResumeContent` (Experiences + Educations + Skills)
- N9: Vilseledande `Handle_VersionCountIncludesOnlyNonDeletedVersions`-test borttaget (InMemory applicerar inte query filter på collections)
- Mi2: `JobSeekerId` borttaget från `ResumeDetailDto` (data minimization)

### Dokumentation

- ADR 0021 — Master-version-strategi (mutera direkt i Fas 1)
- TD-13 — Encryption av PII-kolumner i Fas 2 (paritet med cover_letter)
- TD-14 — DeleteResumeVersion VersionInUse-check aktiveras i Fas 4

### Migration applicerad mot dev-DB

`appsettings.Local.json` hade fel lösenord (`lokalutveckling` istället för `.env`-värdet `c0a9e3b838afc9584511ba4d53defc1c`). Uppdaterat lokalt (gitignorerad fil).
`dotnet ef` plockar inte upp `appsettings.Local.json` — workaround: `export ConnectionStrings__Postgres=...` innan migration-kommandot.

Tabeller `resumes` + `resume_versions` verifierade i postgres-dev (port 5435) via `\dt resume*`.

## Tekniska beslut

- **Master-mutation över versionering** för Fas 1 (KISS, ADR 0021). Versionering kommer naturligt med Tailored i Fas 4.
- **JSONB via HasConversion+System.Text.Json** istället för OwnsOne+ToJson p.g.a. EF Core 10-inkompatibilitet med `init`-only `IReadOnlyList<T>`.
- **`isReferencedByOpenApplication = false` hårdkodat** med TODO för Fas 4. Application-aggregatet har ännu ingen `ResumeVersionId`-referens, så funktionellt risk-zero.
- **CA1716 type-level suppression** på `Resume`-klassen istället för globalt undantag.
- **Plan-design via CC istället för webb-Claude** för STEG 7 — etablerade mönster från STEG 5+6 räcker som granskningsspärr när scope är upprepningsmönster.

## Nästa session

STEG 7b — frontend för CV-hantering på `/cv` (pipeline-tabell, formulär, detaljvy + redigering med field arrays för Experiences/Educations/Skills).

Förväntad HEAD efter 7a: `46a0ad5`
