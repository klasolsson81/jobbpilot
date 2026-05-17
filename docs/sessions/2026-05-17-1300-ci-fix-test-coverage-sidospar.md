---
session: PRIO-1 CI-fix + test-coverage-sidospår
datum: 2026-05-17
slug: ci-fix-test-coverage-sidospar
status: levererad
commits:
  - b3772a3 fix(test) ApiFactory seeder-determinism efter migrations (PRIO-1 CI-fix)
  - a352ed3 docs(ci) PRIO-1 CI-fix levererad — handoff-diagnos falsifierad & korrigerad
  - 2d262ee build(coverage) reproducerbar in-repo coverage-mekanism + ADR 0044 Proposed
  - 6768700 test(auth) DeleteAccountCommandHandler ~100% branch — GDPR Art.17 (B2)
  - 472dbdb test(coverage) ListInvitations (B1) + Hangfire-job failure/empty/cancel (B3)
  - (denna docs-commit — session-end-synk)
---

# Session 2026-05-17 — PRIO-1 CI-fix + test-coverage-sidospår

## Mål

1. **PRIO-1-injektion:** main-`build`-CI RÖD (GetTaxonomyEndpointTests IndexOutOfRange) — äga fixen, grön main före fortsatt arbete.
2. **Test-coverage-sidospår (före FAS 3):** reproducerbar in-repo coverage-mätning + stäng genuina luckor (ListInvitations, DeleteAccount GDPR, Hangfire-grenar). README-skryt = separat senare uppgift.

## PRIO-1 CI-fix — handoff-diagnosen var fel

