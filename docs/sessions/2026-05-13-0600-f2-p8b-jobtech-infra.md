---
session: F2-P8b — JobTech Infrastructure-leverans (Refit + Resilience + admin-trigger)
datum: 2026-05-13
slug: f2-p8b-jobtech-infra
status: Klar — deploy live på v0.2.2.1-dev, E2E-smoke flyttat till P8c per CTO-rond 5
commits:
  - 8c09191  # feat(jobads): F2-P8b — JobTech Infrastructure + admin-trigger-endpoint
  - 037e0e8  # docs: session-end 2026-05-13 — F2-P8b komplett + TD-73 partial-progress
  - 8d89ded  # fix(jobads): F2-P8b — JobStream v2 path-migration (klas-fynd)
  - 139a85e  # fix(jobads): F2-P8b — JobStream v2-shape (webpage_url + PII-skydd)
  - 03f8207  # fix(build): global.json rollForward latestPatch → latestFeature
deploy_tag: v0.2.2.1-dev (live på dev.jobbpilot.se)
---

# Session 2026-05-13 (efterm./natt) — F2-P8b JobTech Infrastructure

## Mål

Leverera ADR 0032 §9 P8b-batch:
- IJobSource Application-port + DTOs
- IJobTechSearchClient via Refit 10.x
- IJobTechStreamClient typed-client
- PlatsbankenJobSource som IJobSource
- Microsoft.Extensions.Http.Resilience + AddStandardResilienceHandler
- Custom Polly-pipeline för JobStream med RateLimiter (1 req/min)
- JobTechOptions + ValidateOnStart
- JobTechPayloadSanitizer (TD-73 punkt 1)
- Admin-trigger-endpoint POST /api/v1/admin/job-ads/sync/platsbanken
- WireMock integration-tester (503 retry, polymorft event-schema)

Klas-disciplinpåminnelse vid session-start: **reviewers INLINE per CLAUDE.md §9.2,
inte post-hoc** (referens: F2-P8a-disciplinmiss där 4 reviewers kördes efter
merge). Etablerat i denna session som standard.

## Vad blev klart

| Område | Innehåll |
|---|---|
| **Application port** | `IJobSource` i `JobAds/Abstractions/` + `JobAdSnapshot`/`JobAdChange`/`JobAdUpsert`/`JobAdRemoval` (LSP-diskriminerad union) + `JobAdImportItem` transport-DTO |
| **Infrastructure** | `JobTechOptions`, `JobTechPayloadSanitizer` (pure static allowlist), `JobTechSearchResponse` (wire-DTOs internal), `IJobTechSearchClient` (internal Refit), `IJobTechStreamClient` + `JobTechStreamClient` (internal typed), `PlatsbankenJobSource` (internal sealed partial — LoggerMessage source-gen) |
| **DI** | `AddJobSources`-extension i `DependencyInjection.cs`. Search via `AddStandardResilienceHandler` (3 retry expo + CB 5/5min). Stream via custom `AddResilienceHandler` (RateLimiter → Retry → CB) med process-statisk `FixedWindowRateLimiter(1, 1 min)`. `MaxResponseContentBufferSize=500 MB` (sec-Min-3 DoS-cap) |
| **Application command** | `SyncPlatsbankenSnapshotCommand` (IAdminRequest) + `SyncPlatsbankenSnapshotResult` + handler (bulk-fetch + in-memory split — race-skydd via DbUpdateException är medvetet P8c-scope per ADR 0032 §5) |
| **Api** | `AdminJobAdsEndpoints.MapAdminJobAdsEndpoints` mappad i `Program.cs:256`. `RequireAuthorization(AuthorizationPolicies.Admin)` |
| **Tester** | Sanitizer 8 + Handler 4 + Architecture 4 + Api.Integration 6 (3 admin + 2 stream-resilience + 1 stub) |
| **GDPR-docs** | `docs/runbooks/gdpr-processing-register.md` skapad med JobTech-entry (TD-73 punkt 3) |

**Commits:** 1 (`8c09191`) — 24 filer, 1703 insertions.

## ADR-status

- **ADR 0032** Accepted — P8a + P8b § levererade. §8-amendment punkt 1 (sanitizer)
  + punkt 3 (processing-register) levererade i denna batch. Punkt 2 (raw_payload
  retention via Hangfire) + punkt 4 (right-to-erasure) kvarstår för P8c.

