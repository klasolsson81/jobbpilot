---
session: Väg B — TD-61 stängd via audit-trail-evidence-test för IdempotentAdminRoleSeeder
datum: 2026-05-11
slug: vag-b-td61-audit-evidence
status: KLAR (väntar Klas-diff-granskning innan push)
commits:
  - (pending) test(infra): TD-61 — audit-evidence-test + XML-doc-korrigering för IdempotentAdminRoleSeeder
---

# Väg B — TD-61 stängd via audit-trail-evidence-test

## Mål

Klas valde Väg B (TD-61 stängning) efter Väg A (ADR 0029) pushad samma dag.
TD-61 lyftes 2026-05-11 Block B av security-auditor (Minor 2) som hävdade att
seederns XML-doc-claim om "samma audit-log som admin-vyn själv granskar" inte
verifieras. Original-scope: ~1h CC-tid med antagandet att seederns påstående
var sann och bara behövde testas.

## Sammanfattning

Discovery avslöjade att TD-61:s ursprungspremiss var **provably false**. CTO
valde Alt A (korrigera XML-doc + integration-test mot rätt observability-sink).
3 testfall levererade i `IdempotentAdminRoleSeederAuditEvidenceTests`. TD-61
stängd. 0 nya TDs lyfta. Backend 612 → 615.

**Faktisk CC-tid:** ~2h (vs original 1h) — discovery + CTO-triage utökade scope
inom 4h-regeln per CLAUDE.md §9.6.

## Process

### Mandatory reads vid session-start

1. CLAUDE.md (hela), särskilt §9.2 agent-invocation + §9.6 4h-regel + §1.5 protocol
2. `docs/current-work.md` — Väg A TD-60 ADR 0029 status
3. `docs/sessions/2026-05-11-1700-vag-a-td60-adr-0029.md` — föregående session
4. `docs/decisions/0029-auth-pipeline-and-claims-transformation.md` — senaste ADR
5. `docs/tech-debt.md` (skim) — TD-61 + aktiva TDs

### Discovery — vidareläsning eftersom TD-premiss misstänkt

Verifierat on-disk-state:

1. **`AuditBehavior<TMessage,TResponse>`** (`src/JobbPilot.Application/Common/Auditing/AuditBehavior.cs`)
   är Mediator-pipeline-behavior. Auditerar ENDAST commands som implementerar
   `IAuditableCommand<TResponse>`. Skriver till `AuditLogEntries`-tabellen via
   `db.AuditLogEntries.Add(entry)` + UnitOfWorkBehavior:s atomiska SaveChanges.
2. **`IdempotentAdminRoleSeeder`** (`src/JobbPilot.Infrastructure/Identity/IdempotentAdminRoleSeeder.cs`)
   anropar `UserManager.AddToRoleAsync(user, Roles.Admin)` direkt — utanför
   Mediator-pipelinen. Enda observability-spår: `LogAdminAssigned`
   (LoggerMessage EventId=2, Information-nivå) till ILogger.
3. **`RegisterCommand`** (Mediator-command bakom `/auth/register`) implementerar
   **INTE** `IAuditableCommand`. Anropar `IAuthAuditLogger.LoginSucceeded` som
   skriver via LoggerMessage 1001 till ILogger.
4. **ADR 0022 §Kontext rad 11:** "IAuthAuditLogger ... skriver bara strukturerad
   logg, **inte till databas**."
5. **`GetAuditLogEntriesQueryHandler`** läser uteslutande från `AuditLogEntries`-
   tabellen.
6. **`AddToRoleAsync`-grep:** ENDAST seedern anropar metoden. Ingen admin-UI för
   promotion (Fas 6).

**Slutsats:** Seederns XML-doc-claim är provably false. TD-61:s föreslagna åtgärd
("läs AuditLogEntries efter EnsureUserIsAdminAsync och assertera relevant rad")
skulle **fallera** för seedern.

### CTO-invocation (senior-cto-advisor)

