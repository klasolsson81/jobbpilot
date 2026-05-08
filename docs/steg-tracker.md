# JobbPilot — STEG-tracker

> **Version:** 1.3
> **Senast uppdaterad:** 2026-05-08
> **Roll:** permanent översikt över STEG- och fas-progression.

Kompletteras av:
- `docs/current-work.md` — aktiv session-state
- `docs/sessions/` — per-session-loggar
- `docs/decisions/` — arkitekturbeslut
- `docs/tech-debt.md` — teknisk skuld

---

## 1. Översikt

JobbPilot:s utvecklingsbana spårar två dimensioner:

- **Faser** — strategiska tidsblock per BUILD.md §18. Nio faser plus Fas 9+ (efter klass-launch).
- **STEG** — tekniska arbetsenheter inom faser. Numrering är historisk och behålls oförändrad även när faserna inte mappar 1:1 mot STEG-gränserna.

Mellan-arbete (upptakter, cleanup-passningar, disciplin-uppgraderingar) är inte STEG men dokumenteras i §4.

## 2. Fas-översikt (per BUILD.md §18)

| Fas | Namn | Tidsuppskattning | Milstolpe | Status |
|-----|------|------------------|-----------|--------|
| Fas 0 | Foundation | ~2 v | Registrera + logga in på dev.jobbpilot.se | Lokalt klar¹ |
| Fas 1 | Core Domain | ~3 v | CV manuellt + "fake" ansökningar i admin-audit | Pågående |
| Fas 2 | JobTech Integration | ~2 v | Söka jobb på Platsbanken via appen, spara sökningar | Planerad² |
| Fas 3 | Application Management | ~2 v | Fullständig ansökningshantering (utan AI) | Planerad |
| Fas 4 | AI Layer | ~3-4 v | Alla AI-features end-to-end + 14 dagar dogfood | Planerad |
| Fas 5 | Integrationer | ~2 v | Gmail auto-loggar, intervjuer i Google Calendar | Planerad |
| Fas 6 | Admin & Analytics | ~2 v | Admin-panel komplett | Planerad |
| Fas 7 | Internal Beta | ~2 v | 3 användare aktivt 14 dagar | Planerad |
| Fas 8 | Klass-launch | ~1 v | 20 klasskamrater onboardade — v1 klar | Planerad |
| Fas 9+ | Efter klass-launch | — | Mobil, Kanban, intervjuträning, LinkedIn, Stripe, Chrome ext m.m. | Framtid |

**Totalt:** ~20 veckor till klass-launch (mjuk uppskattning, inga hårda deadlines).

¹ Fas 0:s lokala milstolpar uppfyllda (Clean Arch-solution, Identity, Next.js, design system). Kvarvarande för full Fas 0-stängning enligt BUILD.md §18: första deploy till dev.jobbpilot.se, GitHub Actions CI/CD verifierad, bootstrap-IAM-user raderad.

² Fas 2 är blockerad till ADR 0005 (go-to-market) är beslutad och kostnadsskydd implementerat (Budget Actions, `registrations_open`-flagga, rate limiting, runbook `docs/runbooks/aws-cost-recovery.md`) per BUILD.md §18.

## 3. STEG-historik

STEG-numrering följer faktisk arbetsutveckling och mappar inte exakt mot fas-gränserna i BUILD.md §18. Se §7 för numreringsfotnot.

### Klara