## Tester (full svit grön)

| Suite | Före → Efter |
|---|---|
| Domain.UnitTests | 218 → 218 (oförändrat) |
| Application.UnitTests | 258 → 270 (+12: sanitizer 8 + handler 4) |
| Architecture.Tests | 33 → 37 (+4: JobSourceLayerTests) |
| Api.IntegrationTests | 226+ → 234 (+6: admin auth/flow + WireMock resilience) |
| Migrate.UnitTests | 6 (oförändrat) |

Totalt backend: ~765 tester gröna.

## Reviewers INLINE (CLAUDE.md §9.2 — disciplin-fix från F2-P8a)

| Reviewer | Tidpunkt | Fynd | Resolution |
|---|---|---|---|
| dotnet-architect | INNAN kod | Design-skiss approved. Två viktiga noter: (1) IJobSource ska ligga i `Application/JobAds/Abstractions/` per-aggregate, (2) JobStream kräver separat RateLimiter — `AddStandardResilienceHandler` täcker inte proaktiv throttling | Båda implementerade |
| code-reviewer | EFTER impl, INNAN commit | 0 Blocker, 1 Major (race-skydd — ADR-acknowledged P8c-scope), 4 Minor (allowlist-keys, rate-limiter docstring, PublishedAt-fallback, integration-test för upsert) | Alla Minor fixade in-block |
| security-auditor | EFTER impl, INNAN commit | 0 Critical, 0 GDPR-Blocker, 2 Major (Description-fri-text SECURITY-NOTE, processing-register-fil), 3 Minor (mailto-URL-guard, API-key-logging-kommentar, MaxResponseContentBufferSize) | Alla Major + Minor fixade in-block |

CTO-advisor ej invokerad — architect gav entydiga svar, inga multi-approach-val
kvarstod (per CLAUDE.md §9.6 + memory `feedback_cto_decides_multi_approach`).

## Disciplinmissar fångade + fixade

1. **NU1902 (CVE NU1902 OpenTelemetry.Api 1.14.0)** — WireMock.Net 1.25.0 drog
   in OpenTelemetry transitivt. Fix: pinning till 1.15.3 i `Directory.Packages.props`
   via `CentralPackageTransitivePinningEnabled` (web-verifierat 2026-05-12 enligt
   GHSA-g94r-2vxg-569j).
2. **CA1848/CA1873 LoggerMessage warnings** — `LogInformation/LogDebug` med
   parametrar bryter `TreatWarningsAsErrors`. Fix: `[LoggerMessage]`
   source-gen-pattern (samma som `IdempotentAdminRoleSeeder.cs`).
3. **CA1822 Sanitizer SanitizeForStorage** — metoden använder inte instance-data.
   Fix: gjorde hela klassen `public static` istället för `public sealed`.
   DI-registrering togs bort. Konsumenter anropar `JobTechPayloadSanitizer.SanitizeForStorage(...)`.
4. **CA1305 DateTime.ToString utan IFormatProvider** — JobTechStreamClient
   formaterar `since` till JobTech-API-param. Fix: `CultureInfo.InvariantCulture`.
5. **CA1859 SanitizeObject return-type** — fix: `JsonObject` istället för `JsonNode`.
6. **xUnit1051 TestContext.Current.CancellationToken** — handler-tester använde
   `await db.SaveChangesAsync()` utan ct. Fix: propagera ct genom alla EF-anrop.
7. **Sanitizer-test SanitizeForStorage_PreservesPublicMetadata FAIL** — allowlist
   saknade `text`-key (nested i description.text). Fix: lade till `text`,
   `conditions`, `abilities`.

## Web-search räddade scope

Tre kritiska fakta-verifieringar via web-search (CLAUDE.md §9.5):

1. **JobTech API:er** — bekräftade jobsearch/jobstream endpoints + API-key krävs
   via apirequest.jobtechdev.se (datum-verifierat 2026-05-12).
2. **Microsoft.Extensions.Http.Resilience** — senaste stabila 10.5.0 för .NET 10.
3. **OpenTelemetry CVE-pinning** — GHSA-g94r-2vxg-569j patchad i 1.15.3
   (`opentelemetry.api/CVE-2026-40894`).

## Lärdomar

- **Reviewers INLINE räddar scope** — 2 Major-fynd från security-auditor
  (Description-PII-doc + processing-register) hade förmodligen lyfts som TDs
  vid post-hoc audit. Inline-discipline gav in-block-fix per §9.6.
