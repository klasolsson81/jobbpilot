# Current work — JobbPilot

**Status:** Session 4b.1 KOMPLETT — STEG 4b Turn 4 (backend session-auth) klar. Nästa: Session 4b.2 — frontend auth-implementation (login/register/me-sidor, auth-helper).
**Datum:** 2026-05-06

---

## Aktivt nu

**STEG 4b Turn 4 (backend session-auth) KOMPLETT.** JWT-utgivning ersatt av stateful Redis-sessioner. 153 tester gröna. ADR 0017-0018 uppdaterade.

**Fas 0-milstolpe:**
- ✅ Backend auth-stack (STEG 3): Identity + JWT + Redis, 75 tester
- ✅ Frontend scaffold (STEG 4a): Next.js 16, civic-tokens, shadcn, demo-page
- ✅ Backend session-auth (STEG 4b Turn 1-4): ISessionStore + SessionAuthenticationHandler + IAuthAuditLogger, 153 tester
- 🔲 Frontend auth-flöden (Session 4b.2): login/register/me-sidor, auth-helper, Next.js proxy

**Vad som är på plats (Fas 0 bootstrap + STEG 1-4b Turn 4):**
- AWS-foundation, Docker-compose, Claude Code-agenter/skills/hooks, GitHub-integration, docs
- ADR 0001-0018
- .NET Solution: 5 src-projekt, 4 test-projekt
- Domain: JobAd aggregate + JobSeeker aggregate
- Infrastructure: AppDbContext, AppIdentityDbContext, SessionAuthenticationHandler (RFC 6750), ISessionStore (InMemory + Redis), IAuthAuditLogger (EventId 1001/1002/1003)
- Application: 4 pipeline-behaviors, auth commands (Login/Register/Logout + SessionDto), JobSeeker queries
- API: `/api/v1/job-ads` + `/api/v1/auth` + `/api/v1/me` · `/auth/refresh` → 410 Gone
- Tests: 153 tester (21 domain, 46 application, 6 arch, 80 integration)
- Frontend: `web/jobbpilot-web/` — Next.js 16.2.4, Tailwind v4 CSS-first, shadcn nova, civic design-tokens, Button/Input/Card

**Commits Session 4b.1:**

| Commit | Innehåll |
|--------|----------|
| *(pending push)* | feat(auth): refactor to stateful session-based authentication |

**Open follow-ups (ej blockande för Session 4b.2):**

| # | Beskrivning | Ursprung |
|---|-------------|----------|
| m5 | Höj warmup-iterationer i RedisSessionStoreTests till 32 om test flakar | code-reviewer Minor |
| m7 | Reflection mot ApiFactory i SessionStoreUnavailableTests — städ-PR Fas 0.x | code-reviewer Minor |
| m8 | AuthAuditLoggerTests fel projekt (Application.UnitTests testar Infrastructure) — städ-PR Fas 0.x | code-reviewer Minor |
| m9 | HashEmail-duplikation i LoginCommandHandler — löses naturligt vid JWT-radering Fas 1 | code-reviewer Minor |
| NB-1/2/3 | ADR 0017+0018 format/språk-divergens (English vs Swedish metadata, kolon vs em-dash) | adr-keeper |
| paste-pattern | CC paste:ar godkänd text verbalt men skriver aldrig till fil — disciplin-gap kräver CLAUDE.md-uppdatering | P3+P4 pattern |

**När nästa session startar (Session 4b.2 — frontend auth):**

1. Kör `git log --oneline -10` — verifiera Turn 4-commit på HEAD
2. Verifiera `dotnet test` — 153 tester gröna
3. Läs `docs/sessions/` senaste session-log
4. Starta frontend: `cd web/jobbpilot-web && pnpm dev`
5. Implementera: Next.js Route Handler proxy → .NET backend, `__Host-jobbpilot_session`-cookie, `lib/auth/session.ts`, login/register/me-sidor

## Klart senaste sessioner

- Session 1: research
- Session 2: plan
- Session 2.5: Design-research
- Session 3–5: AWS, Docker, agents, skills, hooks, GitHub, docs
- Session 6 ✅: ADR 0008-0011 + .NET Solution STEG 1
- Session 7 ✅: Domain, Infrastructure, Application, API, Tests (35 tester — JobAd)
- Session 8 ✅: STEG 3 — ADRs 0012-0014, Auth-stack, JobSeeker-aggregate, 75 tester
- Session 9 ✅: STEG 4a — ADRs 0015-0016, Next.js 16, civic-tokens, shadcn, demo-page
- Session 4b.1 ✅: STEG 4b Turn 1-4 — ISessionStore, SessionAuthenticationHandler, IAuthAuditLogger, ADR 0017-0018, 153 tester

## Kända begränsningar

Se **ADR 0006** för Claude Code-hooks-begränsningar.

**DesignTimeDbContextFactory** använder hårdkodade `postgres/postgres`-credentials för `migrations add`. Ej ett problem i runtime — bara för design-time verktyg.

**guard-spec-files.sh** matchar alla `CLAUDE.md` i repo:t (inte bara rot-relativa). Behöver justeras.
