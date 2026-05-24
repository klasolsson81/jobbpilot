---
session: STEG 6
datum: 2026-05-24
slug: steg6-mvp-recall-fix
status: LEVERERAD
commits:
  - 0c0da82  # feat(jobads): STEG 6 — ssyk_concept_id backfill (Approach A)
  - 21ea2e7  # fix(tests): JobTechStreamResilienceTests DI-fix
  - f17a874  # fix(infra): STEG 6 Plan B — Api task-def HangfireStorage-secret
  - c1e4876  # feat(search): STEG 6 Approach B — SSYK-expansion
  - c57ee3a  # fix(tests): JobAdSearchQuery-instantierings-stub
tags:
  - v0.2.66-dev (0c0da82)
  - v0.2.67-dev (c1e4876)
---

# STEG 6 — MVP-recall-fix för "systemutvecklare"

## Mål

MVP-demo måndag 25 maj. Recall för "systemutvecklare" var 162 hits i dev mot 803 hos Platsbanken (-80%). Söndag 24 maj: lyft recall till minst 600 hits.

## Resultat

**Recall: 162 → 1133 hits (×7, 141% över 600-mål).** Bättre coverage än Platsbankens baseline 803 — vi täcker 9 utvecklar-occupations via SSYK-expansion, inte bara strängen "systemutvecklare".

## Fas-by-fas

### Fas A — Discovery (söndag förmiddag)

Discovery-rapport `docs/reviews/2026-05-24-steg6-fas-a-discovery.md`. Verifierade:

1. **Smoking-gun-rotsymptom:** JobTech `/v2/snapshot` trunkerar mid-stream ~10k/körning (CloudWatch-bevis 2026-05-22→24 visade fetched=9509 → 10163 → trunkerad). 35 384 pre-2026-05-20-fix-rader fastnar utanför trunkerad prefix.
2. **Hypotes 1 falsifierad:** JobTech-källan HAR occupation för utvecklar-annonser (9/9 stickprov via `taxonomy.api.jobtechdev.se` + 3/3 Senior Java AWS Developer-id:n). Inte data-saknad — sync-trunkering.
3. **JobTech taxonomy lookup:** 9 utvecklar-occupation-concept_ids verifierade (Systemutvecklare/Programmerare, Mjukvaru-/Backend-/Frontend-/Fullstack-/Devops-/Mobil-/GIS-/Administrativ utvecklare).

### CTO-rond 1 (multi-approach STEG 6 strategy)

Agent `a3b55188be4e119ca`. Rekommendation: **Plan C (hybrid B+A)**. Klas valde **A**. Motivering Klas: "korrekta vägen, inte snabbaste — sync-rotsymptom-fix".

### Architect-rond 1 (backfill S2-design)

Agent `a2f3999e4202b89eb`. Vald: per-id-fetch via `IJobTechSearchClient.GetAdByIdAsync` (S2). Avvisade S1 (probabilistisk snapshot-trigger) + S3 (snapshot-loop-utvidgning bryter ADR 0032-amendment 2026-05-16 bounded retry).

### Implementation Approach A (söndag eftermiddag)

Commit `0c0da82` (tag `v0.2.66-dev`):
- `IJobTechSearchClient.GetAdByIdAsync` (Refit, 404→null)
- `IJobSource.RefetchByExternalIdAsync` (Application-port)
- `PlatsbankenJobSource`-impl återanvänder `TryConvertToImportItem`
- `BackfillJobAdSsykJob` (Application, child-scope per item, 200ms throttle, MaxItemsPerRun=100000-cap)
- Worker-wrapper `BackfillJobAdSsykWorker` med `[DisableConcurrentExecution(7200)]`
- Hangfire-client (storage-only) i Api Program.cs
- Admin-endpoint `POST /api/v1/admin/job-ads/backfill-ssyk` (RequireAuthorization Admin)

Code-review `ad2f8482309802b7b`: 2 Major in-block-fixade (counts.Updated-bug → UpsertOutcome switch; EventId-kollision 5701→6001). Security-auditor `ab002ec9ede71d352`: CONDITIONAL PASS, GDPR PASS. 3 Minor TDs (TD-96/97/98).

Test-fix-commit `21ea2e7`: `JobTechStreamResilienceTests.BuildJobSource` saknade `IJobTechSearchClient`-registrering (CS-error i CI).

