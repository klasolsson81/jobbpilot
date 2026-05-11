---
session: Fas 1-milestone-stängning — admin-audit-vy
datum: 2026-05-11
slug: fas1-milestone-admin-audit
status: KLAR
commits:
  - feat(api,web): Fas 1-stängning — admin-audit-vy + roll-claim + AdminEndpoints
  - docs(adr): 0028 admin-authorization defense-in-depth
  - docs: Fas 1-milestone-stängning session-end
---

# Fas 1 milestone-stängning — admin-audit-vy

## Mål

Stänga BUILD.md §18 Fas 1-milestone formellt: "Du kan skapa CV manuellt,
submit:a 'fake' ansökningar, se dem i admin-audit." Audit-skrivning fanns
sedan STEG 8 (ADR 0022) + partitionering (STEG 10a). Det som saknades:
**läs-yta** + **admin-roll-infrastruktur** + **frontend-route**.

## Sammanfattning

Tre commits pushas. Backend 585/585 grön (+19 nya tester), frontend 150/150
grön (+7 nya). 12 in-block-fixar applicerade efter 5 parallella agent-reviews
+ CTO-triage. 6 nya TDs lyfta. ADR 0028 dokumenterar admin-authorization
defense-in-depth-pattern retroaktivt.

## CTO-beslut bakom design

Multi-approach-val per CLAUDE.md §9.2 → senior-cto-advisor invokerad
automatiskt:

**Beslut A — Roll-claim-flow**
- A1 vald (per-request fetch i SessionAuthenticationHandler)
- A2 avvisad (Session-record bär roller — 7-dagars stale-fönster, security-regression)
- A3 avvisad (manuell handler-check — bryter CLAUDE.md §5.4 anti-pattern)

**Beslut B — Admin-seeding**
- B1 vald (IdempotentAdminRoleSeeder IHostedService — IaC-konsistens)
- B2 avvisad (manuellt psql-script — bryter Twelve-Factor §III/V)

**Beslut C — Route-slug**
- C1 vald (`/admin/granskning` per CLAUDE.md §4.4 svensk-substantiv-konvention)
- C2 avvisad (`/admin/audit` — bryter konvention utan tillräcklig motivering)

**Beslut D — Scope** (eskalerat till Klas)
- Klas valde: endast read-only audit-vy + roll-infra. BUILD.md ord-för-ord.
- Användarlistning/suspend/impersonate hör till Fas 6 per BUILD.md fas-tabell.

## Implementation

### Backend