Multi-approach Alt A/B/C presenterades:
- **Alt A:** Korrigera XML-doc + integration-test mot ILogger (rätt sink).
- **Alt B:** Lägg till AuditLogEntry-skrivning i seedern utanför Mediator-pipeline.
- **Alt C:** Defer till Fas 6 admin-impersonation.

**CTO-beslut: Alt A.** Motiveringar:

- **Fix the map, not the territory (Fowler 2018 Refactoring kap. 3):** XML-doc
  som ljuger är ett defekt. Korrigera premissen, bygg inte om systemet retroaktivt
  för att göra premissen sann.
- **SRP (Martin 2017 Clean Architecture kap. 7):** Alt B ger seedern två
  change-reasons (role-seeding-policy + audit-skrivnings-format).
- **ADR 0022 port-erosion (Ford/Parsons/Kua 2017 kap. 2 fitness functions):**
  ADR 0022 etablerar `IAuditableCommand`-marker som arkitektonisk port. Alt B
  introducerar en andra audit-skrivnings-port utanför pipelinen — kräver
  **dedikerad ADR**, inte TD-stängning.
- **Honesty over heroics (Martin 2008 Clean Code kap. 4):** comments that lie
  are defects.
- **Test Pyramid + 12-Factor §XI (Cohn 2009 + 12factor.net):** observability-
  spåret som faktiskt finns (LogAdminAssigned EventId=2 → Serilog → Seq/CloudWatch)
  är giltigt evidence-spår — bara fel sink antaget i TD-61.
- **CLAUDE.md §9.6 anti-pattern:** Alt C ("spara TD så scope inte växer")
  avvisat — evidence-kravet kan uppfyllas nu mot rätt sink inom 1h.

CTO avvisade Alt B specifikt: "ADR 0022:s pattern är stark nog att utesluta Alt B
i denna session. Seeder-bootstrap kan vara en distinkt kategori som förtjänar
egen port — men det avgörandet hör till en dedikerad ADR-diskussion, inte till
TD-61-stängning."

### Implementation

1. **XML-doc-korrigering** (`IdempotentAdminRoleSeeder.cs` rad 17-31):
   - Falsk claim "samma audit-log som admin-vyn själv granskar" ersatt med
     ärlig: "Observability (TD-61 korrigering 2026-05-11 efter CTO Alt A):
     bootstrap-aktivitet observeras via strukturerad logg — Admin-role-add
     emit:ar EventId=2 via LogAdminAssigned till ILogger, som routas till Seq
     (dev) eller CloudWatch Logs (staging/prod)."
   - Anti-claim: "Seedern populerar INTE audit_log-tabellen."
   - Hänvisning till ADR 0022 + Fas 6 admin-impersonation-ADR-kandidatur för
     dedikerad bootstrap-audit-port.
   - Referens till `IdempotentAdminRoleSeederAuditEvidenceTests` för audit-evidence.

2. **Integration-test levererat**
   (`tests/JobbPilot.Application.UnitTests/IdentityBootstrap/IdempotentAdminRoleSeederAuditEvidenceTests.cs`):
   3 testfall:
   - `StartAsync_when_matching_user_exists_emits_LogAdminAssigned_EventId_2`
   - `StartAsync_when_user_already_admin_takes_no_op_path_and_does_not_emit_EventId_2`
     (idempotens via EventId=3 LogAdminAlreadyAssigned)
   - `StartAsync_when_no_user_matches_does_not_emit_EventId_2_and_warns_user_not_found`
     (EventId=4 LogAdminUserNotFound)

   Tekniker:
   - `AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppIdentityDbContext>()`
     (matchar Worker-DI HTTP-fri pattern)
   - `UseInMemoryDatabase` med unikt namn per test (`identity-tests-{Guid}`)
   - Custom `CapturingLogger<T> : ILogger<T>` (private sealed class i test-filen,
     undviker `Microsoft.Extensions.Logging.Testing` paketreferens — minimum
     scope-change)
   - NSubstitute för `IHostEnvironment`-mock med env-namn "Test"
   - Accessar `internal sealed IdempotentAdminRoleSeeder` via existerande
     `InternalsVisibleTo` i Infrastructure-csproj

