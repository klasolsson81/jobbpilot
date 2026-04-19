# ADR 0009 — Inga Repositories; direkt IAppDbContext + IUnitOfWork

**Datum:** 2026-04-19
**Status:** Accepted
**Kontext:** Fas 0 kod-scaffolding, session 6. Formaliserar löfte i ADR 0001 §3.
**Beslutsfattare:** Klas Olsson
**Relaterad:** ADR 0001 §3, ADR 0008 (UnitOfWork-behavior), CLAUDE.md §5.1

## Kontext

ADR 0001 §3 lovar: "Ingen Repository-pattern (kommer dokumenteras separat i framtida ADR). Direkt DbContext i Application-handlers med IUnitOfWork-abstraktion." CLAUDE.md §5.1 listar Repository pattern ovanpå EF Core som explicit anti-pattern.

Inför Fas 1 när de första handlers skrivs behöver denna policy vara formaliserad med motivering så att code-reviewer-agenten och framtida utvecklare har ett tydligt beslutsunderlag.

Repository-pattern är ett vanligt .NET-mönster och en vanlig förfrågan. Utan dokumenterat beslut riskeras "men alla gör ju så"-argumentation.

## Beslut

Application-handlers injicerar `IAppDbContext` direkt. `IAppDbContext` är ett interface definierat i `JobbPilot.Application`, implementerat i `JobbPilot.Infrastructure` av EF Core:s `AppDbContext : DbContext, IAppDbContext`.

`IUnitOfWork` är pipeline behavior-abstraktionen (ADR 0008) som hanterar transaction-scope runt commands via `SaveChangesAsync()`. Queries kör utan UoW-behavior.

Specificering av `IAppDbContext`:
- Exponerar `DbSet<T>` properties för varje aggregate root
- Exponerar `SaveChangesAsync(CancellationToken)` 
- Exponerar inget EF Core-specifikt utöver det (inga `ChangeTracker`, `Database`, etc. i interfacet)

## Konsekvenser

**Positivt:**

- EF Core:s DbContext är redan en Unit of Work och ett in-memory Repository — att lägga Repository ovanpå är dubbelarbete utan nytt värde
- `IQueryable<T>` behöver inte wrappas eller läcka igenom ett abstrakt repository-interface (ett klassiskt Clean Arch-brott i Repository-implementationer)
- Handlers är direkta, läsbara och utan extra lager att navigera igenom
- Tester mot riktig databas (Testcontainers/PostgreSQL) ger higher-fidelity garantier än Repository-mockar
- Lättare att dra nytta av EF Core-features (value conversions, owned entities, compiled queries) utan att abstrahera bort dem

**Negativt:**

- Handlers är direkt beroende av EF Core-interfaces (via `IAppDbContext`) — databas-byte kräver Infrastructure-refactor
- Ingen enkel "swap in-memory store" för tester — kräver Testcontainers eller SQLite (som har egna begränsningar)

**Mitigering:**

- Databas-byte är ett Infrastructure-concern (Clean Architecture-garantin) — Repository-pattern löser inte detta problem
- Testcontainers är standard i jobbpilot-teststack (se CLAUDE.md §7)
- EF Core In-Memory provider är förbjuden (false positives på transaktioner, constraints, concurrency) — SQLite i-memory är acceptabelt för enkla tester men Testcontainers är gold standard

## Alternativ övervägda

**Alt 1 — Generic Repository `IRepository<T>`:** Klassiskt mönster, avvisat. Abstraherar IQueryable utan att lösa databas-byteproblemet (som är Infrastructure-concern). Lägger till ett lager att navigera och testa utan arkitekturellt värde. Se CLAUDE.md §5.1.

**Alt 2 — Repository per aggregate (`IApplicationRepository`, `IJobSeekerRepository`):** Mer specifikt och testbart, men fortfarande dubbelarbete ovanpå EF Core. Handlers slutar direkt mot interface som mirrors DbSet-operations med extra namngivningssteg.

**Alt 3 — Specification pattern från start:** Skjuts till Fas 1+ när komplex query-logik faktiskt finns. CLAUDE.md §3.6 tillåter `ISpecification<T>` om samma filtrering används på 3+ ställen. En Specification i Application-lagret bryter inte mot principen — en Repository gör det.

**Alt 4 — Dapper för queries, EF Core för commands:** Välmotiverat i hög-prestanda-system. Overengineering för JobbPilots skala i Fas 0–1. Kan introduceras per query om profildata visar behov.

## Implementationsstatus

**Beslutsdatum:** 2026-04-19 (Fas 0 kod-scaffolding session 6)

**Ej implementerat än:** `IAppDbContext` och `AppDbContext` skrivs i Fas 1. Denna ADR dokumenterar beslutet innan implementation.

**Påverkar:**
- `JobbPilot.Application` — definierar `IAppDbContext`-interface
- `JobbPilot.Infrastructure` — implementerar `AppDbContext : DbContext, IAppDbContext`
- Architecture tests — verifierar att inget `Repository`-suffix finns i Application eller Infrastructure (utom eventuella Specification-klasser)
