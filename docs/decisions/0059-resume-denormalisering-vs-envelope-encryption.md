# ADR 0059 — Resume-denormalisering inom aggregat (driven av ADR 0049 envelope-encryption)

**Datum:** 2026-05-20
**Status:** Accepted
**Beslutsfattare:** Klas Olsson; senior-cto-advisor 2026-05-20 (multi-approach-dom Beslut 3); dotnet-architect 2026-05-20 (design-pass)
**Relaterad:** ADR 0049 (envelope-encryption som driver), ADR 0058 (F6 P3 paus-beslut + spec), ADR 0008 (CQRS pipeline), ADR 0009 (IAppDbContext direkt), ADR 0045 (perf-budget)

---

## Kontext

F6 Prompt 3 frontend (CV v3-design enligt HANDOVER §7.4 + målbild 09-cv-light.png) kräver att `/cv`-listan renderar 6 fält per CV-kort: `name`, `language`, `latestRole`, `sectionCount`, `topSkills`, `updatedAt`. ADR 0058 Beslut 2 dokumenterar att 5 av dessa fält saknas i live `ResumeListItemDto` och kräver backend-utvidgning.

Tre av de saknade fälten (`LatestRole`, `SectionCount`, `TopSkills`) är **deriverade från `ResumeContent`** — det strukturerade CV-innehållet (PersonalInfo/Experiences/Educations/Skills/Summary) som lagras i `resume_versions.content` som JSONB.

ADR 0049 (PII-fält-kryptering via KMS-envelope) gör `ResumeContent` **client-side-krypterad**: kolumn `content_enc` innehåller AES-256-GCM-ciphertext med per-användar-DEK, dekryptering sker i `FieldEncryptionSaveChangesInterceptor` vid materialisering. Detta är en **immutable arkitektur-constraint** — JSONB-fältet är opaque för PostgreSQL: ingen SQL-LINQ-projektion kan läsa `Experiences[0].Role` utan att hämta hela rad, dekryptera, parsa JSON och projicera in-memory.

Två naiva approacher övervägdes:

1. **Query-time-projektion:** `GetResumesQueryHandler` hämtar hela `Resume + Versions + Content` per CV i list-sidan, låter interceptorn dekryptera, projicerar in-memory. För en list-vy med N CVs blir det N JSON-deserialiseringar + N JSONB-läsningar — inte N+1 SQL-queries (kan göras single-trip med `Include`), men **N+1 av krypto/parse-cykler**. Per ADR 0045 read-query p95 300ms-budget är detta i hot-zone, särskilt vid skala.

2. **On-demand denormalisering i query-handler:** projektion via Npgsql `jsonb_path_query` mot `content_enc` — **omöjligt**, kolumnen är opaque ciphertext.

Klas-CTO-dom 2026-05-20 valde Alt B (denormalisering på Resume-aggregatet, uppdaterad i samma metod som muterar Master-content). Klas-GO 2026-05-20 godkände att ADR 0059 skapas i samma STEG som backend-leveransen.

---

## Beslut

**Resume-aggregat-rooten exponerar tre denormaliserade fält som plain-text-kolumner på `resumes`-tabellen:**

- `LatestRole: string?` — senaste Experience efter `StartDate desc`, eller `null` om Experiences är tom
- `SectionCount: int` — antal populerade sektioner (0–4): Summary non-whitespace, Experiences > 0, Educations > 0, Skills > 0
- `TopSkills: IReadOnlyList<string>` — `Skills.Take(5).Select(s => s.Name).ToList()`, eller tom lista

**Mutations-pattern (synkront i samma aggregat-metod, INTE i event-handler):**

- `Resume.Create(...)` beräknar denorm från `ResumeContent.Empty(fullName)` (alla tre fält initieras till tomma värden)
- `Resume.UpdateMasterContent(content, clock)` anropar `ApplyDenormalizedProjection(content)` efter att master-versionen uppdaterats
- Ingen publik setter — endast aggregatet kan mutera fälten

**Persistence:**

- Tre nya kolumner på `resumes`: `latest_role varchar(500) NULL`, `section_count int NOT NULL DEFAULT 0`, `top_skills text[] NOT NULL DEFAULT '{}'`
- Mapping via EF Core value-converter (backing field för `_topSkills`, native Npgsql `text[]`-typ för TopSkills)
- Migration sätter defaults; **ingen backfill för existerande Resumes**

