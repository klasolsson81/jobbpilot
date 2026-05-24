---
session: F6 P5 Punkt 3 — Landing live-stats (ADR 0064 + ADR 0056-amend)
datum: 2026-05-24
slug: f6-p5-punkt3-landing-live-stats
status: FULLT LEVERERAD & DEPLOY-TRIGGAD v0.2.60-dev (3 commits, alla CI-gröna efter PR3-fix)
commits:
  - "e6b08fa feat(landing): F6 P5 Punkt 3 PR1 — publik landing-stats med pre-computed Redis-cache + frontend swap (26 filer, +910/-41)"
  - "4a5d00d docs(adr): F6 P5 Punkt 3 PR2 — ADR 0064 (Accepted) + ADR 0056 Amendment + NBomber perf-scenario + TD-89 (9 filer, +632/-35)"
  - "13d172d fix(landing): test-ordningsoberoende cache-rensning i LandingStatsEndpointTests (1 fil, +5/-1)"
deploys:
  - "Tag v0.2.60-dev pushad på 13d172d → deploy-run 26350409884 queued (2026-05-24)"
adrs:
  - "ADR 0064 NY (Accepted 2026-05-23) — Publik anonym aggregat-read via Worker-precomputed Redis-cache; arkitekturprecedens för F6 P5 Punkt 4 /oversikt"
  - "ADR 0056 Amendment 2026-05-23 — Beslut 4 FAS-DEFERRAL lyft; getLandingStats() async-fetch via GET /api/v1/landing/stats realiserad per ADR 0064; utbytespunkt-design bevarad"
tds:
  - "TD-89 (Minor × Trigger) — Ephemeral API+Redis+Worker-stack i CI loadtest-jobb (LOADTEST_SCENARIOS=landing-stats mot riktig backend); self-vetoad CLAUDE.md §9.6 punkt 2 (funktion-dependency saknas: docker-compose-stöd i loadtest-jobbet finns ej idag)"
---

# F6 P5 Punkt 3 — Landing live-stats

**HEAD vid session-end:** `13d172d` (origin/main). **Tag `v0.2.60-dev` pekar på `13d172d`** — deploy-dev run `26350409884` queued 2026-05-24. Pre-push-gates passerat alla 3 push.

## Mål

Punkt 3 av 5-punkts-ordningen (Klas-bekräftad 2026-05-23): leverera publik `GET /api/v1/landing/stats` så `LandingTopbar` `getLandingStats()` slutar vara hårdkodad konstant och visar riktiga räkningar mot levande JobAd-korpus. Perf-kritiskt (publik landing, ingen auth — cache-stampede-skydd obligatoriskt). Beror på Punkt 1 (snapshot-retention) för korrekta `Active`-räkningar.

## CTO-dom (agentId `a1da26dc2029a5def` 2026-05-23) — multi-approach-val

**Variant B vald** över A/C/D — Worker Hangfire-cron `*/5` skriver `landing:stats:v1` till Redis via `ILandingStatsCache`-port; Api gör ren cache-read med Floor + IsStale=true-fallback.

Motiverat mot principer:

- **A (cache-aside i Api-handler):** cache-stampede-risk vid cold key + N+1 paritetsbrott mellan första anonym request och cron-uppdatering. SoC-brott — Api-handler får dubbla ansvar (kompositions-läs + räkne-skrivning).
- **C (PG materialized view):** REFRESH MATERIALIZED VIEW CONCURRENTLY kräver UNIQUE-index på vy = över-koppling till PG-fysisk-modell + DBA-disciplin för aggregat som ändras. CTO-citat: "DDD bounded-context-brott — vyer är fysisk modell, inte domän-räkne-port".
- **D (Next.js fetch revalidate 60s):** flyttar perf-kritisk yta till CDN-edge utan central observability + cache-miss-storm vid revalidate-fönster-byte. 12-Factor §VI bryts (process-state i CDN).
- **B (vinnaren):** Worker-precomputed Redis-cache — separat skrivar-ansvar (Worker) från läsar-ansvar (Api), `IsStale=true` returneras vid cache-miss istället för 500/timeout (graceful degradation), key `landing:stats:v1` med TTL 12× refresh-intervall (60min vs 5min cron = trygg marginal mot Worker-paus).