Committad handoff (CC #1, gh-logg-only, ingen lokal repro) hävdade saved-search-batch poisonade en singleton-cache. **§9.4-verifiering falsifierade det:**
- GetTaxonomyEndpointTests failar **ensam** (noll saved-search-tester) — identiskt.
- Single-variable-isolering: återställde de 3 saved-search-filerna till green-versionen (`0a2405c`) vid HEAD → failar fortfarande. Saved-search icke-kausal.
- git diff `0a2405c`..HEAD: ApiFactory/GetTaxonomyEndpointTests/seeder/DI byte-identiska.

**Verklig rotorsak (web-verif. .NET 10, dotnet/aspnetcore #60370):** `ApiFactory.InitializeAsync` `Services`-access triggar `EnsureServer()` → host-start → `IHostedService.StartAsync` FÖRE `MigrateAsync`. `TaxonomySnapshotSeeder`+`IdempotentAdminRoleSeeder` träffar tomt schema → 42P01 → Dev/Test-grace-bail utan seed → oseeded hela delade collection-livstiden → `regions[0]` kastar. Pre-existerande latent fixtur-defekt; prod opåverkad (Migrate kör DDL före trafik, ADR 0043 Beslut B).

**Lösning (senior-cto-advisor Approach D/B — fix the cause not the symptom):** kör de två idempotenta seedrarna explicit EFTER migrations i ApiFactory, riktat på exakt dessa typer. Ingen prod-kod, ingen ADR-amendment, ingen security-auditor, ingen Klas-STOPP (entydigt mot Beck/Meszaros/Fowler/Martin). Approach A (Scoped) avvisad (löser ej rotorsaken + bryter MAP-3); C (cacha-ej-tom) avvisad denna touch (security-GO:ad prod-cache-semantik, YAGNI — noterad som framtida incident-trigger-revision i ADR 0043). GetTaxonomyEndpointTests 7/7, full svit 1139/1139, code-reviewer GO 0/0/2, **main-CI grön run `25986194273`**. Handoff-doc korrigerad (falsifiering + lösning + lärdom; originaltext bevarad som granskningstrail).

## Test-coverage-sidospår

**Beslut (agenter INNAN kod):** dotnet-architect (infra-design), senior-cto-advisor (per-lager icke-regression-ratchet-gate `baseline−2pp`, branch-gate endast Domain/Application; Hangfire **Approach (a)** — testa grenar på befintliga thin orchestrators, ej Gemini extract-to-service (b, YAGNI/SoC); EN ADR 0044). Web-verif. (§9.5, nuget.org 2026-05-17): `Microsoft.Testing.Extensions.CodeCoverage` 18.6.2, `dotnet-reportgenerator-globaltool` 5.5.10.

**A — infra (`2d262ee`):** MTP CodeCoverage-paket (CPM, central i tests/Directory.Build.props), ReportGenerator via local tool manifest (ej MSBuild-NuGet — BUILD.md §3.1), `scripts/coverage.ps1`+`.sh` (paritet), rå cobertura ofiltrerad (audit-trail) + first-party-filter report-time → gitignorad `artifacts/coverage/`. CI-jobb `coverage` PROPOSED (continue-on-error, gate `exit 0`, ej i ci.needs — aktivering = Klas-GO). ADR 0044 Proposed + index. Avvikelse från architect-design: `IncludeAssets` exkluderande `compile` bröt builden (MTP.MSBuild auto-genererar SelfRegisteredExtensions.cs) → bara `<PackageReference />` som xunit.v3.mtp-v2 (code-reviewer bekräftade rätt instinkt).

**B — luckor stängda:**
- **B2 (GDPR §5.4 HÖGSTA PRIO, `6768700`):** DeleteAccountCommandHandler 71.8→**100%** branch. 4 grenar (ej-auth/ingen-JobSeeker/idempotent/cascade). security-auditor **BLOCKING → GO 0 Crit/High/GDPR** — cascade-completeness genuint bevisad (icke-vakuösa sanity-Count + IgnoreQueryFilters).
- **B1 (`472dbdb`):** ListInvitationsQueryHandler+InvitationListItemDto 0→**100%** (7 tester: status null/giltig/case-insensitive/okänt/whitespace, tomt repo, ordering+DTO-map).
- **B3 (`472dbdb`):** AuditLogRetentionJob→100%, SyncPlatsbankenSnapshotJob→98.1%, PurgeStaleRawPayloadsJob invalid-config-gren. CTO Approach (a) bekräftad korrekt av test-writer (jobben genuint thin/testbara).

**Resultat:** suite 1139→1156 (+17, 0 failed). First-party Line 92.1% / Branch 84.5% / Method 90.2% (Application 97.7%/91.1%). code-reviewer×3 GO 0 Block/0 Maj.

## Beslut & avstickare

- Handoff-diagnoser utan lokal repro §9.4-verifieras innan de agerar som sanning (lärdom, dokumenterad i handoff-doc).
- PurgeStaleRawPayloadsJob kvarstår 73% medvetet — otäckt = `ExecuteUpdateAsync` provider-bound, Worker.IntegrationTests-nivå (EF InMemory stöder ej ExecuteUpdate). Ej unit-lucka, ej brus att jaga (§9.6 / prompt D).
- Ingen TD lyft (alla luckor genuina, fixade in-block; §9.6).

## Flaggat för Klas / CTO-triage (ej åtgärdat denna session)

1. **ADR 0044 `Proposed→Accepted`-flip + CI-gate-aktivering** (per-lager-golv pinnas mot post-B-baseline, ta bort continue-on-error+exit 0, lägg coverage i ci.needs) — strategisk transition, Klas-STOPP (§9.2).
2. **`Resume.SoftDelete` saknar idempotens-guard** (`if (DeletedAt.HasValue) return;`) som `Application.SoftDelete`/`JobSeeker.SoftDelete` har. security-auditor: **ej GDPR/erasure-risk** (DeleteAccount-path korrekt via handlerns early-return) — duplicate-domain-event/timestamp-hygien-inkonsistens. CTO-triage vid lämplig domän-touch (ej TD att dumpa, ej in-block i test-only-scope).
3. **README-skryt-omskrivning** — separat senare Klas-uppgift (grunden + siffrorna levererade; README §9.2-skyddad).
4. PRIO-1 CTO-not: Approach C (cacha-ej-tom defensiv prod-hardening) avvisad denna touch, noterad som framtida incident-trigger-revision i ADR 0043 (ej TD, ej nu).

## Nästa session

FAS 3 (Application Management) kräver explicit Klas strategisk GO för sessionsbyte (§9.2). Innan dess: Klas-beslut om ADR 0044 Accepted-flip + gate-aktivering, ev. Resume.SoftDelete-CTO-triage, README-skryt-uppgift.
