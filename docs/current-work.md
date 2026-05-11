# Current work — JobbPilot

**Status:** **VÄG B TD-61 STÄNGD 2026-05-11 ~20:00 — väntar Klas-diff-granskning innan push.** TD-61 audit-trail-evidence-test för `IdempotentAdminRoleSeeder` levererad. Discovery avslöjade att ursprungs-premissen var false (seedern skriver inte till `AuditLogEntries`-tabellen). CTO valde Alt A: XML-doc korrigerad + integration-test mot ILogger (rätt sink). Backend 612 → 615. TD-61 stängd. 0 nya TDs lyfta.
**Senast uppdaterad:** 2026-05-11
**Långsiktig bana:** `docs/steg-tracker.md` — single source of truth för STEG/fas-progression
**Tech debt:** `docs/tech-debt.md`

---

## Aktivt nu — Väg B: TD-61 stängd (pending push)

**Stationär-CC-session 2026-05-11 ~19:00 — TD-61-stängning via discovery-driven Alt A.** Klas valde Väg B efter Väg A (ADR 0029) pushad. Original-scope: ~1h CC-tid. Faktisk scope: ~2h efter CTO-triage av provably-false TD-premiss.

### Discovery-fynd som triggade CTO-triage

TD-61 byggde på antagandet att `IdempotentAdminRoleSeeder.AddToRoleAsync` skriver till `AuditLogEntries`-tabellen via "samma Identity-pipeline som /auth/register". Discovery 2026-05-11:

- `AuditBehavior` är Mediator-pipeline-behavior som auditerar ENDAST `IAuditableCommand<T>`-markerade commands.
- Seedern anropar `UserManager.AddToRoleAsync` direkt — utanför Mediator.
- `RegisterCommand` implementerar INTE `IAuditableCommand` — använder bara `IAuthAuditLogger` (strukturerad logg).
- ADR 0022 §Kontext rad 11 bekräftar explicit: "IAuthAuditLogger ... skriver bara strukturerad logg, **inte till databas**".
- Admin-vyns `GetAuditLogEntriesQueryHandler` läser `AuditLogEntries`-tabellen, dit varken seedern eller `/auth/register` skriver.

Seederns XML-doc-claim om "samma audit-log som admin-vyn själv granskar" var alltså provably false.

### CTO-triage (multi-approach Alt A/B/C)

| Alt | Beskrivning | CTO-bedömning |
|-----|-------------|---------------|
| A | Korrigera XML-doc + integration-test mot ILogger (rätt sink) | **Valt.** ~1h. Bekräftar ADR 0022. Inga side-effects utöver test + XML-doc. |
| B | Lägg till `AuditLogEntry`-skrivning i seedern utanför Mediator | Avvisad: SRP-brott (Martin 2017 kap. 7), ADR 0022-port-erosion (Ford/Parsons/Kua 2017), kräver dedikerad ADR. |
| C | Defer till Fas 6 admin-impersonation | Avvisad: CLAUDE.md §9.6 anti-pattern "spara TD så scope inte växer". Evidence-kravet kan uppfyllas nu mot rätt sink. |

CTO-motiveringar mot: Martin 2017 (SRP), Martin 2008 (Clean Code kap. 4 — lögnaktiga kommentarer är defekter), Fowler 2018 (Refactoring kap. 3 — code smells), Ford/Parsons/Kua 2017 (Building Evolutionary Architectures — fitness functions skyddar portar), Cohn 2009 (Test Pyramid), 12-Factor §XI (Logs as event streams), ADR 0022 immutable-policy.

### Leverans

| Artefakt | Output | Status |
|----------|--------|--------|
| Seeder XML-doc | `src/JobbPilot.Infrastructure/Identity/IdempotentAdminRoleSeeder.cs` rad 17-31 — ärlig formulering: observability via `LogAdminAssigned` EventId=2 → ILogger → Serilog → Seq/CloudWatch. Anti-claim att DB-audit INTE skrivs + ADR 0022-hänvisning + Fas 6-defer-not. | ✓ Klart |
| Integration-test | `tests/JobbPilot.Application.UnitTests/IdentityBootstrap/IdempotentAdminRoleSeederAuditEvidenceTests.cs` — 3 testfall (happy path EventId=2 / idempotens EventId=3 / saknad user EventId=4) | ✓ Klart |
| TD-61 stängd | `docs/tech-debt.md` rad 1925+ — STÄNGD med full CTO-motivering + leveransdetalj | ✓ Klart |