CTO citerade: SRP/SoC (Martin 2017 kap. 7), Cache-stampede-pattern (Fowler 2002 PoEAA), DDD bounded-context (Evans 2003 §14), 12-Factor §VI (stateless processes), Saltzer & Schroeder least-common-mechanism (för Beslut b nedan).

### Beslut a/b/c/d

- **(a)** Variant B: Worker Hangfire-cron `*/5` skriver `landing:stats:v1` via `ILandingStatsCache`-port; Api ren cache-read med Floor + `IsStale=true`-fallback vid miss; key TTL 12× refresh (60 min).
- **(b)** Dedikerad `LandingPublicReadPolicy` IP-partitionerad 60/min/IP — får **INTE** återanvända UserId-partitionerad `ListReadPolicy` (Saltzer & Schroeder least-common-mechanism — anonym yta ska inte dela rate-limit-mekanism med autentiserade ytor).
- **(c)** `Cache-Control: public, max-age=30` strikt < refresh-fönstret (5 min) för CDN/proxy-absorb utan stale serve mot users.
- **(d)** Komplementär axel till ADR 0048 Beslut b — fjärde benet jämte bounded-context (0043 ACL) / provider-assembly (0062 FTS-port) / publik↔privat-overlay (0063 batch-port), EJ supersession.

ADR 0056 Beslut 4 FAS-DEFERRAL lyfts via amend: utbytespunkt-design (frontend-side `getLandingStats()` async-fetch) bevarad — bara implementationen byts från hårdkodad konstant till async fetch mot `/api/v1/landing/stats`.

## Klas-direktiv denna session

**Stats-fält-lista bekräftad till 2 fält** per ADR 0056 (`activeCount` + `newToday`) — Klas valde "ADR 0056 spec (2 fält)"-alternativet via AskUserQuestion. Alternativen var (i) ADR 0056 spec 2 fält / (ii) utvidga till 4 fält (`activeCount` + `newToday` + `applicationsTodayCount` + `topRegionsTop3`). Klas-motivering: ADR 0056 var redan spec'd för 2 — vidgning är scope-kryp, bör triggas av separat Klas-beslut om frontend-design senare.

## Vad som levererades per PR

### PR1 (commit `e6b08fa`) — Publik landing-stats med pre-computed Redis-cache + frontend swap

**26 filer, +910/-41.**

Backend:

- **Domain:** `LandingStats` value object (`ActiveCount`, `NewToday`, `IsStale`, `RefreshedAt`) — invariant-skyddad konstruktor (`ActiveCount >= 0`, `NewToday >= 0`, `NewToday <= ActiveCount`).
- **Application:**
  - `GetLandingStatsQuery` (DTO-projektion) + `GetLandingStatsQueryHandler` — ren cache-read mot `ILandingStatsCache`-port, returnerar `LandingStatsDto` med `IsStale=true` + Floor (`ActiveCount=0`, `NewToday=0`) vid cache-miss.
  - `ILandingStatsCache` Application-port (`GetAsync` + `SetAsync`), `ILandingStatsCalculator` Application-port (för Worker).
  - `RefreshLandingStatsJob` orchestrator (Worker-konsument) — kallar Calculator → SetAsync.
  - Aldrig-writes-disciplin: handler-test `never_writes_via_handler` etablerar att GetLandingStatsQueryHandler aldrig anropar `SetAsync` (write-isolation till Worker).
- **Infrastructure:**
  - `LandingStatsRedisCache` (StackExchange.Redis JSON-serialiserad payload, key `landing:stats:v1`, TTL 60 min).
  - `LandingStatsCalculator` (EF Core query mot `JobAd` med `Status=Active` + `DeletedAt IS NULL` filtrering, `NewToday = COUNT(WHERE PublishedAt >= today_utc_midnight)`).
  - DI-bindningar i Api (cache + Calculator) + Worker (cache + Calculator + Hangfire-cron `*/5 * * * *`).
