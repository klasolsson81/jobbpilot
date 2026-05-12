---
session: F2-P7 + P8a + P8a.5 + bootstrap + aggregate-review komplett, dev-deploy grön
datum: 2026-05-12
slug: f2-p7-p8a-bootstrap-aggregate-review
status: Klar
commits:
  - 0fc4b76  # F2-P7 feat(jobads): JobAd-paginering med PagedResult (TD-56 stängd)
  - 6bdce04  # docs(adr): ADR 0032 utkast — JobTech-integration
  - 06ee2b3  # docs(adr): ADR 0032 Proposed → Accepted
  - c5aa089  # F2-P8a feat(jobads): ExternalReference VO + JobAd.Import + EF migration
  - 4bb91d8  # F2-P8a.5 feat(migrate): CLI-mode-dispatch + Phase E (ADR 0033)
  - ff136ad  # F2-P8a.5c feat(deploy): auto-trigga schema-task i deploy-dev.yml
  - 0fe0ce6  # fix(migrate): Dockerfile saknar Infrastructure-projekt-context
  - ad7988f  # fix(infra): ecs:DescribeTasks task-ARN-pattern
  - f69308f  # fix(migrate): Dockerfile aspnet-runtime
  - daab6ec  # fix(deploy): containerOverrides.command mode-arg
  - 2c9232a  # fix(migrate): Dockerfile RDS-CA-bundle
  - b1f50bf  # F2-P8a.5e feat(migrate): bootstrap-mode + ADR 0034
  - e228b7f  # refactor(infra): MigrationsOptionsFactory single source of truth (DRY)
  - baf901b  # fix(api): auth-gate + sort-default-explicit (review-fynd)
  - acc6ff3  # fix(migrate): Bootstrap re-fetch + extract types + password-local
  - bef983c  # fix(infra): EcsReadOurCluster cluster-condition
  - 24a7135  # docs(tech-debt+adr): TD-13 utökas + TD-72/73/74 + ADR 0032 §8-amendment
deploy_tag: v0.2.1-dev (live på dev.jobbpilot.se)
---

# Session 2026-05-12 (kväll) — F2-P7 + P8a + bootstrap + aggregate-review komplett

## Mål

Leverera F2-JobTech-features (P7 paginering + P8 JobTech-integration) per
föregående sessions STARTPROMPT-FAS2-P7-P8.md. Stort scope — slutade bli
17 commits + 4 CTO-ronder + 4 parallella post-hoc reviews.

## Vad som blev klart

**Feature-leveranser:**

| Batch | Innehåll |
|---|---|
| **F2-P7** | JobAd-paginering med `PagedResult<JobAdDto>` + `JobAdSortBy`-enum (whitelist) + `ListJobAdsQueryValidator`. TD-56 stängd. |
| **F2-P8a** | `ExternalReference` value object + `JobAd.Import`-factory + `JobAd.UpdateFromSource` + `JobAdImportedDomainEvent` + EF migration (external_source/external_id/raw_payload + UNIQUE-index filter). |
| **F2-P8a.5** | `JobbPilot.Migrate` CLI-mode-dispatch (`init`/`bootstrap`/`schema`) + Phase E (EF MigrateAsync) + ADR 0033 + auto-trigga schema i deploy-dev.yml. |
| **F2-P8a.5e** | Bootstrap-mode (master-creds applicerar Identity-migrations + skapar identity-schema + GRANTar jobbpilot_app least-privilege) + ADR 0034. |
| **Review-fixes** | 4 parallella reviewers (code/architect/security/db-migration) + CTO-rond 4 → 4 fix-commits (auth-gate, Bootstrap re-fetch, EcsReadOurCluster cluster-condition, TD-13 + TD-72/73/74 + ADR 0032 §8-amendment). |

**ADRs skapade (3 stycken):**
- **ADR 0032** — JobTech-integration: resilience-stack, dedup-strategi, sync-flöde (+ §8-amendment för PII-stripping)
- **ADR 0033** — JobbPilot.Migrate CLI-mode-dispatch (+ amendment för auto-trigga i deploy-dev.yml)
- **ADR 0034** — DB-role privilege-separation: runtime vs migration-time creds (Saltzer/Schroeder 1975)

**Stängda TDs:** TD-56 (paginering)

**Nya TDs:**
- **TD-72** (Minor, Trigger): Auto-trigga Migrate bootstrap-mode i deploy-dev.yml vid Identity-schema-change. Per CTO-rond 4 (Variant B — manuell tills trigger).
- **TD-73** (Major, Fas 2 P8c-gating): JobTech raw_payload PII-stripping + retention. Per security-auditor Sec-Major-1.
- **TD-74** (Minor, Fas 2 opportunistic): Strikta DML-GRANTs istället för GRANT ALL. Per security-auditor Sec-Minor-2.

## CTO-konvergens (4 ronder)

| Rond | Beslut | Fil |
|---|---|---|
| 1 | F2-P7 (paginering) + F2-P8 (JobTech) multi-approach: A1/B1/C2/D1/E1 + A1/B3/C2/D2/E1/F3/G1/H3/I3, S1 sekvens | `docs/reviews/2026-05-12-f2-p7-p8-cto.md` |
| 2 | F2-P8a.5 CLI-mode-dispatch: Variant β-modifierad (default-less `init`/`schema`) | ADR 0033 |
| 3 | F2-P8a.5e Identity-context permission: bootstrap-mode med master-creds | ADR 0034 |
| 4 | F2-P8a-aggregate-review fynd A2 (bootstrap auto-trigga): Variant B (TD-72) | `docs/reviews/2026-05-12-f2-p7-p8a-aggregate.md` |

## Aggregate-review