- **Architect-rond före kod = preventiv mot Polly-detour** — utan architect:s
  varning hade `AddStandardResilienceHandler` använts på JobStream → 1-req/min
  brutits vid retry-loop. Detta är "design-as-decision-not-test"-värde.
- **Process-statiska rate-limiters är test-fientliga** — fix: separat DI-container
  i `JobTechStreamResilienceTests` som testar bara retry+CB-pipeline utan
  rate-limit-bagage. P8c-Hangfire-jobben kommer dela samma limiter i prod.
- **CVE-pinning är inte luxe — det är default** — varje gång ett nytt paket
  läggs till bör senaste CVE-status verifieras. NU1902 är `WarningAsError`.

## Post-tag-push: smoke-test mot dev (2026-05-13 ~06:00–06:30)

### Tag-cykel

- `v0.2.2-dev` på `139a85e` → **FAILED** (SDK 10.0.200 vs container 10.0.300, `global.json rollForward=latestPatch` för strikt)
- Fix `03f8207`: `latestPatch` → `latestFeature` (accepterar 10.0.x.x)
- `v0.2.2.1-dev` på `03f8207` → **SUCCESS** ✓ (deploy `25778778579`)

### Klas-fynd via curl mot live JobTech

Klas observation av Swagger UI (`jobstream.api.jobtechdev.se` 2.1.1) avslöjade
att v1-endpoints (`/snapshot`, `/stream`) är **deprecated** — `/v2/snapshot` +
`/v2/stream?updated-after=...` är aktuella. Plus: `apirequest.jobtechdev.se`
ger DNS-fel (subdomän nedlagd).

Live-curl mot `/v2/stream?updated-after=...` → **HTTP 200 utan api-key** =
bekräftat open API. Markdown-docs som säger key krävs är föråldrad.

V2-shape skiljer sig dock från v1 — kritisk bugg upptäcktes:
- `webpage_url` (top-level) ersätter `source_links[0].url`
- `application_details.email` + `employer.email/phone_number` är PII
- Nya fält: `text_formatted`, `number_of_vacancies`, `logo_url`, `coordinates`, etc.

Utan fix hade min `FirstNonMailtoUrl(sourceLinks=null, applicationDetailsUrl=null)`
returnerat null → URL tom → alla items skippats. Commit `139a85e` fixade:
- `JobTechHit.WebpageUrl`-property
- 3-args `FirstNonMailtoUrl(webpageUrl, sourceLinks, applicationDetailsUrl)`
- Sanitizer-allowlist utökad med v2-publika fält
- 3 nya v2-PII-regression-tester

### Admin-bootstrap för smoke-test