- **Api:**
  - Endpoint `GET /api/v1/landing/stats` (anonym, no auth) i `LandingEndpoints` module.
  - `LandingPublicReadPolicy` rate-limit (60/min/IP, dedikerad partition — separat från `ListReadPolicy`).
  - `Cache-Control: public, max-age=30` response-header.
  - **Min-1 Cache-Control IN-BLOCK** per security-auditor — explicit header för CDN/proxy-absorb.
  - **Min-3 Redis-exception IN-BLOCK** per security-auditor — `catch (RedisException)` → returnera Floor (`IsStale=true`) istället för 500 (graceful degradation, ingen Redis-stack-trace-läckage till anonym yta).
  - **Min-2 JsonSerializerDefaults.Web IN-BLOCK** per dotnet-architect — camelCase + nullable-tolerant deserialisering paritet med övriga publika DTO:er.

Frontend:

- `lib/api/landing.ts` — `fetchLandingStats()` mot `/api/v1/landing/stats`.
- `lib/landing/getLandingStats.ts` — byter hårdkodad konstant mot async fetch; ADR 0056 Beslut 4 utbytespunkt realiserad. Behåller `RevalidateTag`-stöd för framtida ISR.
- `LandingTopbar` använder svar oförändrat (data-shape `{activeCount, newToday}` matchade ADR 0056-spec exakt).
- Vitest: 4 zod-mirror-tester (`landing-schemas.test.ts`) + 5 fetch-fallback-tester (`getLandingStats.test.ts` — Floor vid network error, IsStale-pass-through, schema-validation, etc.).

### PR2 (commit `4a5d00d`) — ADR 0064 (Accepted) + ADR 0056 Amendment + NBomber + TD-89

**9 filer, +632/-35.**

- **ADR 0064 skriven** (Accepted 2026-05-23) — Publik anonym aggregat-read via Worker-precomputed Redis-cache. Etablerar arkitekturprecedens för Punkt 4 `/oversikt`.
- **ADR 0056 Amendment 2026-05-23** — Beslut 4 FAS-DEFERRAL lyft, `getLandingStats()` async-fetch realiserad per ADR 0064, utbytespunkt-design bevarad, Beslut 1/2/3/5 oförändrade.
- **ADR-index uppdaterat** (`docs/decisions/README.md`) — rad 79 för ADR 0064, ADR 0056-raden får amend-not med cross-ref.
- **NBomber perf-scenario** etablerat i `perf/JobbPilot.LoadTests/` — scenario `landing_stats_cache_hit` (60/min/IP burst-test, p95 < 50ms vid cache-hit). perf-test-writer (agentId `a8d8c9a68d076ba85`).
- **TD-89 lyft** — Ephemeral API+Redis+Worker-stack i CI loadtest-jobb (`LOADTEST_SCENARIOS=landing-stats` mot riktig backend). Self-vetoad enligt CLAUDE.md §9.6 punkt 2 (funktion-dependency saknas: docker-compose-stöd i loadtest-jobbet finns ej idag).

### PR3 (commit `13d172d`) — Test-ordningsoberoende cache-rensning

**1 fil, +5/-1.**

Symptom: `LandingStatsEndpointTests` röt i CI när test-ordning skiftade beroende på parallell-fördelning. Rotorsak: `IDistributedCache` (`StackExchange.Redis.Extensions.Core`) använder `InstanceName`-prefix internt; test-fixturens cache-rensning mellan tester använde redan-prefixad key → cache-entry kvar mellan tester → tidigare-test-state läckte in i nästa.

**Fix:** rensa cache via underliggande `IConnectionMultiplexer.GetServer().FlushDatabaseAsync()` (test-isolerad Redis-DB) istället för key-vis `RemoveAsync` med dubbel-prefix. CI grön efter pushen.

## Reviews

