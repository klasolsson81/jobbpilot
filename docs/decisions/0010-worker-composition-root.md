# ADR 0010 — Worker som separat composition root

**Datum:** 2026-04-19
**Status:** Accepted
**Kontext:** Fas 0 kod-scaffolding, session 6. Kompletterar ADR 0001.
**Beslutsfattare:** Klas Olsson
**Relaterad:** ADR 0001 (kompletteras), ADR 0008 (pipeline-behaviors gäller även Worker)

## Kontext

ADR 0001 specificerar Clean Architecture med fyra projekt: Domain, Application, Infrastructure, Api. Flera källor i kodbasen refererar dock konsekvent "Api + Worker" som composition roots:

- `dotnet-architect.md` agent (`.claude/agents/dotnet-architect.md`)
- CLAUDE.md §2.1
- `docs/research/SESSION-1-FINDINGS.md`

En spec-drift identifierades under Fas 0-handover 2026-04-19: ADR 0001 säger 4 projekt, koden- och agent-specarna säger 5. Denna ADR formaliserar beslutet om ett femte projekt och stänger spec-driften.

Bakgrunden till Worker som separat projekt är konkret: JobbPilots AI-operationer (CV-parse 10–30s, cover-letter-generation, match-scoring) är för långsamma för HTTP-request/response-cykler. De behöver köras som asynkrona bakgrundsjobb.

## Beslut

`JobbPilot.Worker` läggs till som femte projekt och separat composition root. Worker är en .NET Generic Host (BackgroundService) med Hangfire-workers i Fas 1+.

**Dependency-struktur (samma som Api):**
- `JobbPilot.Worker` → `JobbPilot.Application`
- `JobbPilot.Worker` → `JobbPilot.Infrastructure`
- `JobbPilot.Worker` → `JobbPilot.Domain` (transitiv via Application, men explicit i .csproj)

Worker är ett separat deployed artifact — inte ett library refererat från Api.

**Mediator-setup:** Worker registrerar Mediator.SourceGenerator och egna pipeline-behaviors (ADR 0008) i sin egen DI-container. Mediator.SourceGenerator genererar source-code per assembly; Worker och Api är separata assemblies med egna genererade Mediator-instanser.

**Fas 0-state:** Worker börjar som tom shell med en no-op BackgroundService (`Worker.cs`) som loggar heartbeat var 5:e minut. Inga Hangfire-jobs registreras i Fas 0. Tom shell kostar noll runtime.

## Konsekvenser

**Positivt:**

- Långa AI-operationer (CV-parse, cover-letter-gen) blockerar inte HTTP-trådar i Api
- Worker kan skalas oberoende av Api (t.ex. spot instances för batch-jobb)
- Separat observability: Worker-jobs syns i Seq med separat service-tagg
- Separat deployment: Worker kan pausa/restart utan Api-downtime
- Tom shell i Fas 0 = noll runtime-kostnad; ompacketering i Fas 1 = undviks

**Negativt:**

- Ett extra projekt att underhålla, bygg-tid, test-coverage
- Mediator.SourceGenerator måste installeras i båda composition roots (Api + Worker) — dokumenterat i ADR 0001 och BUILD.md
- Architecture tests måste täcka Worker (ytterligare ett dependency-test-case)

**Mitigering:**

- Worker delar Application + Infrastructure med Api — inget nytt domänlager att underhålla
- Scaffolding är gjord i Fas 0 — framtida Fas 1-aktivering är bara att lägga till Hangfire-jobs

## Alternativ övervägda

**Alt 1 — In-process Hangfire i Api:** Vanligt mönster, avvisat. Blandar HTTP-request-livscykel med bakgrundsjobb-livscykel. Hangfire-workers konkurrerar med HTTP-request-threads om CPU. Skalning är allt-eller-inget (Api + workers växer ihop). Separata APM-traces blandas.

**Alt 2 — Separat repo för Worker:** Avvisat. Monorepo-friction. Domain/Application/Infrastructure behöver synkas. CI/CD-pipelines dubbleras. Deployment-koordination ökar komplexitet utan nytt värde.

**Alt 3 — Azure Functions / AWS Lambda:** Intressant för event-driven AI-triggers men over-engineering för Fas 0–1. Hangfire ger job-persistence, retry, och dashboard ur lådan. Kan migreras till Lambdas vid behov i Fas 3+.

**Alt 4 — Inget Worker-projekt förrän Fas 1:** Avvisat (det som denna ADR stänger). Scaffolding är trivial och kostar <1 timme. Retroaktiv ompacketering när AI-jobs behövs i Fas 1 kostar mer: ny projektfil, uppdaterade project references, uppdaterade CI/CD-pipelines, uppdaterade architecture tests.

## Implementationsstatus

**Scaffoldat i:** Fas 0 kod-scaffolding session 6 (`feat(src): scaffold 5 .NET projekt per ADR 0010`)

**Konfiguration:**
- `JobbPilot.Worker.csproj` med `Mediator.SourceGenerator` (Analyzer, PrivateAssets=all)
- `Worker.cs` — no-op BackgroundService, loggar heartbeat var 5:e minut
- `Program.cs` — minimal Hosted Service setup

**Aktiveras Fas 1:** Hangfire-integration, första AI-background-job, retry-policies.

**Påverkar ADR 0001:** ADR 0001 §Beslut listar fyra projekt. Denna ADR
kompletterar listan med ett femte (Worker). ADR 0001 förblir oförändrad
enligt immutable-policyn — läsare som söker komplett projektlista måste
konsultera både ADR 0001 och denna ADR. Relationen synliggörs via
"Relaterad: ADR 0001 (kompletteras)" i båda ADR:erna och via
`docs/decisions/README.md` där index-läsare ser dem i sekvens.
