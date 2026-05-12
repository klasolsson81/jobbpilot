# Current work — JobbPilot

**Status:** **F2-P7 + P8a + P8a.5 + bootstrap komplett 2026-05-12 ~21:10. Dev-deploy GRÖN på `v0.2.1-dev` (`https://dev.jobbpilot.se/api/ready` returnerar 200). 17 commits, 3 nya ADRs (0032/0033/0034), 1 stängd TD (TD-56), 3 nya TDs (72/73/74), 4 parallella post-hoc reviewers + 4 CTO-ronder. Nästa: F2-P8b (Infrastructure — Refit + Resilience + admin-trigger-endpoint).**
**Senast uppdaterad:** 2026-05-12 (session-end efter F2-P7 + P8a + bootstrap + aggregate-review)
**HEAD:** `24a7135` (tag `v0.2.1-dev` på samma SHA, deployad)
**Långsiktig bana:** `docs/steg-tracker.md`
**Tech debt:** `docs/tech-debt.md` (aktiva) + `docs/tech-debt-archive.md` (stängda)

---

## Aktivt nu — F2-P8a komplett, P8b nästa

### Levererat denna session (17 commits)

| Commit | Innehåll |
|---|---|
| `0fc4b76` | feat(jobads): F2-P7 — JobAd-paginering med PagedResult (TD-56 stängd) |
| `6bdce04` | docs(adr): ADR 0032 utkast — JobTech-integration |
| `06ee2b3` | docs(adr): ADR 0032 → Accepted |
| `c5aa089` | feat(jobads): F2-P8a — ExternalReference VO + JobAd.Import + EF migration |
| `4bb91d8` | feat(migrate): F2-P8a.5 — CLI-mode-dispatch + Phase E (ADR 0033) |
| `ff136ad` | feat(deploy): F2-P8a.5c — auto-trigga schema-task i deploy-dev.yml |
| `0fe0ce6` | fix(migrate): Dockerfile Infrastructure-projekt-context |
| `ad7988f` | fix(infra): ecs:DescribeTasks task-ARN-pattern |
| `f69308f` | fix(migrate): Dockerfile aspnet-runtime |
| `daab6ec` | fix(deploy): containerOverrides.command mode-arg |
| `2c9232a` | fix(migrate): Dockerfile RDS-CA-bundle |
| `b1f50bf` | feat(migrate): F2-P8a.5e bootstrap-mode + ADR 0034 |
| `e228b7f` | refactor(infra): MigrationsOptionsFactory single source of truth (DRY) |
| `baf901b` | fix(api): F2-P7 auth-gate + sort-default-explicit (review-fynd) |
| `acc6ff3` | fix(migrate): Bootstrap re-fetch + extract types + password-local |
| `bef983c` | fix(infra): EcsReadOurCluster cluster-condition |
| `24a7135` | docs(tech-debt+adr): TD-13 utökas + TD-72/73/74 + ADR 0032 §8-amendment |

### Granskningstrail

- `docs/reviews/2026-05-12-f2-p7-p8-cto.md` — CTO-rond 1 (P7+P8 designval)
- `docs/reviews/2026-05-12-f2-p7-p8a-aggregate.md` — 4 parallella reviewers + CTO-rond 4
- `docs/sessions/2026-05-12-2110-f2-p7-p8a-bootstrap-aggregate-review.md` — session-log

### ADRs

- **ADR 0032** Accepted — JobTech-integration (+ §8-amendment för PII-stripping)
- **ADR 0033** Accepted — Migrate CLI-mode-dispatch (+ amendment för auto-trigga)
- **ADR 0034** Accepted — DB-role privilege-separation (Saltzer/Schroeder)

### TD-status

- **TD-56** stängd (paginering)
- **TD-72** (Minor, Trigger) — bootstrap auto-trigga vid Identity-schema-change
- **TD-73** (Major, Fas 2 P8c-gating) — JobTech raw_payload PII-stripping + retention
- **TD-74** (Minor, Fas 2 opportunistic) — strikta DML-GRANTs istället för GRANT ALL
- **TD-13** utökas — `job_ads.raw_payload` tillagd i berörda-kolumner-listan

Aktiva: 16 → 18 (TD-56 stängd, TD-72/73/74 lyfta).

