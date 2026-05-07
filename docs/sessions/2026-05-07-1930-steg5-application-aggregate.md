---
session: "2026-05-07-1930"
datum: 2026-05-07
slug: steg5-application-aggregate
status: klar
commits:
  - sha: 82af5fa
    message: "feat(applications): implementera Application-aggregat med full CQRS-stack (STEG 5)"
---

## Mål

Implementera Application-aggregatet fullt ut: domain, EF Core, CQRS, API, tester. Koden var redan skriven från förra sessionen (pre-kompaktering) men inget hade committats.

## Genomförda steg

### Agent-granskning (code-reviewer + security-auditor)

Kördes i parallell efter att Klas påpekade att de fattades. Hittade:
- **code-reviewer Blockers:** EF Core i IAppDbContext (B1), saknade handler-tester (B2 — löst), saknade integration-tester (B3 — löst)
- **security-auditor Majors:** M-1 audit log saknas (TD-9), M-2 CoverLetter i list-DTOs (fixat)

### Fixes utan Klas-beslut (F-1 till F-4)

- F-1: Tog bort `CoverLetter` ur `ApplicationDto` — skapade separation list-DTO vs detail-DTO
- F-2: NotFoundException-meddelanden utan ApplicationId (ID-läckage)
- F-3: TODO(GDPR) i ApplicationConfiguration för CoverLetter
- F-4: `GetApplicationsQueryValidator` med `PageSize` 1-100

### Beslut B1-B4

- B1: Accepterat — IAppDbContext med DbSet<T> är etablerat undantag per ADR 0009
- B2: Fixat — `Application.AddFollowUp` returnerar `Result<FollowUpId>` direkt, handlens använder inte `[^1]`
- B3: Dokumenterat i TD-8 — GetPipeline är kanban-vy, `.Take(500)` som skyddsventil
- B4: TD-9 — GDPR-risk dokumenterad, audit-infrastruktur till Fas 1

### Tester (test-writer)

37 unit tests för 5 commands + 3 queries. 16 integration tests för 7 endpoints. Fixade xUnit1051-kompileringsfel (CancellationToken.None → FindAsync-overload med CT).

### xmin-RowVersion

**Rotsak:** `IsRowVersion()` på `byte[]` i Npgsql skapar `bytea NOT NULL`-kolumn utan databas-generering. INSERT misslyckas med NOT NULL-constraint.

**Fix:** Tog bort `byte[] RowVersion` ur domänklassen. Konfigurerade shadow property `uint xmin` med `ValueGeneratedOnAddOrUpdate` + `IsConcurrencyToken`. Npgsql 10 detekterar detta automatiskt och mappar till PostgreSQL:s `xmin`-systemkolumn (ingen DDL-kolumn behövs). Migration `RemoveRowVersionUseXmin` droppar den gamla `row_version`-kolumnen.

## Commits

| SHA | Meddelande |
|-----|-----------|
| 82af5fa | feat(applications): implementera Application-aggregat med full CQRS-stack (STEG 5) |

## Nästa session

STEG 6: Frontend för ansökningar. Pipeline-vy (kanban-board), ansökningsformulär, detaljvy med uppföljningar och noteringar. Next.js 16 + Tailwind v4 + civic design-tokens.