**Application-lagret:**
- `Common/Authorization/Roles.cs` — `public const string Admin = "Admin"` (magic-string-fix)
- `Common/Authorization/AuthorizationPolicies.cs` — `Admin = "AdminPolicy"` (separat från roll-namn per dotnet-architect Viktigt #1)
- `Common/Abstractions/IAdminRequest.cs` — marker, ärver IAuthenticatedRequest (401-före-403)
- `Common/Behaviors/AdminAuthorizationBehavior.cs` — defense-in-depth-behavior
- `Common/PagedResult.cs` — generisk paged-shape med argument-validering (PageSize>=1)
- `Admin/Queries/GetAuditLogEntries/` — Query + Handler + Validator + DTO

**Infrastructure-lagret:**
- `Auth/SessionAuthenticationHandler.cs` — injicerar IUserAccountService, emit:ar ClaimTypes.Role per request, try/catch role-fetch → AuthenticateResult.Fail (Sec-Minor-2)
- `Auth/CurrentUser.cs` — IsInRole impl
- `Identity/AdminBootstrapOptions.cs` + `IdempotentAdminRoleSeeder.cs` — IHostedService med ct.ThrowIfCancellationRequested + PostgresException 42P01-catch som test-fixture-resilience. PII-disciplin: loggar UserId istället för email (M2).

**Api-lagret:**
- `Endpoints/AdminEndpoints.cs` — GET /api/v1/admin/audit-log med .RequireAuthorization(AuthorizationPolicies.Admin)
- `Program.cs` — AddAuthorization-policy, ForbiddenException → 403-middleware

**Tester:**
- 7 integration-tester (401/403/200/filter/roll-revoke-immediacy/validation)
- 3 AdminAuthorizationBehavior-unit-tester (defense-in-depth)
- 8 GetAuditLogEntriesQueryHandler-unit-tester (filter-permutationer)
- 1 ApplicationLayerTests architecture-test (Application får inte bero på ASP.NET) — dotnet-architect Viktigt #2

### Frontend

**Route-grupp `(admin)`:**
- `(admin)/layout.tsx` — roll-check via getServerSession + redirect till `/` om icke-Admin
- `(admin)/admin/granskning/page.tsx` — Server Component, URL-searchParam-driven filter
- 3 Server Components: AuditLogFilter, AuditLogTable, AuditLogPagination

**API + typer:**
- `lib/api/admin.ts` — diskriminerat union AuditLogResponse (ok/forbidden/unauthorized/error)
- `lib/types/admin.ts`
- `lib/auth/session.ts` — ny `ROLES = { Admin: "Admin" } as const` (FE-M2)

**Civic-utility-pattern (design-reviewer Praise):**
- Zero `"use client"` i scope
- URL-driven state via searchParams (bookmarkbar, browser-back fungerar)
- Native HTML `<form method="get">` för filter
- Native `datetime-local` istället för custom date-picker
- Semantisk `<table>` + `<caption>` + `scope="col"`
- Distinkta error-meddelanden per kind (forbidden/unauthorized/error)
- Europe/Stockholm-explicit timezone (FE-M4)
- pageSize-klamp till 200 (FE-M5)

**Tester:**
- 10 audit-log-table-tester
- 7 audit-log-pagination-tester
- 7 audit-log-filter-tester (FE-M3)

### Conditional Admin-länk i (app)-layout

`(app)/layout.tsx` visar Granskning-länk endast om `user.roles.includes(ROLES.Admin)`.

## Reviews

5 parallella agent-reviews. Alla APPROVED. Rapporter sparade i
`docs/reviews/2026-05-11-fas1-admin-audit-*.md`:

| Review | Verdict | Blockers | Major | Minor/Nit |
|--------|---------|----------|-------|-----------|
| Backend code-reviewer | APPROVE-WITH-FIXES | 0 | 0 | 4 Minor, 3 Nit |
| Backend security-auditor | Approved | 0 | 0 | 5 Sec-Minor |
| Backend dotnet-architect | APPROVE-WITH-FIXES | 0 | 2 Viktigt | 7 Mindre |
| Frontend code-reviewer | Approved | 0 | 0 | 5 Minor, 2 Nit |
| Frontend design-reviewer | Approved | 0 | 0 | 3 Minor, 2 Nit |

## CTO-triage av reviews

senior-cto-advisor invokerad för triage mot 4-timmarsregeln. Total in-block-scope:
~2h22min (under 4h). CTO motiverade fix-listan mot Clean Arch principles, MS
Learn Role-based authorization, GDPR-praxis, WCAG 2.1 AA.

**In-block applied:**
- Backend Viktigt #1 (AuthorizationPolicies-konstant)
- Backend Viktigt #2 (ApplicationLayerTests)
- Backend M1 (ct.ThrowIfCancellationRequested)
- Backend M2 (PII email → UserId)
- Backend Sec-Minor-2 (role-fetch try/catch)
- Backend N2+M7 (PagedResult guard)
- Backend M4 (AdminAuthorizationBehaviorTests + GetAuditLogEntries handler-tester)
- Frontend FE-M2 (ROLES-const)
- Frontend FE-M3 (AuditLogFilter-test)
- Frontend FE-M4 (timezone Europe/Stockholm)
- Frontend FE-M5 (pageSize-klamp)
- Frontend FE-Mi2 (border-danger-600/30 ersätter saknad danger-200)

**TDs lyfta (6 nya: TD-50 till TD-55):**
- TD-50: Prod-konfig-källa runbook (Sec-Minor-1)
- TD-51: Admin-läs audit (Fas 6, Sec-Minor-3)
- TD-52: Admin rate-limit (Fas 6, Sec-Minor-4)
- TD-53: Kind-union vs T|null (>4h, FE-M1)
- TD-54: text-tertiary kontrast projektbrett (FE-Mi1, replikerat)
- TD-55: PagedResult retro-fit för GetApplicationsQuery m.fl.

**Separat docs-commit:** ADR 0028 (admin-authorization defense-in-depth).

**Nits ignorerade:** mestadels kommentar-justeringar, dead-code-branches,
cosmetic alias.

## Tester totalt

- **Backend:** 585 (157 Domain + 194 Application UnitTests + 24 Architecture + 26 Worker + 178 Api Integration + 6 Migrate UnitTests) — alla gröna
- **Frontend Vitest:** 150 (143 + 7 nya filter-tester)
- **Backend tillkomst:** +19 (7 admin-integration + 3 AdminAuthorizationBehavior + 8 GetAuditLogEntries handler + 1 ApplicationLayerTests arch)
- **Frontend tillkomst:** +24 (10 table + 7 pagination + 7 filter)

## ADR

**ADR 0028:** Admin authorization via marker-interface + HTTP-policy defense-in-depth.
Skriven av adr-keeper retroaktivt. ADR 0027 var upptaget (HTTPS-aktivering),
0028 är nästa fria nummer. Refererar ADR 0008, 0017, 0019, 0022.

## Lärdomar

- **Per-request roll-fetch är right design för Fas 1:** A2:s Session-record-cache är prematur optimization med 7+ touch-points blast-radius. UserManager har request-scope-cache → en DB-query per request. Mitigering till memcache är isolerad ändring vid behov.
- **Defense-in-depth via dubbel-gate fångade real risk:** AdminAuthorizationBehavior i Mediator-pipen fångar Worker-/CLI-/test-fixture-dispatch som HTTP-policy missar. WorkerSystemUser.IsInRole => false enforce:ar isolation.
- **Test-fixture catch-22:** WebApplicationFactory-flödet triggar host-start innan migrations körs. IdempotentAdminRoleSeeder catch:ar PostgresException 42P01 som pragmatisk resilience. I prod kör JobbPilot.Migrate FÖRE Api-tasken → catch träffas aldrig.
- **CLA1848 LoggerMessage source-gen** är obligatorisk under .NET 10 i JobbPilot — direkt LogWarning() bryter build. Var noggrann med både IdempotentAdminRoleSeeder och SessionAuthenticationHandler.
- **AuthorizationPolicy-namn vs roll-namn:** false equivalence när båda är "Admin"-strängen. dotnet-architect Viktigt #1 fångade — `AuthorizationPolicies.Admin = "AdminPolicy"` är separation.
- **Server Components-disciplin pinnacle:** zero "use client" i admin-route, native HTML form GET, URL-driven state. design-reviewer noterade specifikt som mönsterimplementation.
- **Tidszons-bug i manuell datum-format:** `d.getHours()` på server kräver Europe/Stockholm-server. `toLocaleString("sv-SE", { timeZone: ... })` är robust.

## Pre-existing infra (oförändrat)

| Resurs | Identifier |
|---------|-----------|
| Public URL | `https://dev.jobbpilot.se/api/ready` |
| API task-def | `jobbpilot-dev-api` (post-TD-38 apply) |
| Worker task-def | `jobbpilot-dev-worker` (post-TD-38 apply) |
| Tag (senaste) | `v0.1.2-dev` på SHA `7cde3c7` |

## Nästa session — startprompt

Sparas i separat fil eller embedded nedan av session-end-rutinen.

## Cost

Oförändrat ~$79.65/mån (inga nya AWS-resurser).