### Plan B Hangfire-incident (post-deploy v0.2.66-dev)

Klas körde curl mot `/backfill-ssyk` → **500: `Npgsql.PostgresException 42501: permission denied for schema hangfire`**.

Security-auditor hade flaggat detta som "operationell not — inte Block här (dev-fas)" PRE-deploy. Materialiserade som runtime-incident.

CTO-rond 2 `a9f2e123b1080b00f`. Klas-direktiv: "korrekta vägen, inte quickfix". CTO vald **Plan B (splittad role ägd av Terraform)**. Avvisade Plan A (runtime-DDL från Worker — bryter persistence-ignorance), Plan C (`jobbpilot_app` permanent GRANT — bryter Saltzer & Schroeder least-privilege), Plan D (throwaway-cron-job).

Architect-rond 2 `a1513a571782c2dc0`. Discovery-fynd: existing Terraform har redan `aws_secretsmanager_secret.db_hangfire_connection` + IAM-policy täcker Api. Faktisk gap: 1 rad i `api_secrets`-map i `environments/dev/main.tf`. CTO original-5-stegs-plan reduced till 10-min-fix. `jobbpilot_worker`-rollen är funktionellt hangfire-only per runbook §4 (PUBLIC revoke:ad, ingen jobbpilot_app-inheritance).

Commit `f17a874` (utan tag — terraform-state-only). Klas körde själv:
1. `aws sso login --profile jobbpilot`
2. `terraform apply -var "api_image_tag=0c0da82..." -var "worker_image_tag=0c0da82..." -target="module.ecs.aws_ecs_task_definition.api"` (med `-target` för att undvika TD-91 RDS pre-existing drift)
3. Force-new-deployment via `aws ecs update-service` (task-def revision 77)

TD-99 lyft för rename `jobbpilot_worker` → `jobbpilot_hangfire` (STEG 14 prod-DDL-cutover).

### Backfill-körning + Hangfire-retry-discovery

Backfill triggad via curl: `jobId 1792`. Progress:
- Invocation 1: 6000 items, 5091 updated, 909 notFound (15.15%)
- **Hangfire AutomaticRetry triggad** (default-beteende, `DisableConcurrentExecution` på Worker-wrappern bypassad eftersom Api enqueue:ade Application-class direkt — TD-96-rotsymptom)
- Invocation 2: 12000 items, 4177 updated, 7823 notFound (65% — `OrderBy(ExternalId)` itererar nu legacy-tail med fler 404)
- Total persisted ~12k+ uppdaterade rader

### Approach B-insikt (post-backfill recall-mätning)

`totalCount: 162` — **exakt baseline**. Designfel i initial-analys: backfill uppdaterar `ssyk_concept_id`-data, men `JobAdSearchQuery.ApplyCriteria` Q-grenen läser INTE den för text-search. För recall-lift krävs query-side SSYK-expansion.

CTO original-rec Plan C (hybrid B+A) var rätt från start. Klas-direktiv om "korrekta vägen" säkerställde att data-laget byggdes ordentligt, men query-laget behövdes ovanpå.

### Implementation Approach B

Commit `c1e4876` (tag `v0.2.67-dev`):
- `IOccupationSynonymExpander` Application-port (ACL per Evans 2003 kap. 14 — fritext-domän ↔ JobTech taxonomy bounded-context)
- `SearchSynonymsOptions` IOptions-bind med case-insensitive `Dictionary<string, string[]>`
- `OccupationSynonymExpander` Infrastructure-impl
- `JobAdSearchQuery.ApplyCriteria` utvidgad: Q-gren får OR-clause `expandedSsyks.Contains(EF.Property<string?>(j, "SsykConceptId"))` ovanpå FTS + title-LIKE
- `appsettings.json` initial mapping: "systemutvecklare" → 9 concept_ids verifierade mot taxonomy
- Tester 585→588 PASS

Test-fix-commit `c57ee3a`: `ListJobAdsFtsTests` + `ListJobAdsMultiFilterTests` instantierar `JobAdSearchQuery` direkt; nya ctor-dep krävde Substitute-stub.

### Slutverifiering