### AWS dev-state efter session

- 10 EF-migrations applicerade på public-schema (InitialCreate till Fas2P8aJobAdExternalReference)
- 2 Identity-migrations applicerade på identity-schema (InitialIdentity, AddAuthProviderToUser)
- identity-schema skapad med jobbpilot_app GRANT USAGE/CREATE/ALL on tables
- API + Worker live på `v0.2.1-dev`, smoke-test `HTTP 200` mot `/api/ready`
- IAM policy `jobbpilot-github-actions-deploy-dev` version 5 (med `EcsRunMigrateTaskInDevCluster` + `EcsReadOurCluster.cluster-condition`)

### Tester (full svit grön)

- Domain.UnitTests: 202 → **218** (+16)
- Application.UnitTests: 249 → **258** (+9)
- Architecture.Tests: 32 → **33** (+1 ListJobAdsQuery_returns_PagedResult)
- Api.IntegrationTests: ~226+ (uppdaterade för auth-gate)
- Migrate.UnitTests: 6 (oförändrat)

### Sessions disciplinmissar fångade + fixade

1. Migrate-Dockerfile saknade transitiva Project-references → fixat
2. IAM `ecs:DescribeTasks` saknade task-ARN-pattern → fixat
3. Dockerfile runtime-image saknade ASP.NET-framework → fixat
4. `containerOverrides.command` skickade hela kedjan istället för bara mode-arg → fixat
5. Migrate-Dockerfile saknade RDS-CA-bundle → fixat
6. Original copy-paste-fix för Identity-options-skew → DRY-refactor till `MigrationsOptionsFactory` (Klas-discipline-feedback)
7. Reviewers inte invokerade inline → post-hoc audit + 4 fix-commits

### Lärdomar

- **Web-search räddade scope** vid två tillfällen (Npgsql #1770 + AWS OIDC thumbprint)
- **CTO-disciplin** vid multi-approach-val gav 4 separata CTO-ronder med tydliga beslut
- **DRY > copy-paste** — Klas fångade `MigrationsOptionsFactory`-extraktion innan spaghetti-fix
- **Auto-trigga schema-mode i CI** fångade F2-P0b-glömskan mekaniskt
- **Bootstrap-mode-design** bevarar least-privilege permanent (ingen `CREATE ON DATABASE` på `jobbpilot_app`)

---

## Nästa session — F2-P8b

Per ADR 0032 leverans-plan: P8b — Infrastructure-leverans.

### Scope

- `IJobTechSearchClient` via **Refit** (klassisk REST/JSON, BUILD.md §9.1)
- `IJobTechStreamClient` typed-client (NDJSON long-polling + polymorft event-schema)
- `PlatsbankenJobSource : IJobSource`
- `Microsoft.Extensions.Http.Resilience` + `AddStandardResilienceHandler` (Polly v8, BUILD.md §9.1 semantik: 3 retry expo + CB 5/5min)
- `JobTechOptions` (appsettings-binding) + `IOptions<T>`-pattern
- Admin-trigger-endpoint `POST /api/v1/admin/job-ads/sync/platsbanken` (synkron snapshot för smoke-test)
- WireMock-baserade integration-tester
- **TD-73-arbete (blockerar P8c):** `JobTechPayloadSanitizer` + allowlist-design + 30d retention via Hangfire-job

### Klas-STOPP-flagga (CTO-rond 1)

Admin-endpoint exponerar synkron JobTech-call → verifiera resilience-config mot dev innan tag-push.

### Pending operativt

- (Inga blockerande från denna session) — dev-deploy är grön
- TD-72 trigger om Identity-schema-migration läggs på roadmap för Fas 2-end
- Bootstrap-task manuellt körd 2026-05-12 (engångs eller vid Identity-schema-change)

---

## Tidigare session — F2-P3 + P4 + P6 komplett

Föregående session (eftermiddag 2026-05-12) levererade Budget Actions
terraform-modul (F2-P3), cost-recovery-runbook full utbyggnad (F2-P4),
strict readiness-probe-split (F2-P6, TD-29 stängd). Alla Fas 2-prereqs
avklarade.

Se session-log `docs/sessions/2026-05-12-1330-fas2-p3-p4-p6-prereqs-komplett.md`.
