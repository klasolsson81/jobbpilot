---
session: v0.2-prod-tag-readiness — smoke-test verifierad + CTO-rond + checklist
datum: 2026-05-13
slug: v02-prod-tag-readiness
status: Docs-only-session. Audit-wire smoke-test VERIFIERAD live (3 audit-INSERTs efter v0.2.4-dev). v0.2-prod-launch-checklist skapad. senior-cto-advisor 5 entydiga beslut. TD-77 + TD-78 lyfta som Fas 8. **Pending Klas-GO** för 3 in-block-fix-leveranser (CloudWatch-alarm × 2 + RDS-backup-bump) FÖRE v0.2-tag-push.
commits:
  - (pending push)  # docs: v0.2-prod-launch-checklist + TD-77 + TD-78
deploy_tag: v0.2.4-dev (oförändrad, denna session påverkar inte prod-yta)
---

# Session 2026-05-13 (förmiddag/eftermiddag) — v0.2-prod-tag-readiness

## Mål

Per Klas-prompt: prod-launch-checklist + ev. CloudWatch-alarms + smoke-test-verifiering av nyligen levererad TD-73 audit-wire α.

Klas-direktiv: "non-stop arbete, STOPP bara efter PR" + "minimera STOPP, multi-approach → CTO".

## Vad blev klart

### Steg 1 — Smoke-test verifiering av audit-wire (KLAR)

Verifiering via CloudWatch Logs Insights mot `/aws/ecs/jobbpilot-dev/worker`. Tre konsekutiva stream-cron-tick efter `v0.2.4-dev`-deploy bekräftade `SystemEventAuditor`-flödet:

| Tid (UTC) | Stream-result | audit_log-trail |
|---|---|---|
| 08:21:55 | fetched=1029, added=72, errors=0 | `SELECT FROM audit_log AS a` (idempotens-check) → `INSERT INTO audit_log (… payload …)` |
| 08:30:47 | fetched=1076, added=84, errors=0 | Samma mönster |
| 08:40:41 | (cron-tick pågående vid query-tid) | Samma mönster |

**0 EventId 5602** (Critical audit failure) i hela 2h-fönstret. ADR 0035-implementationen funkar i prod-flöde.

### Steg 2 — `docs/runbooks/v0.2-prod-launch-checklist.md` (KLAR)

Komplett checklist över 10 områden:
1. GDPR / Compliance (12 items — alla ☑ utom TD-13 + TD-27 defer)
2. CloudWatch alarms (7 items — 2 ☑, 2 in-block-fix, 2 TD, 1 N/A)
3. Backup-strategi (5 items — 3 ☑, 1 backup-bump pending, 1 TD-defer)
4. Health probes + ALB (5 items — alla ☑)
5. Rate-limiting + DoS-skydd (5 items — alla ☑ utom TD-52 Fas 6 defer)
6. Secrets-hygien (4 items — alla ☑ utom TD-74 opportunistisk)
7. Operativa pending (5 items — Klas-bedömning krävs)
8. TD-status snapshot
9. **In-block-fix-batch FÖRE v0.2-tag (3 leveranser)** — pending Klas-GO
10. Sluttillstånd-spec för v0.2-tag

### Steg 3 — CTO-rond 2026-05-13 (KLAR)

5 strategiska frågor till `senior-cto-advisor`. Alla beslut entydigt motiverade mot principer (Martin/Evans/Fowler/Beck/Saltzer-Schroeder/Humble-Farley/Ford-Parsons-Kua/EDPB/GDPR-Art-17/30).

| # | Fråga | CTO-beslut |
|---|---|---|
| Q1 | v0.2-definition (a/b/c)? | **(c)** Första prod-deploy-triggande tag oavsett feature-completeness |
| Q2 | BUILD.md §14.4-alerts launch-blocker? | JobTech-sync = in-block; 5xx + DB CPU = TD-77/TD-78 Fas 8 |
| Q3 | SystemEventAuditor failure-alarm launch-blocker? | **In-block-fix FÖRE tag** (ADR 0035 §6 egen leveransspec) |
| Q4 | RDS backup-retention för prod? | **14d** (industry-common, EDPB-verifierat) |
| Q5 | TD-13 launch-blocker? | **Defer Fas 2** (EDPB CEF 2025 verifierar standard practice räcker) |

CTO-Klas-STOPP-flagga: **JA**, FÖRE batch-leverans (v0.2-definition är strategisk + prod-Terraform-state + tag-push) — entydigt CLAUDE.md §9.6 punkt 5-territorium.

### TDs lyfta (per CTO-triage)

- **TD-77** — Backend 5xx-rate-alarm (1% över 5 min), Minor, Fas 8 Klass-launch
- **TD-78** — DB CPU > 80% i 10 min-alarm, Minor, Fas 8 Klass-launch

Båda mot §9.6-kriterier: **annan fas** (Fas 8 Klass-launch, SLA-relevant först vid multi-user-volym). YAGNI mot 1-user-volym i v0.2-prod-launch-fas.

## Web-search-källor (CLAUDE.md §9.5)

Verifierat 2026-05-13:

- **[AWS RDS Backup Retention](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_WorkingWithAutomatedBackups.BackupRetention.html)** — default 7d console / 1d API, max 35d. Dev TF idag: 7d. Prod-rekommendation: 14d.
- **[EDPB CEF 2025 Right-to-erasure-rapport (PDF, 2026-02)](https://www.edpb.europa.eu/system/files/2026-02/edpb_cef-report_2025_right-to-erasure_en.pdf)** — automatic overwrite cycles + live-radering acceptabelt; crypto-erasure inte krav. TD-13 defer Fas 2 verifierad.
- **[Terraform aws_cloudwatch_log_metric_filter](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/cloudwatch_log_metric_filter)** — pattern + metric_transformation block. Befintlig precedens via TD-68-modul.
- **[Terraform aws_cloudwatch_metric_alarm](https://registry.terraform.io/providers/hashicorp/aws/latest/docs/resources/cloudwatch_metric_alarm)** — provider v6.30 stable. Befintlig modul redan kompatibel.

## Discovery (CloudWatch-state idag)

- **Log groups (alla 30d retention per ADR 0024 D7):** api, worker, ecs-exec, migrate, RDS-postgresql, RDS-upgrade
- **CloudWatch alarms (2 stycken):**
  - `jobbpilot-dev-api-log-pipeline-health` (Logs/IncomingLogEvents)
  - `jobbpilot-dev-failed-access-anomaly` (JobbPilot/Security/FailedAccessAttempts)
- **Metric filters (1 styck):** `jobbpilot-dev-failed-access-attempts` på api-loggruppen
- **Terraform-modul:** `modules/cloudwatch_security_alarms` (TD-68-precedens) — utbyggnadsfärdig för §9.1 + §9.2

## Pending Klas-GO (in-block-fix-batch)

Per CTO-rond + `docs/runbooks/v0.2-prod-launch-checklist.md` §9:

1. **CloudWatch-alarm: JobTech-sync 3 consecutive failures**
   - Terraform: utöka `modules/cloudwatch_security_alarms` eller ny modul
   - Metric filter: Worker-error-pattern (`errors > 0` i sync-completion-log)
   - Alarm: `Sum >= 3` över `30 min` (3 cron-tick × 10 min)
2. **CloudWatch-alarm: SystemEventAuditor failure (EventId 5602)**
   - Metric filter: `level="Critical" AND EventId.Id=5602` på api/worker-log-groups
   - Alarm: `Sum >= 1` över `5 min`
   - Stänger ADR 0035 §6-gap (kongruens-bug om tag:as utan)
3. **RDS backup-retention: 7d → 14d (prod-tfvars)**
   - `infra/terraform/environments/prod/terraform.tfvars` ändras
   - Dev oförändrad (7d räcker för dev-lifecycle)
   - Dokumentera i v0.2-checklist + ev. ADR-cross-ref

**Scope-uppskattning:** 2-3 commits, ~3-4h CC-tid. Klas-STOPP-territorium per CLAUDE.md §9.6 punkt 5.

## Lärdomar

- **Smoke-test via CloudWatch Logs Insights är effektivt** för audit-wire-verifikation utan att kräva psql-access till dev-RDS. EF Core-genererade SQL-statements ("INSERT INTO audit_log…") räcker som bevis när idempotens-check + insert-mönstret är arch-låst via SystemEventAuditor.
- **CTO-rond utan föregående Klas-STOPP** fungerar väl när scope är *bedömning* (gap-triage) snarare än kod-skifte. CTO levererar entydigt beslut + Klas-STOPP-rekommendation där det behövs.
- **EDPB CEF 2025-rapporten (2026-02)** är konkret verifiering att TD-13 (crypto-erasure) inte är v0.2-launch-blocker. Web-search vid GDPR-tradeoffs > gissning från training data per CLAUDE.md §9.5.
- **Audit-wire LIVE-verifikation är värdefull granskningstrail.** Idempotens-check + insert-mönstret bekräftades fungera per Hangfire-retry-design från ADR 0035 §5.

## Pending operativt för Klas (sedan tidigare)

- AWS SSO-token-livslängd
- JobTech-API-key registrering
- Frontend-deploy till Vercel (kommer i v0.2.x-patch efter v0.2)
- BUILD.md §9.1 sync mot ADR 0032 §3 — Klas-instruktion krävs

## Nästa session — alternativ

Beroende på Klas-GO för in-block-fix-batch:

1. **Klas GO (full batch):** leverera 3 alarm- + backup-leveranser → v0.2-tag-readiness komplett → tag-push efter explicit Klas-GO
2. **Klas GO (subset):** bara CloudWatch-alarms × 2 (defer backup-bump till separat batch)
3. **Klas väntar:** docs är redan committade som granskningstrail; nästa session kan starta med F2-P9 (search/filter-yta) eller Frontend-deploy

## Tidsuppskattning

~1.5h CC-tid effektivt (docs-only session: 1 ny runbook + 2 nya TD-blocks + current-work-uppdatering + session-log + CTO-rond + 4 web-searches + CloudWatch-discovery + audit-wire-verifikation). Inga build-cyklar, inga tester körda (docs påverkar ingen test-yta).

**HEAD vid session-end:** `6914990` + 1 docs-commit kommande.