### Test-strategi-detalj

Custom `CapturingLogger<T> : ILogger<T>` (private sealed class i test-filen) — undviker `Microsoft.Extensions.Logging.Testing` paketreferens. Identity-stack: `AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppIdentityDbContext>()` (matchar Worker-DI HTTP-fri pattern) + `UseInMemoryDatabase`. NSubstitute för `IHostEnvironment`-mock (env-namn "Test"). Tester accessar `internal sealed IdempotentAdminRoleSeeder` via befintlig `InternalsVisibleTo` i Infrastructure-csproj.

### Tester (full svit grön — pending push)

- Domain.UnitTests: **163** (oförändrat)
- Application.UnitTests: **204** (+3 från TD-61 audit-evidence-tester)
- Architecture.Tests: **32** (oförändrat)
- Migrate.UnitTests: **6** (oförändrat)
- Api.IntegrationTests: **184** (oförändrat)
- Worker.IntegrationTests: **26** (oförändrat)
- **Total: 615** (+3 från Väg B)

### Pending commits (1, väntar Klas-diff-granskning)

| Commit | Scope | Filer |
|--------|-------|-------|
| 1 | `test(infra): TD-61 — audit-evidence-test + XML-doc-korrigering för IdempotentAdminRoleSeeder` | `src/.../IdempotentAdminRoleSeeder.cs` + `tests/.../IdempotentAdminRoleSeederAuditEvidenceTests.cs` + `docs/tech-debt.md` + `docs/current-work.md` + `docs/sessions/2026-05-11-1900-vag-b-td61-audit-evidence.md` + `docs/steg-tracker.md` (om uppdaterad) + `STARTPROMPT-NÄSTA-2026-05-11.md` (raderas) |

Single bundled commit: docs + test + XML-doc-fix är en logisk enhet (TD-stängning).

---

## När nästa session startar

Klas reviewar diff per CLAUDE.md §6.3 punkt 4. Vid GO: 1 commit + push.

Sedan optionell väg:

- **Väg C-fortsättning:** Feature-arbete (Fas 2 JobTech blockerad till ADR 0005, eller annan icke-blockerad Fas 1-feature från steg-tracker)
- **Väg D:** Pausa

Aktiva TDs: TD-39, TD-41, TD-51, TD-52, TD-53, TD-56, TD-57, TD-58, TD-59. (TD-60 + TD-61 stängda.)

Inga aktiva TDs blockerar feature-arbete.

---

## Föregående session-summary (referens) — Väg A TD-60 ADR 0029

**2026-05-11 ~17:00:** TD-60 stängd via ADR 0029 (HTTP-auth-pipeline + IClaimsTransformation-disciplin) + 5 integration-tester. Backend 607 → 612. Commit `f4a1569`. ADR 0029 komplementär till ADR 0028 (supersedas inte). 0 nya TDs lyfta.

---

## Pre-existing infra (oförändrat)

| Resurs | Identifier |
|---------|-----------|
| Public URL | `https://dev.jobbpilot.se/api/ready` |
| API task-def | `jobbpilot-dev-api` (post-TD-38 apply) |
| Worker task-def | `jobbpilot-dev-worker` (post-TD-38 apply) |
| Tag (senaste) | `v0.1.2-dev` på SHA `7cde3c7` |

---

## Workflow-disciplin (oförändrad)

Per CLAUDE.md §9.2 + §9.6:

1. Discovery först (denna session: avslöjade falsk TD-premiss → CTO-triage triggerad)
2. Multi-approach-val → senior-cto-advisor auto-invokeras (denna session: Alt A/B/C — CTO valde Alt A entydigt motiverat mot Martin/Fowler/Cohn/Ford-Parsons-Kua/12-Factor/ADR 0022)
3. STOPP-rapport till Klas innan implementation om CTO osäker / fas-strategiskt (denna session: ingen STOPP behövd — CTO-rek entydigt + användar-mode "kör utan att stanna")
4. Agent-reviews parallellt vid relevant scope (denna session: code-reviewer + dotnet-architect parallellt + 1 CTO-triage)
5. In-block-fix-default per 4h-regel (alla agent-fynd hanterade in-block, 0 nya TDs)
6. Commit + push efter Klas-diff-granskning (direct-push till main per ADR 0019)
