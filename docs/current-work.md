# Current work — JobbPilot

**Status:** STEG 5 (Application aggregate, Väg A) — KLAR. Nästa: STEG 6 (frontend för ansökningar — pipeline-vy och formulär).
**Senast uppdaterad:** 2026-05-07
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu

**STEG 5 klar.** All kod committad och pushad (SHA 82af5fa).

**Vad som genomfördes:**

- Application aggregate (Domain): SmartEnum state machine (10 states), FollowUp + ApplicationNote child entities, 5 domain events, xmin-based optimistic concurrency (Npgsql-idiomatic)
- EF Core (Infrastructure): ApplicationConfiguration/FollowUpConfiguration/ApplicationNoteConfiguration, 2 migrations (AddApplicationAggregate + RemoveRowVersionUseXmin)
- CQRS (Application): 5 commands + 3 queries + DTOs + validators. AddFollowUp returns Result<FollowUpId> from domain method (no [^1] hack)
- API: 7 endpoints under /api/v1/applications med RequireAuthorization()
- Tests: 280 totalt (53 nya för Application-lagret: 37 unit + 16 integration), alla gröna
- Security fixes: CoverLetter borttagen från list-DTOs, NotFoundException-meddelanden utan ID-läckage, TODO(GDPR) i ApplicationConfiguration, GetApplicationsQueryValidator med PageSize-gränser
- Tech debt: TD-8 (GetPipeline hard cap .Take(500)), TD-9 (Application audit log saknas, GDPR Art. 5(2))
- Decisions: B1 accepterat (IAppDbContext med DbSet<T> per ADR 0009), B3 dokumenterat (GetPipeline är kanban, inte paginerat), B4 uppskjutet till Fas 1

**Viktiga tekniska beslut:**

- `IsRowVersion()` på `byte[]` fungerar inte med Npgsql/PostgreSQL (ingen auto-generering) → bytte till xmin shadow property (`uint`, ValueGeneratedOnAddOrUpdate, IsConcurrencyToken) som Npgsql 10 detekterar och mappar till PostgreSQL:s xmin-systemkolumn automatiskt
- `DomainApplication` type alias (global using) i Application- och Infrastructure-projekten för att lösa namespace-kollision med `JobbPilot.Application`

## Senaste commits

| SHA | Beskrivning |
|-----|-------------|
| 82af5fa | feat(applications): implementera Application-aggregat med full CQRS-stack (STEG 5) |

## Open follow-ups

Se `docs/tech-debt.md` för aktuella poster (TD-numrering).

## När nästa session startar

1. Kör `git log --oneline -10` — verifiera HEAD = 82af5fa
2. Verifiera `dotnet test` — 280 tester gröna
3. Läs `docs/steg-tracker.md` för långsiktig bana
4. Läs `docs/sessions/2026-05-07-1930-steg5-application-aggregate.md` för STEG 5-detaljer
5. Påbörja STEG 6: frontend för ansökningar (pipeline-vy, formulär, detaljvy)

## Kända begränsningar

Se **ADR 0006** för Claude Code-hooks-begränsningar.

**DesignTimeDbContextFactory** använder hårdkodade `postgres/postgres`-credentials för `migrations add`. Ej runtime-problem — bara design-time verktyg.

**guard-spec-files.sh** kontrollerar sentinel-fil i `.claude/spec-edit-approved` istället för `CLAUDE_USER_PROMPT` (som inte sätts i Agent SDK-läge). Se `fix(hooks)`-commit 2026-05-07.
