# Current work — JobbPilot

**Status:** **F2-P8b komplett 2026-05-13 ~06:30. Tag `v0.2.2.1-dev` LIVE på dev (deploy `25778778579`). 5 commits + Terraform-sync för AdminBootstrap. CTO-rond 5: Variant B — accepterade 504 ALB-timeout på admin-trigger eftersom ADR 0032 §3 redan designar snapshot som Hangfire-cron. Pipeline-mekanik verifierad i CloudWatch (CT-propagation, no partial-persist). End-to-end DB-persist-verifiering flyttad till P8c-acceptance-criteria som planerat arbete (inte TD per §9.6). TD-73 punkt 1 + 3 levererade. Nästa: F2-P8c (Hangfire-jobben).**
**Senast uppdaterad:** 2026-05-13 (session-end efter F2-P8b + smoke-test + CTO-rond 5)
**HEAD:** `03f8207` (commit för global.json roll-forward-fix) + Terraform-uncommitted i wd
**Deploy:** `v0.2.2.1-dev` live på `https://dev.jobbpilot.se/api/ready` (200 OK)
**Långsiktig bana:** `docs/steg-tracker.md`
**Tech debt:** `docs/tech-debt.md` (aktiva) + `docs/tech-debt-archive.md` (stängda)

---

## Aktivt nu — F2-P8b komplett, tag-push väntar Klas-GO

### Levererat denna session (5 commits)

| Commit | Innehåll |
|---|---|
| `8c09191` | feat(jobads): F2-P8b — JobTech Infrastructure + admin-trigger-endpoint |
| `037e0e8` | docs: session-end 2026-05-13 — F2-P8b komplett + TD-73 partial-progress |
| `8d89ded` | fix(jobads): F2-P8b — JobStream v2 path-migration (klas-fynd) |
| `139a85e` | fix(jobads): F2-P8b — JobStream v2-shape (webpage_url + PII-skydd) |
| `03f8207` | fix(build): global.json rollForward latestPatch → latestFeature |

Plus uncommitted Terraform-sync för `AdminBootstrap__InitialAdminEmail` (commit pågår).

### Granskningstrail

- `docs/sessions/2026-05-13-0600-f2-p8b-jobtech-infra.md` — session-log
- Agent-rapporter inline (code-reviewer + security-auditor + dotnet-architect)
- Tidigare session: `docs/sessions/2026-05-12-2110-f2-p7-p8a-bootstrap-aggregate-review.md`
- `docs/reviews/2026-05-12-f2-p7-p8a-aggregate.md` — F2-P8a aggregate-review (lyfts in i denna session)

### Leveranser

| Område | Innehåll |
|---|---|
| **Application port** | `IJobSource` + `JobAdSnapshot`/`JobAdChange`/`JobAdUpsert`/`JobAdRemoval` (LSP-diskriminerad union) + `JobAdImportItem` |
| **Infrastructure clients** | Refit-baserad `IJobTechSearchClient` (internal) + typed `IJobTechStreamClient` + `JobTechStreamClient` (NDJSON streaming + snapshot) |
| **Sanering** | `JobTechPayloadSanitizer` pure static allowlist, default-deny per Saltzer/Schroeder 1975, recursive sanering (TD-73 punkt 1) |
| **DI** | `AddJobSources`: Standard resilience (Search) + custom Polly-pipeline (Stream: RateLimiter → Retry → CB), process-statisk `FixedWindowRateLimiter(1, 1 min)` |
| **Application command** | `SyncPlatsbankenSnapshotCommand` (IAdminRequest) + handler (bulk-fetch + in-memory split — race-skydd via DbUpdateException är P8c-scope per ADR 0032 §5) |
| **Api-endpoint** | `POST /api/v1/admin/job-ads/sync/platsbanken` (AuthorizationPolicies.Admin) |
| **GDPR-docs** | `docs/runbooks/gdpr-processing-register.md` skapad med JobTech-entry (TD-73 punkt 3) |

### ADRs

- **ADR 0032** Accepted — JobTech-integration (P8a + P8b § levererade. §8-amendment 2026-05-12 punkt 1 + 3 levererade. Punkt 2 + 4 kvarstår för P8c. **Amendment 2026-05-13** — JobStream v2 path-migration efter Klas-fynd att jobstream.api.jobtechdev.se kör 2.1.1 med `/v2/snapshot` + `/v2/stream?updated-after=` istället för deprecated v1)
- **ADR 0033** Accepted — Migrate CLI-mode-dispatch (oförändrat)
- **ADR 0034** Accepted — DB-role privilege-separation (oförändrat)

### TD-status