| STEG | Fas | Beskrivning | Sessions |
|------|-----|-------------|----------|
| Pre-STEG | Fas 0 | Research, plan, design-research | Sessions 1, 2, 2.5 |
| Pre-STEG | Fas 0 | AWS, Docker, agents, skills, hooks, GitHub, docs | Sessions 3-5 |
| STEG 1 | Fas 0 | .NET Solution-uppsättning + ADR 0008-0011 | Session 6 |
| STEG 2 | Fas 0 | Domain/Infrastructure/Application/API — JobAd aggregate (35 tester) | Session 7 |
| STEG 3 | Fas 1 | Auth-stack (Identity + JWT, ADR 0012-0014) + JobSeeker aggregate (75 tester) | Session 8 |
| STEG 4a | Fas 0 | Frontend bootstrap (Next.js 16, civic-tokens, shadcn nova, ADR 0015-0016) | Session 9 |
| STEG 4b | Fas 1 | Session-auth backend (ISessionStore, SessionAuthenticationHandler, IAuthAuditLogger, ADR 0017-0018) + frontend auth (login/register/me-sidor, /(app)-layout, 153 tester) | Session 4b.1, 4b.2 |
| STEG 5 | Fas 1 | Application-aggregat — domän (SmartEnum state machine, FollowUp, ApplicationNote), EF Core, 5 commands, 3 queries, 7 API-endpoints, 280 tester (53 nya) | 2026-05-07 |
| STEG 6 | Fas 1 | Frontend /ansokningar — pipeline-tabell, ny-ansökan, detaljvy, transitionsformulär, Server Actions, Zod v4, 28 Vitest + 13 Playwright E2E | 2026-05-08 |
| STEG 7a | Fas 1 | Resume-aggregat backend — domain (Resume AR + ResumeVersion + ResumeContent VO), EF JSONB via HasConversion, migration `AddResumeAggregate`, 5 commands + 2 queries, 7 API-endpoints, +98 tester. Plan-design via CC (utan webb-Claude). ADR 0021 (Master-mutation), TD-13/TD-14. | 2026-05-08 |
| STEG 7b | Fas 1 | Frontend /cv — Resume-pages, ResumeContentForm med RHF `useFieldArray` för Experiences/Educations/Skills, Server Actions, Zod v4, 37 Vitest + 6 Playwright E2E. TD-15. | 2026-05-08 |
| STEG 8 | Fas 1 | Audit log-infrastruktur — pipeline-behavior + marker-interface (ADR 0022). 10 commands märkta IAuditableCommand. Migration `AddAuditLogTable`. IP-anonymisering /24+/48, server-gen correlation-ID. Stänger TD-9. +41 tester (14 Domain + 11 Application + 12 Integration + 4 Architecture). | 2026-05-08 |

### Pågående

(inga aktiva STEG just nu — se §5)

### Planerade

| STEG | Fas | Beskrivning | Status |
|------|-----|-------------|--------|
| STEG 9 | Fas 1/0 | Ej beslutat. Kandidater: Hangfire-setup + GhostedDetectionJob (Fas 2-3 förskott), eller steg mot Fas 0-stängning (deploy till dev.jobbpilot.se, GitHub Actions, bootstrap-IAM-cleanup) | Behöver beslutas |

STEG 9 beslutas i nästa session.

## 4. Mellan-arbete

Cleanup-passningar, disciplin-uppgraderingar och dokumentations-arbete som inte hör till någon enskild STEG. Klas använder begreppet "Fas 0.x" för cleanup-arbete mellan officiella faser.

| Period | Beskrivning | Källor | Status |
|--------|-------------|--------|--------|
| 2026-05-07 | Upptakt: ADR 0019 etablerad (solo direct-push), CLAUDE.md uppgraderad (§9.4 discovery, §9.5 web-search, §9.2 utökad), tech-debt.md etablerad, hook-vakt fix:ad för Agent SDK-läget, precompact-rapporter exkluderade från versionshantering | Webb-chats: ADR 0019-chatt + Moment 1-5-chatt | Pågående (Moment 5 = denna tracker) |

## 5. Aktuellt

**STEG-fokus:** STEG 8 klar 2026-05-08. TD-9 (Major GDPR Art. 5(2) accountability-gap) stängd. Inga aktiva STEG.

**STEG 7a** (Resume-aggregat backend): Komplett 2026-05-08.

**STEG 7b** (Frontend /cv): Komplett 2026-05-08.