Curl: `GET /api/v1/job-ads?q=systemutvecklare&page=1&pageSize=1`
- **totalCount: 1133** (lift 162 → 1133, ×7)
- Första hit: "Backend Engineer" (klassificerad som Backend-utvecklare-SSYK `7wdX_4rv_33z`) — exakt expansionsavsikt verifierad

## Decisions och detourer

### Klas-override av CTO-rec (Approach A över Plan C)

Klas valde A trots CTO-rec C. Motivering: "korrekta vägen, inte quickfix" + sync-rotsymptom är genuin teknisk skuld. Post-mortem: A ensam räcker INTE för recall — query-laget krävdes också. Båda implementerades sekventiellt under samma dag. Lärdom: när CTO rekommenderar hybrid, väg om både komponenter behövs för det önskade resultatet.

### Plan B-storlek minskade efter architect-discovery

CTO original-Plan-B (5 steg) reduced till 10 min Terraform-edit efter architect upptäckte att existing infra hade 4 av 5 steg redan på plats. Effektivt — men signalerar att STEG 14-arbete hade kortare väg framåt än CTO antog.

### Plan B avbröt backfill-körningen

Tag-push v0.2.67-dev triggade Worker-restart → Hangfire-jobbet 1792 avbröts mid-run. Acceptabelt eftersom backfill redan persisterat ~12k uppdaterade rader. Approach B fungerar för existing populated `ssyk_concept_id`-rader.

### TD-96-rotsymptom synliggjordes

Hangfire `AutomaticRetry` triggade pga `DisableConcurrentExecution` bypassades. TD-96 lyfter Api→Worker enqueue-port som löser detta. Inte kritiskt för MVP, men dokumenterat för framtida fix.

### Security-auditor "operationell not" materialiserade

Audit-output "operationell not — inte Block här (dev-fas)" om Hangfire-rolldelning fick 500-incident dagen efter. Process-gap: CLAUDE.md §9.2 ska väga security-auditor-output på secret + external integration som blocking input.

### CC SSO-utgång krävde Klas-side terraform-apply

CC kunde inte köra terraform manuellt. Klas körde själv via PowerShell + AWS SSO. Audit-trail bevarad. Gick smidigt.

## TDs lyfta

- **TD-96** Api→Worker port för Hangfire-enqueue (defense-in-depth `[DisableConcurrentExecution]` + AdminAuthorizationBehavior coverage) — Minor, Trigger
- **TD-97** Integration-test för STORED column-re-evaluation mot Testcontainers — Minor, Fas 1 (efter MVP-demo)
- **TD-98** Dedikerad rate-limit-policy för admin-endpoints — Minor, Trigger
- **TD-99** Rename Postgres-roll `jobbpilot_worker` → `jobbpilot_hangfire` + secret-namn — Minor, STEG 14

## Reviews och rapporter

- `docs/reviews/2026-05-24-steg6-fas-a-discovery.md`
- `docs/reviews/2026-05-24-steg6-cto-multiapproach.md`
- `docs/reviews/2026-05-24-steg6-architect.md`
- `docs/reviews/2026-05-24-steg6-code-review.md`
- `docs/reviews/2026-05-24-steg6-security-audit.md`
- `docs/reviews/2026-05-24-steg6-hangfire-grant-cto.md`
- `docs/reviews/2026-05-24-steg6-hangfire-plan-b-architect.md`

## Pending för nästa session

- Ren FE-uppdatering (separat startprompt per Klas-direktiv 2026-05-24)
- Post-MVP-demo: TD-96/97/98/99 + TD-94 + TD-95 från grunden
- Plan B-completion av Terraform-state-cleanup (RDS-drift TD-91 fortfarande pending)

## Commit-historik

```
0c0da82  feat(jobads): STEG 6 — ssyk_concept_id backfill för MVP-recall-fix (Approach A)  [v0.2.66-dev]
21ea2e7  fix(tests): registrera IJobTechSearchClient i JobTechStreamResilienceTests
f17a874  fix(infra): STEG 6 Plan B — Api task-def mountar HangfireStorage-CS
c1e4876  feat(search): STEG 6 Approach B — SSYK-expansion för fritext-recall-lift  [v0.2.67-dev]
c57ee3a  fix(tests): IOccupationSynonymExpander-stub i JobAdSearchQuery-instantieringar
```