ECS task-def `jobbpilot-dev-api:10` hade **ingen** `AdminBootstrap__InitialAdminEmail`
— Klas hade aldrig satt upp det. Skapade task-def rev 11 med
`AdminBootstrap__InitialAdminEmail=klasolsson81@gmail.com`. Synkroniserade
Terraform i samma session (per Klas-disciplin "lyft inga TDs som vi inte kan
fixa direkt"):
- `infra/terraform/environments/dev/variables.tf` — ny `initial_admin_email`-variabel
- `infra/terraform/environments/dev/terraform.tfvars` — `initial_admin_email = "klasolsson81@gmail.com"`
- `infra/terraform/environments/dev/main.tf` — `api_environment["AdminBootstrap__InitialAdminEmail"]`

Klas registrerade konto via curl → force-deploy så `IdempotentAdminRoleSeeder`
körs igen vid host-startup → Admin-roll tilldelad (verifierat via
`/api/v1/me` returnerade `roles: ["Admin"]`).

### Smoke-test admin-trigger — 504 Gateway Timeout

`POST /api/v1/admin/job-ads/sync/platsbanken` med admin-session → **504 efter
exakt 60s**.

CloudWatch-loggar bevisar **korrekt design**:
```
Failed handling SyncPlatsbankenSnapshotCommand after 59879ms
System.Threading.Tasks.TaskCanceledException: A task was canceled.
   at JobTechStreamClient.FetchSnapshotAsync line 34
   at PlatsbankenJobSource.FetchSnapshotAsync line 28
   at SyncPlatsbankenSnapshotCommandHandler line 33
   [pipeline-behaviors propagerade CT korrekt: Audit → UnitOfWork →
    Validation → Logging]
```

CT-propagation OK, no partial-persist, clean cancellation, stack-trace ren.

**ALB default `idle_timeout=60s`** är otillräckligt för JobTech-snapshot
(~50-100 MB JSON-array). Min `HttpClient.Timeout=5min` löser inte detta —
ALB stänger downstream-anslutningen och triggar CT-cancellation.

### CTO-rond 5: ALB-timeout-beslut

Klas tre varianter presenterade till `senior-cto-advisor`:
- **A** — Bumpa ALB idle_timeout 60s → 300s (+ Terraform-sync)
- **B** — Acceptera 504, vänta P8c (synkron HTTP är fel transport för snapshot)
- **C** — Filtrerad sync via query-param `?limit=N`

**CTO-beslut: Variant B.** Kärnargument:

> "Snapshot är **per design** ett bakgrundsjobb. En 50–100 MB JSON-import över
> HTTP-request är fel transport oavsett timeout-värde. Att bumpa ALB för att
> tvinga in en synkron variant är att låta operations-konfig kompensera för
> fel arkitektur (Martin 2017 kap 17 Boundaries; Fowler 2002 Asynchronous
> Messaging)."

> "504:n är **bevis på korrekt design**, inte en bug att maskera. Att flytta
> timeout-gränsen tar bort signalen som validerar att CT-kedjan fungerar."

> "Bumpen blir **dead config** så fort Hangfire är på plats — vi bumpar för
> en vecka."

> "Saltzer/Schroeder 1975: global ALB-bump exponerar alla endpoints (inkl
> anonyma /api/ready, /api/v1/auth/login) för 5× längre slow-read-fönster
> för att lösa ett admin-anrop som inte ens hör hemma där."

ADR 0032 §3+§9 stödjer redan Variant B — **inget ADR-amendment behövs**.
End-to-end DB-persist-verifiering flyttas till **P8c-acceptance-criteria**
som planerat arbete, inte TD (CLAUDE.md §9.6 kriterium 1: annan fas).

### Vad som verifierats i smoke-test

| Test | Resultat |
|---|---|
| GET /api/ready | 200 ✓ |
| POST /admin/sync/platsbanken (anonym) | 401 ✓ defense-in-depth |
| POST /admin/sync/platsbanken (non-admin) | 403 ✓ AdminAuthorizationBehavior |
| GET /me + sessionId-flow | 200 ✓ |
| GET /job-ads paginated | 200 ✓ (empty list pre-sync) |
| Auth-flow register → login → session | 200/200 ✓ |
| `AdminBootstrap__InitialAdminEmail`-mekanism | Admin-roll tilldelad ✓ |
| Pipeline-behaviors-stack | Stack-trace bevisar korrekt ordning ✓ |
| CT-propagation Api → Handler → Infrastructure | Loggar bevisar ✓ |
| JobTech v2 open API + shape | Curl-verifierat ✓ |
| Full DB-persist mot riktig JobTech | **Skjuten till P8c** per CTO |

## Pending operativt

## Nästa session — F2-P8c (Hangfire)

Per ADR 0032 §9 leverans-plan:

- `SyncPlatsbankenStreamJob` (cron `*/10 * * * *`) — använder `IJobSource.StreamChangesAsync`
- `SyncPlatsbankenSnapshotJob` (cron `0 2 * * *`) — använder `IJobSource.FetchSnapshotAsync`
- `UpsertExternalJobAdCommand` med DbUpdateException-catch-pattern (ADR 0032 §5)
- Removal-handling via `JobAd.Archive` (ADR 0032 §6)
- `JobAdsSyncedDomainEvent` audit-wire (ADR 0032 §8)
- `PurgeStaleRawPayloadsJob` (cron `0 3 * * *`, 30 dagars retention) — TD-73 punkt 2
- `RawPayloadPurgedDomainEvent` audit-wire
- TD-73 punkt 4 (right-to-erasure-cascade till raw_payload)
- E2E-tester på dev: verifiera Stream-cron kör ~6×/timme

## Tidsuppskattning

~5h CC-tid effektivt (24 filer, 1703 insertions, 4 agent-ronder, 7 disciplinmiss-
fixar). Reviewers-INLINE-discipline kostade ~1h extra men sparade post-hoc fix-batch.

**HEAD vid session-end:** `8c09191` (icke-deployad — tag-push väntar Klas-GO)