**STEG 8** (Audit log-infrastruktur): Komplett 2026-05-08. Pipeline-behavior + marker-interface (ADR 0022). 10 commands i Application+Resume märkta `IAuditableCommand<TResponse>`. Audit-rad och data-mutation persisteras atomiskt i samma SaveChanges. IP-anonymisering /24+/48 per Breyer-domen + WP29. Server-gen correlation-ID per OWASP ASVS V7.1.4. +41 tester. TD-9 stängd, TD-16 ny (retention + Art. 17-anonymisering, blocker för Fas 1 prod-deploy).

**Plan-design-modell:** STEG 7+8 körde plan-design via CC istället för webb-Claude — fungerade när scope var upprepningsmönster eller väl-avgränsad infrastruktur. Lärdom: webb-Claude behövs inte för upprepningsmönster eller när ADR-spec finns färdig.

**Nästa:** STEG 9 kräver beslut. Se §6.

För session-detaljer och commit-historik, se `docs/current-work.md`.

## 6. Nästa STEG

**STEG 9 — kräver beslut**

Två primära kandidater (Alt B från STEG 8-valet är nu klar — TD-9 stängd):

**Alt A — Hangfire-setup + GhostedDetectionJob (Fas 2-3 förskott)**
- Hangfire-infrastruktur i Worker-projektet (ADR 0010-shell aktiveras)
- Worker-pipeline-registrering (Mediator + Application + Infrastructure-DI)
- Stub-implementationer av `ICurrentUser`, `ICorrelationIdProvider`,
  `IRequestContextProvider` för Worker-scope (per ADR 0022)
- `GhostedDetectionJob` som kör `MarkGhostedCommand` för stale applications
- Konfiguration: `ghosted_threshold_days` per JobSeeker (default 21)
- Sätter scenen för Fas 4 AI-jobb och Fas 2 JobTech sync-jobb
- *Notera:* Hangfire är egentligen Fas 2 per BUILD.md §18 men kan tas tidigt

**Alt B — Steg mot Fas 0-stängning (BUILD.md §18 kvarvarande)**
- Första deploy till dev.jobbpilot.se
- GitHub Actions CI/CD verifierad (tag-baserad deploy per BUILD.md §15.3)
- Bootstrap-IAM-user raderad
- Stänger Fas 0 ordentligt innan vi går vidare

**Alt C — TD-16-implementation (Fas 1 prod-deploy-blockare)**
- Audit-log retention-jobb (Hangfire — beroende av Alt A)
- GDPR Art. 17-anonymiserings-cascade vid kontoradering
- Runbook `docs/runbooks/audit-retention.md`
- Stänger den enda kvarvarande prod-blockaren från STEG 8

**Rekommendation:** Alt A om Hangfire-infrastruktur behövs för senare faser (sätter chassit för Fas 4-AI-jobb och Fas 2-sync). Alt C kan inte göras utan Alt A. Alt B är ren Fas 0-stängning utan beroenden uppåt. Klas beslutar.

## 7. Numreringsfotnot

Faktisk historisk numrering följer projektets utveckling, inte BUILD.md §18:s fas-indelning. Sammanfattning:

- **Sessions 1-5:** pre-STEG infrastrukturarbete. Hör till Fas 0.
- **STEG 1-2:** kärnkod-grundläggning (.NET solution, JobAd domain). Hör till Fas 0.
- **STEG 3+:** post-bootstrap arbete. Hör delvis till Fas 0 (frontend bootstrap STEG 4a — design system-baseline) och delvis till Fas 1 (auth + Core Domain).
- **STEG 4a/4b sub-numrering:** "a/b/c"-suffix används när ett STEG sträcker sig över flera sessioner med substantiellt distinkt scope.
- **Moment-numrering** (1-5 i upptakter): separat axel för mellan-arbete, inte STEG.

Renumrering har övervägts och avvisats — bryter audit-trail mot commits, sessions/-loggar och ADR-referenser.