**Backfill-strategi: runtime self-heal:**

- Existerande Resumes pre-deploy får default-värden (`null/0/[]`)
- Första `UpdateMasterContent` på en existerande Resume triggar `ApplyDenormalizedProjection` → denorm-fälten fylls
- Worker-baserad batch-backfill avvisad (kräver KMS-roundtrips för dekryptering, lägger 1–2 dagars utvecklingstid, värdet är lågt i F6 där användarbasen är minimal)

**Konsistens-garanti via integrity-test:**

- Domain-unit-test verifierar `(LatestRole, SectionCount, TopSkills)` matchar `MasterVersion.Content`-projektion efter varje mutation
- Test fungerar som fitness function (Ford/Parsons/Kua 2017): denormaliserings-divergens fångas i CI, inte i produktion

**PII-bedömning (GDPR Art. 4(1)):**

- `Role` (jobbtitel) och `Skill.Name` är **professionella attribut**, inte personuppgifter i sig själv
- Värdena finns ändå i `content_enc` (krypterat) — denormalisering duplicerar dem som plain-text på samma aggregat (samma användar-scope)
- Crypto-erasure-domän (ADR 0024 + ADR 0049): vid kontoradering soft-deletas hela Resume-aggregatet → denorm-fälten försvinner med raden vid hard-delete + KMS-DEK-zeroize
- `SectionCount` är aggregat-count, inte personuppgift

---

## Konsekvenser

### Positiva

- **List-rendering förblir 3-query single-trip** (jobSeeker, count, page) utan KMS-overhead. CV-list skalar linjärt med page-storlek, inte med Resume-content-storlek.
- **Hot read-path uppfyller ADR 0045 budget** (p95 read-query 300ms) även vid större användarbas.
- **Single source of truth:** aggregatet är ensam mutator → ingen race condition mellan write-handlers och denorm-state.
- **Klar krypto-respekt:** ADR 0049 envelope-constraint bevaras — denorm-fälten är plain-text på `resumes`-rooten, separata från `content_enc` på `resume_versions`.

### Negativa

- **Existerande Resumes pre-deploy visar `null/0/[]`** tills användaren editerar (runtime self-heal). Acceptabelt i F6 (pre-launch, minimal användarbas). Trigger för framtida worker-backfill dokumenterad som potentiell post-MVP-TD.
- **Schema-evolution kostar mer:** om TopSkills går från 5→10 eller om Role-formuleringen ändras (t.ex. konkateneras med Company) krävs ny migration + worker-recalc av alla rader. Trade-off accepterad mot ADR 0049-låsning.
- **Aggregat-rooten har read-model-fält** — inte ren CQRS-write-model. Hybrid accepterad eftersom JobbPilot inte har separat read-store (precedens i §3.6 IAppDbContext direkt, ADR 0048 cross-aggregat-read-join).

### Mitigering

- Integrity-test (domain-unit) håller denorm konsistent med source-of-truth — divergens omöjlig att smyga in.
- Mutations-pattern är **synkront i samma metod** (inte event-handler) → ingen eventual-consistency-gap inom aggregatet.
- Kolumn-namn med `_` snake-case + tydliga defaults gör SQL-läsning av list-vyn självförklarande.

---

## Alternativ övervägda

### Alternativ A — Query-time-projektion via in-memory hydrering
Avvisat (CTO 2026-05-20). N+1 av krypto/parse-cykler i list-vyn bryter ADR 0045 read-budget. KMS-DEK-cache mildrar bara DEK-unwrap (1× per request), inte content-dekryptering (N× per request).

### Alternativ B — On-demand projektion via Npgsql JSONB-operators
Avvisat. `content_enc` är opaque ciphertext — `jsonb_path_query` kan inte läsa krypterad data.

### Alternativ C — Separat read-store (CQRS read-side med eventual consistency)
Avvisat (Klas + CTO). JobbPilot har inte separat read-store i någon annan domän; introducera infrastruktur för en feature är YAGNI/överengineering. ADR-precedens (0009/0048) håller read-projektion in-process.

