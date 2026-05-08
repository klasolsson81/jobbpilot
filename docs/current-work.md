# Current work — JobbPilot

**Status:** STEG 8 KLAR. TD-9 stängd. Audit log-infrastruktur live i kodbasen. Nästa: STEG 9 — kräver beslut.
**Senast uppdaterad:** 2026-05-08
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu

**STEG 8 klar.** Audit log-infrastruktur (BUILD.md §18 Fas 1) implementerad och pushad.

### STEG 8 — Audit log-infrastruktur

**Strategi (ADR 0022):** Pipeline-behavior + marker-interface, placerad innerst i Mediator-pipelinen (efter UnitOfWork). Audit-rad och data-mutation persisteras atomiskt i samma `SaveChangesAsync`.

**Domain:**
- `AuditLogEntry` (flat entity) + `AuditLogEntryId` i `Domain/Auditing/`
- `Static Create()`-factory med invariant-validering (event_type/aggregate_type whitespace-skydd, aggregate_id Guid.Empty-skydd)

**Application:**
- `IAuditableCommand` + `IAuditableCommand<TResponse>` med `ExtractAggregateId(response)`
- `ICorrelationIdProvider` + `IRequestContextProvider` (nya portar)
- `AuditBehavior<TMessage, TResponse>` — skip på Result.Failure, skip om command inte är auditable

**Infrastructure:**
- `AuditLogEntryConfiguration` (EF Core) — index `audit_log (occurred_at DESC)`, ingen FK
- `CorrelationIdProvider` — server-genererat (X-Correlation-Id-header läses INTE — audit-spoofing-skydd per OWASP ASVS V7.1.4)
- `RequestContextProvider` — IP-anonymisering (`/24` IPv4, `/48` IPv6) per Breyer-domen + WP29

**Markerade commands (10):**
- Application: CreateApplication, TransitionTo, AddFollowUp, AddNote, MarkGhosted
- Resume: CreateResume, RenameResume, UpdateMasterContent, DeleteResume, DeleteResumeVersion

**Pipeline-ordning (ny):**
```
Logging → Validation → Authorization → UnitOfWork → Audit → Handler
```

**Migration:** `20260508062501_AddAuditLogTable` — applicerad mot dev-DB. Tabell `audit_log` med kolumner per BUILD.md §7.1. Partitionerings-DDL-stub som kommentar i migrationen, aktiveras av Fas 4 retention-jobb.

**Tester:** +41 nya (14 Domain + 11 Application behavior + 12 Integration + 4 Architecture). Totalt 419 backend-tester, alla gröna.

### Reviews genomförda

- **dotnet-architect** (innan kod): 2 kritiska + 8 viktiga feedbacks. Pipeline-placering ändrad, `IAuditableCommand<TResponse>` med `ExtractAggregateId` införd, `ICorrelationIdProvider` + `IRequestContextProvider` nya portar.
- **code-reviewer:** Approved. 0 Blocker, 0 Major, 6 Minor (alla informationella, defereras till Fas 2/4).
- **security-auditor:** 0 Critical, 3 Major, 4 Minor.
  - Major #1 (IP-PII per Breyer): fixad — IP-anonymisering /24+/48 i RequestContextProvider
  - Major #2 (Art. 17-policy saknades): fixad — ADR 0022-sektion + TD-16 ny
  - Major #3 (X-Correlation-Id user-controlled audit-spoofing): fixad — server-gen alltid
  - Minor #1 (MarkGhosted XML-doc): fixad
  - Minor #3 (ImpersonatedBy TODO): fixad
  - Minor #2, #4: defereras (alerting + smoke-test, prod-deploy-blockare)

### Viktiga tekniska beslut (STEG 8)