| Roll | AgentId | Domslut |
|---|---|---|
| senior-cto-advisor | `a1da26dc2029a5def` | **Variant B** vald (Worker Hangfire-cron + Redis pre-computed) över A (cache-aside) / C (PG materialized view) / D (Next.js fetch revalidate). Motiverat mot SRP/SoC/SOLID/cache-stampede/DDD bounded-context/12-Factor. |
| security-auditor | `a5d1509436995d094` | **APPROVED** — 0 Block / 0 Critical / 0 High / 0 Medium / 0 Major / **3 Minor**. Min-1 Cache-Control IN-BLOCK, Min-2 KnownNetworks redan täckt (no-op), Min-3 Redis-exception → Floor IN-BLOCK. Rate-limit 60/min/IP försvarbart mot OWASP API4:2023. |
| code-reviewer | `a7f0b30922a24a879` | **APPROVED full clean** — 0/0/0. |
| dotnet-architect | `ae0f3583e4ed741e3` | 0 Block / 0 Major / **3 Minor**. Min-2 JsonSerializerDefaults.Web IN-BLOCK, andra skip-motiverade (paritet med befintliga endpoints). |
| design-reviewer | `a8a5426b816dd3f08` | **GO** — 0 Block, 0 Major, 3 Minor FYI (frontend swap är prop-data-byte, inte visuell ändring). |
| adr-keeper | `a1f53f8e90f8d714d` | ADR 0064 Accepted + ADR 0056 Amendment 2026-05-23 skrivna (Klas-override för verbatim-källa per memory `feedback_klas_can_override_adr_verbatim_source`). |
| perf-test-writer | `a8d8c9a68d076ba85` | NBomber-scenario `landing_stats_cache_hit` etablerat i `perf/JobbPilot.LoadTests/`. |

## TDs lyfta (§9.6-godkända)

### TD-89 (Minor × Trigger)

**Ephemeral API+Redis+Worker-stack i CI loadtest-jobb.** NBomber-scenariot `landing_stats_cache_hit` är etablerat in-repo men körs idag bara lokalt — CI har ingen docker-compose-stack-up som rest backend för loadtest. §9.6 punkt 2 self-vetoad: funktion-dependency saknas (docker-compose-stöd i loadtest-jobbet finns ej idag). Triggers: Punkt 4 (`/oversikt`) eller framtida publik-yta som kräver perf-budget-gate.

## Tester-delta

| Suite | Före | Efter | Delta |
|-------|------|-------|-------|
| Domain.UnitTests | 404 | 404 | (oförändrat) |
| Application.UnitTests | 573 | **578** | +5 (3 `GetLandingStatsQueryHandlerTests` inkl. `never_writes_via_handler`-disciplinerings-test + 2 `RefreshLandingStatsJobTests`) |
| Architecture.Tests | 78 | 78 | (oförändrat) |
| Api.IntegrationTests | +n | **+3** | Anonym-åtkomst, cache-miss-floor, cache-hit; Testcontainers Postgres+Redis; 1512 totalt grönt |
| Worker.IntegrationTests | n | n | (oförändrat) |
| Migrate.UnitTests | 6 | 6 | (oförändrat) |
| Web vitest | 635 | **644** | +9 (4 zod-mirror + 5 fetch-fallback; uppdaterade landing-topbar/landing-page tester ingår) |

CI grön på `13d172d` efter PR3-fix. Frontend `pnpm build` grön — RSC-boundary verifierad.

## Beslut / detours

- **Variant B vald över A/C/D** — Worker-precomputed Redis-cache är arkitekturprecedens för Punkt 4 `/oversikt`. ADR 0064 dokumenterar fyra-axel-modellen (bounded-context / provider-assembly / publik↔privat / cache-skrivar-läsar-separation). Cache-stampede-pattern (Fowler 2002) + DDD-bounded-context (Evans 2003 §14) bär domen.
- **Klas valde 2 fält** (`activeCount` + `newToday`) per ADR 0056 spec — vidgning till 4 fält är scope-kryp, separat Klas-beslut vid framtida frontend-design.
- **EF Core 10 + Npgsql 10 Contains-issue uppstod inte** här (cap=2 fält + ingen VO-collection-Contains mot entity-property) — relevant bara för PR1-vägen i Punkt 2 PR5.
- **TD-89 self-vetoad** — funktion-dependency saknas (docker-compose-stöd i loadtest-jobbet), §9.6 punkt 2-träff.
- **Test-ordningsoberoende cache-rensning** — Klas-bestämd disciplin (test-isolation > test-svit-ordning-beroende). PR3 är en-rad-fix men anti-regression värd.