Klas-disciplinpåminnelse: code-reviewer + security-auditor borde invokerats
inline vid IAM-utvidgningar + nya jsonb-PII-kolumner. Post-hoc audit
genomförd parallellt av 4 reviewers:

| Reviewer | Blocker | Major | Minor | Verdict |
|---|---|---|---|---|
| code-reviewer | 0 | 0 | 9 | GO — "Mastercard-snitt, ADR-template-kvalitet" |
| dotnet-architect | 0 | 2 (A1, A2) | 3 | GO efter A1-fix |
| security-auditor | 0 (no Critical) | 2 | 3 | Approved — inga GDPR-Blockers |
| db-migration-writer | 0 | 0 | 2 | GO — funktionellt korrekt |

Granskningstrail i `docs/reviews/2026-05-12-f2-p7-p8a-aggregate.md`.

## Disciplinmissar fångade + fixade

Sex iterations av Migrate-fixes innan deploy gick grön. Lärdom: när jag
lägger till transitive ProjectReferences till ett containeriserat projekt
måste jag verifiera (a) Dockerfile build-context, (b) runtime-image-bas,
(c) IAM-yta för alla AWS-API-anrop i workflow.

1. Migrate Dockerfile saknade Domain/Application/Infrastructure i build-context → `0fe0ce6`
2. IAM `ecs:DescribeTasks` saknade task-ARN-pattern → `ad7988f`
3. Migrate Dockerfile runtime-image saknade ASP.NET-framework (transitiva deps) → `f69308f`
4. `containerOverrides.command` skickade hela kedjan istället för bara mode-arg → `daab6ec`
5. Migrate Dockerfile saknade RDS-CA-bundle (TD-38-pattern) → `2c9232a`
6. RunBootstrapAsync matchade inte DesignTimeIdentityDbContextFactory-konfig → DRY-fix `e228b7f` (Klas-discipline-feedback: extrahera helper istället för copy-paste)

## Web-search räddade scope

Två kritiska fakta-verifieringar via web-search (CLAUDE.md §9.5):

1. **Npgsql #1770 + #1551 + PostgreSQL CREATE SCHEMA docs**: `MigrateAsync` kräver `CREATE ON DATABASE`-privilege oavsett om schemat finns. CTO:s antagande "schemat finns → ingen privilege" var fel. → Bootstrap-mode-design (master-creds för Identity).
2. **AWS OIDC GitHub Actions thumbprint** (2023/2025-update): thumbprint är legacy, AWS validerar mot root CAs. → Drift-cleanup OK vid IAM-apply.

## Akut path — dev-RDS skema-state

**Stor anomali identifierad:** dev-RDS public-schema var **helt tomt** innan
F2-P8a.5d (10 EF-migrations applicerades, inte bara F2-P0b + F2-P8a).
Tidigare deploys-success var false-positive — ALB target-group `/api/ready`
slog inte mot Identity-tabellerna. F2-P0b-glömskan-mönstret fångat
mekaniskt nu via Phase E auto-trigga.

**Bootstrap manuellt körd** efter v0.2.0-dev-tag-build:
- Step 1: identity-schema + GRANTs (idempotent, success)
- Step 2: 2 Identity-migrations applicerade (`InitialIdentity`, `AddAuthProviderToUser`)

## Live på dev

`HTTP/1.1 200 OK` från `https://dev.jobbpilot.se/api/ready` med tag `v0.2.1-dev`
(skapad efter tag-deploy v0.2.0-dev triggade workflow_dispatch som failade
på OIDC trust-policy — ny patch-tag triggade ren tag-push-flow).

**HEAD vid session-end:** `24a7135`

## Tester (full svit grön)

| Suite | Antes → Efter |
|---|---|
| Domain.UnitTests | 202 → 218 (+16: ExternalReference + JobAd.Import/UpdateFromSource) |
| Application.UnitTests | 249 → 258 (+9: paginering + sort + validator) |
| Architecture.Tests | 32 → 33 (+1: `ListJobAdsQuery_returns_PagedResult`) |
| Api.IntegrationTests | 223 → 226+ (+3 paginering, JobAds-tester uppdaterade för auth-gate) |
| Migrate.UnitTests | 6 (oförändrat) |

## TD-status

- **Stängda:** TD-56 (paginering). Aktiva: 16 → **15** (efter TD-56-stängning) → **18** efter TD-72/73/74-lyft.
- **Nya:** TD-72 (Minor, Trigger), TD-73 (Major, P8c-gating), TD-74 (Minor, Phase A-touch).
- **Utökade:** TD-13 (raw_payload tillagd i berörda-kolumner-listan).

## Nästa session — F2-P8b

Per ADR 0032 leverans-plan: P8b — Infrastructure-leverans:
- `IJobTechSearchClient` via Refit (klassisk REST/JSON)
- `IJobTechStreamClient` typed-client (NDJSON polymorft event-schema)
- `PlatsbankenJobSource : IJobSource`
- `Microsoft.Extensions.Http.Resilience` + `AddStandardResilienceHandler`
- `JobTechOptions` (appsettings-binding)
- Admin-trigger-endpoint `POST /api/v1/admin/job-ads/sync/platsbanken`
- WireMock-baserade integration-tester
- **TD-73-arbete (PII-stripping + retention) som blockerar P8c**: `JobTechPayloadSanitizer` + allowlist-design

Klas-STOPP-flagga per CTO: admin-endpoint exponerar synkron JobTech-call,
verifiera resilience-config mot dev innan tag-push.

## Tidsuppskattning

~6h CC-tid effektivt (17 commits + 4 CTO-ronder + 4 parallella reviews +
6 deploy-iterations). Disciplin-friktion höll tempot nere — sex
Migrate-Dockerfile-iterations var min disciplinmiss (matchade inte
Worker-Dockerfile-mönstret från start).