- **TD-73** Major → progress note tillagt (punkt 1 + 3 levererade; punkt 2 + 4 kvarstår, trigger flyttat till P8c-start)
- **TD-72** Minor — bootstrap auto-trigga (oförändrat)
- **TD-74** Minor — strikta DML-GRANTs (oförändrat)

Aktiva oförändrade: 18.

### Reviewers INLINE (disciplin-fix från F2-P8a)

| Reviewer | Tidpunkt | Verdict |
|---|---|---|
| dotnet-architect | INNAN kod | Design-skiss approved. IJobSource → per-aggregate Application port. JobStream kräver separat RateLimiter (Polly custom pipeline) — implementerat |
| code-reviewer | EFTER impl, INNAN commit | GO med villkor. 0 Blocker, 1 Major (ADR-acknowledged P8c-scope), 4 Minor — alla in-block-fixade |
| security-auditor | EFTER impl, INNAN commit | APPROVED med villkor. 0 Critical, 0 GDPR-Blocker, 2 Major + 3 Minor — alla in-block-fixade |

CTO-advisor inte invokerad — architect gav entydiga svar (per `feedback_cto_decides_multi_approach`).

### Tester (full svit grön)

- Domain.UnitTests: 218 (oförändrat)
- Application.UnitTests: 258 → **270** (+12: sanitizer 8 + handler 4)
- Architecture.Tests: 33 → **37** (+4: JobSourceLayerTests)
- Api.IntegrationTests: 226+ → **234** (+6: admin auth/flow + WireMock resilience)
- Migrate.UnitTests: 6 (oförändrat)

Totalt backend: ~765 tester gröna.

### Disciplinmissar fångade + fixade

1. NU1902 CVE — OpenTelemetry 1.14.0 → 1.15.3 (CentralPackageTransitivePinning)
2. CA1848/CA1873 LoggerMessage warnings → `[LoggerMessage]` source-gen
3. CA1822 Sanitizer-instance → pure static class + DI-borttagning
4. CA1305 DateTime.ToString → CultureInfo.InvariantCulture
5. CA1859 SanitizeObject return-type → JsonObject
6. xUnit1051 CancellationToken → TestContext.Current.CancellationToken
7. Sanitizer-test allowlist saknade `text` (nested description) → lagt till

### Klas-STOPP-flagga (CTO-rond 1 2026-05-12)

Admin-endpoint exponerar synkron JobTech-call → verifiera resilience-config mot dev INNAN tag-push.

**Steg som väntar Klas-GO:**

1. Skapa tag `v0.2.2-dev` på commit `8c09191`
2. Smoke-test admin-endpoint mot dev — POST `/api/v1/admin/job-ads/sync/platsbanken`
3. Verifiera counts + att job_ads-tabellen får nya rader med External-ref + sanerad RawPayload

### Pending operativt för Klas

- **JobTech-API-key registrering** på apirequest.jobtechdev.se — krävs INNAN admin-trigger fungerar mot riktig JobTech
- (Valfritt) cost_anomaly_alert_email + SES email-verifiering

---

## Nästa session — F2-P8c (Hangfire)

Per ADR 0032 §9 leverans-plan:

- `SyncPlatsbankenStreamJob` (`*/10 * * * *`) — inkrementell update via `IJobSource.StreamChangesAsync`
- `SyncPlatsbankenSnapshotJob` (`0 2 * * *`) — nattlig backfill
- `UpsertExternalJobAdCommand` med `DbUpdateException`-catch-pattern (ADR 0032 §5)
- Removal-handling via `JobAd.Archive` (ADR 0032 §6)
- `JobAdsSyncedDomainEvent` audit-wire (ADR 0032 §8)
- `PurgeStaleRawPayloadsJob` (`0 3 * * *`, 30d retention) — TD-73 punkt 2
- `RawPayloadPurgedDomainEvent` audit-wire
- TD-73 punkt 4 (right-to-erasure-cascade)

Klas-STOPP-flagga: production-schedule = deploy-gränsande.

---

## Tidigare session — F2-P7 + P8a + bootstrap + aggregate-review

Föregående session 2026-05-12 kväll levererade F2-P7 (JobAd-paginering, TD-56
stängd) + F2-P8a (ExternalReference VO + JobAd.Import + EF migration) + F2-P8a.5
(JobbPilot.Migrate CLI-mode-dispatch + Phase E bootstrap) + 4 post-hoc reviewers
+ 4 CTO-ronder. 17 commits, 3 nya ADRs (0032/0033/0034), 1 stängd TD (TD-56),
3 nya TDs (72/73/74). Dev-deploy `v0.2.1-dev` grön.

Se session-log `docs/sessions/2026-05-12-2110-f2-p7-p8a-bootstrap-aggregate-review.md`.