3. **TD-61-stängning** i `docs/tech-debt.md`: status STÄNGD med
   discovery-detalj, CTO-motivering, leveransdetalj, faktisk CC-tid, 0-TDs-not.

## Tester (full svit grön — väntar push)

- **Domain.UnitTests:** 163 (oförändrat)
- **Application.UnitTests:** 204 (+3 från IdempotentAdminRoleSeederAuditEvidenceTests)
- **Architecture.Tests:** 32 (oförändrat)
- **Migrate.UnitTests:** 6 (oförändrat)
- **Api.IntegrationTests:** 184 (oförändrat)
- **Worker.IntegrationTests:** 26 (oförändrat)
- **Total: 615** (+3)

Körning: per-projekt-executables (TestingPlatformDotnetTestSupport).

## Beslut/avvägningar

1. **Discovery > sammanfattnings-trust** — TD-formuleringar kan vara felgrundade.
   I detta fall var seederns XML-doc-claim provably false och TD-61:s föreslagna
   åtgärd skulle ha fallerat om implementerad rakt av. Discovery före
   implementation är CLAUDE.md §9.4-disciplin.
2. **CTO entydig motivering** mot Martin (Clean Code + Clean Architecture),
   Fowler (Refactoring), Ford/Parsons/Kua (Building Evolutionary Architectures),
   Cohn (Test Pyramid), 12-Factor §XI, ADR 0022 immutable-policy.
3. **Test-placering Application.UnitTests** över Api.IntegrationTests med
   Testcontainers — InMemory Identity-store räcker för EventId-evidence-test;
   Testcontainers reserveras för tester som verifierar Postgres-specifika
   side-effects (jfr `IdempotentAdminRoleSeederProdBubbleTests` för
   42P01-bubbling).
4. **Custom CapturingLogger** över `Microsoft.Extensions.Logging.Testing.FakeLogger`
   — minimum scope-change (~20 rader i test-filen vs ny paketreferens).
   FakeLogger kan introduceras vid bredare observability-test-sweep om/när
   pattern repeteras 3+ gånger (Rule of Three, Fowler 2018).
5. **0 nya TDs lyfta.** Bootstrap-audit-port-frågan (om DB-persistent audit för
   Identity-side-effects någonsin ska finnas) hör till Fas 6 admin-
   impersonation-ADR-arbete — inte en defekt i nuvarande system så länge
   XML-doc:en är ärlig.

## Risker/oklarheter

- **InMemory Identity-store har kända begränsningar** (concurrency tokens,
  komplexa queries). För detta scope (CreateAsync/FindByEmail/IsInRole/AddToRole)
  fungerar det. Vid framtida seeder-tester med komplexare query-mönster:
  pivotera till Testcontainers via `ApiFactory`-pattern.
- **EventId=N-assertions är fragile mot ID-omnumrering.** Mitigerat av
  `LoggerMessage`-attribut som fixerar EventIds i seedern (compile-time
  konstanter). Om framtida refaktorering ändrar EventIds: testet failar tydligt
  med "expected EventId=2 not found" — fail-fast.

## Nästa session

Klas reviewar diff (CLAUDE.md §6.3 punkt 4). Vid GO: 1 commit + push.

Sedan optionell väg:

- **Väg C-fortsättning:** Feature-arbete (Fas 2 JobTech blockerad till ADR 0005,
  eller annan icke-blockerad Fas 1-feature).
- **Väg D:** Pausa.

Aktiva TDs efter denna session: TD-39, TD-41, TD-51, TD-52, TD-53, TD-56, TD-57,
TD-58, TD-59. (TD-60 + TD-61 stängda.)
