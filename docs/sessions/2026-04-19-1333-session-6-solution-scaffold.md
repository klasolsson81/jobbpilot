---
session: 6
datum: 2026-04-19
slug: session-6-solution-scaffold
status: komplett
commits:
  - 87e870d  # docs(decisions): ADR 0008-0010
  - TBD      # feat(solution): JobbPilot.sln + Directory.Build.props + global.json
  - TBD      # feat(src): scaffold 5 .NET projekt
  - TBD      # feat(tests): scaffold 4 xUnit v3 testprojekt
  - TBD      # feat(husky): aktivera test-gates
  - TBD      # docs(session): session 6
---

## Mål för sessionen

Fas 0 kod-scaffolding STEG 1:

1. Skriv tre ADRs (0008 pipeline-order, 0009 no-repository, 0010 worker comp root)
2. Scaffolda .NET Solution med 5 src-projekt + 4 test-projekt
3. Aktivera Husky test-gates i pre-commit

## Fas A — ADRs

Skapades med `adr-keeper`-agenten. Alla tre direkt Accepted. Viktigaste punkter:

- **ADR 0008:** Formaliserar pipeline-ordningen Logging→Validation→Authorization→UnitOfWork som CLAUDE.md §2.3 specificerade men inte motiverade. Alternativ diskuterade (bl.a. varför Validation innan Auth — Auth kräver valid ResourceId).
- **ADR 0009:** Stänger löftet i ADR 0001 §3. No-repository-pattern formaliserat med motivering: EF Core DbContext är redan UoW + Repository; `IQueryable<T>` ska inte wrappas.
- **ADR 0010:** Stänger spec-driften — ADR 0001 nämnde 4 projekt, agents/CLAUDE.md nämnde 5. Worker formellt tillagd som femte composition root. Tom shell Fas 0, aktiveras Fas 1 med Hangfire.

Två edits efter initial skapning: ADR 0008 "Nästa steg" fick neutral Mediator-syntaxformulering (MediatR-API ≠ martinothamar/Mediator), ADR 0010 sista para fick klarare immutable-policy-formulering.

Commit `87e870d` efter Klas-godkännande.

## Fas B — Solution scaffolding

### Avvikelser och rotorsaker

**global.json version:** Spec sa `"10.0.0"` med `latestPatch`. SDK 10.0.202 är feature-band 2xx; `latestPatch` täcker bara 0xx-bandet. Fixat till `"10.0.200"`.

**dotnet new sln → .slnx:** .NET 10 SDK default är nu det nya XML-formatet `.slnx`. NuGet 7.3.1 i SDK 10.0.202 ger `"Invalid framework identifier ''"` vid restore av en `.slnx`-fil. Workaround: `dotnet new sln --format sln`. Problemet försvann inte när solution-folder entries togs bort från `.sln` — det är formatet i sig som är inkompatibelt med detta NuGet-versionen.

**Solution-folder entries auto-skapas:** `dotnet sln add` i .NET 10 lägger automatiskt till virtuella solution-folder entries (`{2150E333...}`) baserat på underkatalogstruktur. Dessa skapar NuGet-restore-fel. Manuell rensning av `.sln` nödvändig. Klas godkände platt projektlista (folders är cosmetic).

**xunit3-mallen:** Genererar `xunit.v3.mtp-v2` (Microsoft Testing Platform v2), inte `xunit.v3`. `xunit.runner.visualstudio` behövs ej — MTP v2 kommunicerar direkt med dotnet test-infrastrukturen. `global.json` fick ett `"test": { "runner": "Microsoft.Testing.Platform" }`-block av mallen automatiskt. Template sätter `TargetFramework: net8.0` — borttagen, Directory.Build.props hanterar `net10.0`.

**tests/Directory.Build.props:** Skapad för att supprimera CA1707 (underscores i testnamn — konflikt med CLAUDE.md §3.2 testkonvention). Insåg att MSBuild slutar söka uppåt när den hittar en `Directory.Build.props` i en underkatalog — test-projekten fick tomt `TargetFramework`. Fix: explicit `<Import>` av root-filen via `MSBuild::GetDirectoryNameOfFileAbove`.

**Worker.cs CA1848/CA1873:** `logger.LogDebug(...)` med string interpolation gav två analyzer-fel (performance-regler). Löst med `[LoggerMessage]` source generator + `partial class Worker`.

**dotnet test syntax:** SDK 10.0.202 kräver `--project <csproj>` — positional directory-argument och .csproj-path utan flagga accepteras inte längre. Hook uppdaterad. `--no-build` borttagen (letade efter Debug-binaries som inte existerade; incremental build sker nu vid behov).

### Slutresultat

```
Build succeeded.
    0 Warning(s)
    0 Error(s)

total: 4 | failed: 0 | succeeded: 4

bash .husky/pre-commit → [pre-commit] ✓ Alla pre-commit-gates passerade.
```

## Beslut under sessionen

- `xunit.runner.visualstudio` borttagen: Klas godkände — VS 2026 + Rider stöder MTP nativt.
- Solution-folders: Klas godkände platt projektlista.
- `tests/Directory.Build.props` CA1707-suppression: kod-fix snarare än suppressionen hade kunnat väljas (rename till `TestProjectBuilds`), men vår testkonvention per CLAUDE.md §3.2 kräver underscores för alla framtida riktiga tester — suppression i test-scope är rätt lösning.

## Nästa session (STEG 2)

1. `IAppDbContext` + `IUnitOfWork` interfaces i Application
2. `AppDbContext : DbContext, IAppDbContext` i Infrastructure
3. Mediator-pipeline-behaviors (Logging→Validation→Auth→UoW) per ADR 0008
4. Första aggregate: `JobSeeker`
5. Initial EF Core-migration