### Alternativ D — Domain-event-handler beräknar denorm post-commit
Avvisat (architect 2026-05-20). JobbPilots `AggregateRoot.DomainEvents` saknar dispatch-mekanism — events lagras för audit/observability men konsumeras inte av Application-side notification handlers (verifierat: ingen `INotificationHandler<>`-implementation i `JobbPilot.Application`). Eventual-consistency-gap mellan write och denorm hade dessutom skapat race condition på list-rendering direkt efter write.

---

## Implementation

### Domain (`src/JobbPilot.Domain/Resumes/Resume.cs`)

```csharp
public string? LatestRole { get; private set; }
public int SectionCount { get; private set; }
private readonly List<string> _topSkills = [];
public IReadOnlyList<string> TopSkills => _topSkills.AsReadOnly();

private static (string? latestRole, int sectionCount, IReadOnlyList<string> topSkills)
    ComputeDenormalizedProjection(ResumeContent content)
{
    var latestRole = content.Experiences
        .OrderByDescending(e => e.StartDate)
        .FirstOrDefault()?.Role;

    var sectionCount =
        (!string.IsNullOrWhiteSpace(content.Summary) ? 1 : 0) +
        (content.Experiences.Count > 0 ? 1 : 0) +
        (content.Educations.Count > 0 ? 1 : 0) +
        (content.Skills.Count > 0 ? 1 : 0);

    var topSkills = content.Skills
        .Take(5)
        .Select(s => s.Name)
        .ToList();

    return (latestRole, sectionCount, topSkills);
}

private void ApplyDenormalizedProjection(ResumeContent content)
{
    var (latestRole, sectionCount, topSkills) = ComputeDenormalizedProjection(content);
    LatestRole = latestRole;
    SectionCount = sectionCount;
    _topSkills.Clear();
    _topSkills.AddRange(topSkills);
}
```

Recalc-trigger sker i `Create` (efter `_versions.Add(master)`) och `UpdateMasterContent` (efter `master.UpdateContent(...)`).

### Infrastructure (`src/JobbPilot.Infrastructure/Persistence/Configurations/ResumeConfiguration.cs`)

```csharp
builder.Property(r => r.LatestRole)
    .HasMaxLength(500)
    .HasColumnName("latest_role");

builder.Property(r => r.SectionCount)
    .HasColumnName("section_count")
    .IsRequired()
    .HasDefaultValue(0);

builder.Property(r => r.TopSkills)
    .HasField("_topSkills")
    .UsePropertyAccessMode(PropertyAccessMode.Field)
    .HasColumnName("top_skills")
    .HasColumnType("text[]")
    .HasConversion(
        v => v.ToList(),
        v => v.AsReadOnly())
    .IsRequired();
```

### Migration

`AddResumeLanguageDenormProjAndPrimaryResume` — adds tre kolumner med defaults; ingen backfill-pipeline.

### Integrity-test

`ResumeTests.UpdateMasterContent_RecalculatesDenormalizedProjection` (multi-fall) verifierar att denorm matchar Master-content efter varje mutation.

---

## Livscykel-not

Denna ADR omformulerar inte ADR 0049 — den dokumenterar en **pragmatisk koexistens** mellan envelope-encryption (känslig data via `content_enc`) och denormalisering (icke-känslig display-data på Resume-rooten) i samma aggregat. Vid framtida re-design granska om client-side-dekryptering i web-frontend (BFF-mönster) gör backend-denorm onödig.

Vid superseder: kontrollera (a) att `LatestRole`/`SectionCount`/`TopSkills` fortfarande är icke-PII enligt aktuell GDPR-tolkning, (b) att perf-budget (ADR 0045) inte ändrats så query-time blir möjligt, (c) att inga andra aggregat följt detta mönster utan medvetet ADR (CCP-bevakning).

---

## Referenser

- Eric Evans, *Domain-Driven Design* (2003), kap. 6 "Aggregates" — invariant-ägande
- Martin Fowler, *Patterns of Enterprise Application Architecture* (2002) — N+1 Selects
- Martin Fowler, "CQRS" (2011) — read-model på write-side-aggregat som hybrid
- Ford/Parsons/Kua, *Building Evolutionary Architectures* (2017) — Fitness Functions
- ADR 0049 (envelope-encryption — driver-constraint)
- ADR 0058 (F6 P3 paus + scope-spec)
- ADR 0045 (perf-budget read-query p95 300ms)
- CLAUDE.md §2.2 (DDD), §2.5 (perf granskningsbar kärnprincip), §3.6 (IAppDbContext direkt)