## Disciplin-bekräftelser

- Alla commits explicit pathspec (`-- <paths>`) per memory `feedback_pathspec_commit_parallel_cc`.
- `.claude/settings.json` aldrig committad; auto-skapad scaffolding (`docs/handoff-oversikt/`, `docs/jobbpilot-v3-bundle/`, `docs/reviews/2026-05-17-agent-roster-gap-cto.md`) orört per memory `feedback_dont_delete_auto_files`.
- ADR 0064 + ADR 0056-amend via adr-keeper (agentId `a1f53f8e90f8d714d`) — Klas-override för verbatim-källa per memory `feedback_klas_can_override_adr_verbatim_source`.
- CC gav **inte** egen rekommendation vid multi-approach-val — senior-cto-advisor är decision-maker per §9.6 + memory `feedback_cto_decides_multi_approach`.
- TD-89 self-vetoad mot §9.6 punkt 2 — funktion-dependency saknas, behöver inte lyftas som "TODO-CI-bygg".

## Pending Klas-operativt

1. **Visual-verify post-deploy** efter `v0.2.60-dev`-deploy klar grön: `curl https://dev.jobbpilot.se/api/v1/landing/stats` (förvänta JSON med `activeCount > 0` + `newToday >= 0` + `isStale=false` efter första Worker-cron-pass `*/5`) + landing-page-rendering i browser.
2. **Korpus-konvergens Punkt 1** — fortfarande ~72h-fönstret från 2026-05-23 v0.2.57-dev-deploy (separat spår, klart 2026-05-26).
3. **Klas-granskning av ADR 0064-text** + ev. över-ride av prosa (substansen är CTO-låst — Klas kan finjustera ordval per memory `feedback_klas_can_override_adr_verbatim_source`).

## Klassiska följdfrågor (ej Punkt 3-scope, för framtida design-rond)

- **Stats-fält-vidgning till 4 fält** (`applicationsTodayCount` + `topRegionsTop3`) — Klas-beslut, kräver frontend-design innan backend-vidgning.
- **CDN-edge-cache-policy** — `Cache-Control: public, max-age=30` används idag; framtida Cloudflare/CloudFront-edge-tuning kan vinna under landing-trafik-burst (separat infra-rond).

## Lessons learned

1. **CTO Variant B Worker-precomputed Redis-cache är publik-anonym-aggregat-precedens.** Punkt 4 `/oversikt` återanvänder mönstret. Cache-stampede-pattern (Fowler 2002) + DDD-bounded-context (Evans 2003 §14) gav entydig dom mot cache-aside / materialized view / Next.js revalidate.
2. **IDistributedCache InstanceName-prefix-mönster är test-isolations-fälla.** Test-cache-rensning med pre-prefixad key blir dubbel-prefixad → entries läcker mellan tester. Använd `IConnectionMultiplexer.FlushDatabaseAsync()` (test-Redis-DB-isolerad) istället för key-vis `RemoveAsync`.
3. **2-fält-spec över 4-fält-vidgning** — Klas-disciplin "ADR spec bär, vidgning är separat beslut". Hade jag (CC) lyft Variant 2 (4 fält) som rekommendation hade jag brutit memory `feedback_cto_decides_multi_approach`. AskUserQuestion var rätt väg.

## Nästa session

**Punkt 4 (Översiktssida `/oversikt`)** — large, frontend+ev. backend. Handover-källa `docs/jobbpilot-v3-bundle/JobbPilot/handoff-oversikt/HANDOVER-oversikt.md` (Klas-godkänd 2026-05-23). Tre sektioner: Title+I dag / Notiser / Sammanfattning. Mockdata-tillåtelse för fält där BE saknas (HANDOVER §0). **Stänger TD-82**. **Återanvänder ADR 0064-mönster** (Worker-precomputed Redis-cache) för aggregat-fält som behöver perf-budget. CTO-rond vid sessionsstart för data-mappning riktigt-vs-mock (HANDOVER §3).

Startprompt genereras separat per `docs/runbooks/session-start-template.md` när Klas ger GO.