- **Pipeline-behavior framför domain event subscriber:** ingen domain event dispatcher finns idag → bygga en är separat ADR + scope. Pipeline-behavior är atomiskt rätt och scope-effektivt.
- **AuditBehavior INNERST (efter UoW):** Audit:s post-action lägger entity i DbContext, UoW.SaveChanges persisterar handler-mutation och audit-rad atomiskt i samma transaction.
- **Generic `IAuditableCommand<TResponse>` + `ExtractAggregateId`:** löser Create-fall där ID genereras i handler (response.Value) vs mutation av befintliga aggregat (command-fält).
- **Failure → ingen audit i Fas 1:** mutations-failures loggas redan strukturerat via LoggingBehavior. Failed-attempts retro-fittas i Fas 6.
- **Ingen payload i Fas 1:** `audit_log.payload` reserverad i schema men null. PII-sanering deferras till Fas 4.
- **Ingen FK mot users/job_seekers:** audit är write-only och får inte hindras av soft-delete-cascades.
- **IP-anonymisering /24+/48:** bevarar geo-region för incident-response, eliminerar unique fingerprint. WP29-standard.
- **Server-genererat correlation-ID:** OWASP ASVS V7.1.4. X-Correlation-Id-header läses inte längre — klient-correlation kan komma som separat fält senare.
- **Worker-pipeline-registrering deferreras till Fas 2:** Worker-shell är tom (ADR 0010), ingen Mediator/Application-DI ännu.

## Senaste commits

| SHA | Beskrivning |
|-----|-------------|
| (pending) | docs: STEG 8 docs-sync (current-work + steg-tracker + tech-debt + session-logg) |
| (pending) | feat(auditing): STEG 8 — audit log-infrastruktur (ADR 0022, stänger TD-9) |
| 1cb2926 | docs(claude): förtydliga §1.5 — docs-sync efter varje STEG, inte bara session-end |
| 3172cdc | docs: session-avslut STEG 7b + steg-tracker + current-work uppdaterade |
| 64ea3cc | docs(sessions): session-logg STEG 7a — Resume-aggregat backend |

## Open follow-ups

Tech-debt-status efter STEG 8:

- ~~**TD-9** — STÄNGD (audit log för Application-domänhändelser)~~
- **TD-13** — Encryption av PII-kolumner i Fas 2 (paritet med cover_letter)
- **TD-14** — DeleteResumeVersion VersionInUse-check aktiveras i Fas 4
- **TD-15** — Resume-formulär: koppla Zod-issue path till `aria-invalid` per fält (a11y-pass)
- **TD-16** — *NY:* Audit-log retention + GDPR Art. 17-anonymisering (Fas 4 + blocker för Fas 1 prod-deploy)

## Tester totalt

- **Backend:** 419 (148 Domain + 157 Application + 10 Architecture + 104 Integration) — +41 sedan STEG 7
- **Frontend:** 65 Vitest + 19 Playwright E2E (oförändrat)

## När nästa session startar

1. Kör `git log --oneline -10` — verifiera HEAD
2. Verifiera backend-tester: kör test-exen direkt under `tests/*/bin/Debug/net10.0/` (`dotnet test` på solution-nivå är trasigt)
3. Läs `docs/steg-tracker.md` §6 för STEG 9-kandidater
4. Läs senaste session-logg (STEG 8) för detaljer

## Kända begränsningar / quirks

- **postgres-dev** på port **5435** — `appsettings.Local.json` med rätt port + `.env`-lösenord
- **`dotnet ef`** plockar inte upp `appsettings.Local.json` — använd `export ConnectionStrings__Postgres=...`
- **`dotnet test`** på solution-nivå returnerar "Zero tests ran" (xunit.v3.mtp-v2-issue)
- **API kräver `ASPNETCORE_ENVIRONMENT=Development`** för Redis-connstring
- **Audit-tabellen växer obegränsat** i dev — TD-16 dokumenterar retention-jobb-implementation som blocker för prod-deploy
- **Middleware-deprecation-varning** i Next.js (kvar från STEG 6)
