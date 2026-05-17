# Arkitektur-beslut — datamodell för manuella ansökningar (FAS 3 /ansokningar-redesign)

**Datum:** 2026-05-17
**Roll:** dotnet-architect som arkitektur-decision-maker (Klas-eskalerat från STOPP 1, samma mönster som CTO för Variant A/B save-strategi)
**Scope:** skrivväg + datamodell för MANUELLA ansökningar (`Application.JobAdId == null`)
**Status:** Read-only-pass — INGEN kod skriven. Entydigt beslut nedan.

---

## Sammanfattning

**Beslut: Variant A — `ManualPosting` value object på `Application` (INTE lösa nullable-kolumner, INTE Variant B).**
Variant B (lokal `JobAd` med `Source=Manual`) är **ett invariant-/semantik-brott** mot ADR 0032 och måste avvisas — det är inte en giltig variant. Variant A i sin lösa-kolumn-form vore primitive obsession (CLAUDE.md §5.1); rätt form är ett value object. Beslutet är **entydigt mot principer** — Klas-STOPP krävs INTE för datamodell-valet i sig. Klas-STOPP kvarstår dock för **ADR 0048-precedensbeslutet** (redan fastställt i plan §1.4 / CTO Beslut 4), och en mindre **scope-flagga** lyfts nedan (manuell skrivväg ⇒ migration ⇒ ADR 0046 D-DoD-konsekvens).

---

## Verifierad kodgrund (ingen gissning)

| Fynd | Källa (verifierad) |
|---|---|
| `Application.JobAdId` är `JobAdId?` (nullable), `Create(jobSeekerId, jobAdId?, coverLetter?, clock)` | `Application.cs:11,46-66` |
| `Application` har INGEN manuell jobbmetadata idag | `Application.cs:10-18` |
| `CreateApplicationCommand(Guid? JobAdId, string? CoverLetter)` — endast dessa två fält | `CreateApplicationCommand.cs:8-12` |
| **`JobAd.ValidateCore` gör `Url` obligatorisk + tvingar giltig absolut http(s)-URL** (TD-80 scheme-whitelist) | `JobAd.cs:155-189` (rad 177-183) |
| `JobAd.Create` (publik Manual-factory) kör `ValidateCore` → Url-kravet gäller även Manual | `JobAd.cs:50-70` |
| `JobAd.Description` obligatorisk (non-empty) | `JobAd.cs:168-170` |
| `JobAd.Company` obligatorisk non-empty VO | `Company.cs:11-21` |
| `JobSource.Manual` existerar redan | `JobSource.cs:11` |
| **`ExternalReference.Create` FAILAR explicit för `JobSource.Manual`** ("ExternalReference kräver extern källa, inte Manual") | ADR 0032 §4 rad 138-141 |
| `JobAd` global query filter `HasQueryFilter(j => j.DeletedAt == null)` | `JobAdConfiguration.cs:82` |
| 3 read-handlers projicerar `app.JobAdId.Value.Value` med null-guard | `GetApplicationByIdQueryHandler.cs:57`, `GetPipelineQueryHandler.cs:40`, `GetApplicationsQueryHandler.cs:50` |
| ADR 0032 §8 + amendments: `JobAd.raw_payload` behandlas som **rekryterar-PII / publik annons-metadata** med egen GDPR-pipeline (sanitizer, 30d-retention, right-to-erasure via `RedactRecruiterPiiCommand`) — JobAd ≠ job-seeker-användardata | ADR 0032 §8 + amendments 2026-05-12/05-13 |
| ADR 0009: `IAppDbContext` = aggregate-per-DbSet; `JobAd` är aggregate root | ADR 0009 + ADR 0043 Beslut C |

---

## Variant B avvisas — invariant- och semantik-brott (inte en giltig variant)

Variant B ("Ny manuell ansökan skapar lokal `JobAd` Source=Manual, sedan `Application` med `JobAdId`") bryter mot **fyra** verifierade gränser:

1. **JobAd-invariant (hård, kod-verifierad):** `JobAd.ValidateCore` (`JobAd.cs:177-183`) kräver en giltig absolut **http(s)-URL** — inte nullbar, inte tom. En manuell ansökan skapas idag med *enbart* `coverLetter` (`CreateApplicationCommand.cs:8-12`); ingen URL finns. Att skapa en lokal `JobAd` skulle kräva antingen (a) att luckra upp `ValidateCore` (försvagar TD-80 XSS-scheme-whitelist — en security-auditor-fastställd invariant, regression mot OWASP A01) eller (b) en syntetisk placeholder-URL (data-lögn i annons-katalogen). Båda är otillåtna. `Description` + `Company` är dessutom obligatoriska non-empty (`JobAd.cs:168-170`, `Company.cs:13-15`) — manuell ansökan har inget av detta garanterat.

2. **Ingen icke-import Create-väg passar:** `JobAd.Create` är den enda Manual-factoryn men kör samma `ValidateCore` (Url-kravet gäller). `JobAd.Import` / `UpdateFromSource` kräver `ExternalReference`, och **`ExternalReference.Create` failar by design för `JobSource.Manual`** (ADR 0032 §4 rad 138-141). Det finns alltså ingen domän-väg som producerar en användarskapad `JobAd` utan URL utan att bryta en invariant.

3. **ADR 0032 §8 semantik-brott (GDPR):** ADR 0032 etablerar `JobAd` som **extern, publicerad annons-katalog** vars privacy-modell behandlar innehållet som rekryterar-PII med en dedikerad pipeline (`JobTechPayloadSanitizer`, `PurgeStaleRawPayloadsJob` 30d-retention, `RedactRecruiterPiiCommand`). Att stoppa in **job-seeker-skapad användardata** (vad användaren skrev om jobbet hen sökte) i `JobAd` blandar två GDPR-data-kategorier med olika rättslig grund (legitimt intresse / publicerad annons vs. samtycke / användardata), olika retention och olika data-subject. `PurgeStaleRawPayloadsJob`-mönstret och soft-delete/sync-jobben är dimensionerade för extern katalog — en användar-`JobAd` blir en främmande kropp i den modellen. Detta är ett **Aggregate Consistency Boundary-brott** (Vernon 2013): `JobAd`-aggregatets invarianter och livscykel (Archive vid removal-event, sync-upsert, retention-purge) antar extern härkomst.

4. **Ökad cross-aggregat-koppling i skrivvägen + ADR 0048-konsekvens:** Variant B kräver att `CreateApplication`-handlern skapar och persisterar *två* aggregat (`JobAd` + `Application`) i en write — en cross-aggregat-skrivtransaktion som varken ADR 0009 eller plan-§1.4:s ADR 0048-precedens (som gäller en **läs**-join, 1:0..1) täcker. Det vidgar ADR 0048:s blast-radius från read-only-join till write-side multi-aggregate-create utan motiverande behov.

**Slutsats:** Variant B är inte "variant B" — det är ett designfel som river tre dokumenterade invarianter (kod-verifierad Url-invariant, ADR 0032 §4 Manual-exkludering, ADR 0032 §8 GDPR-semantik). Per CLAUDE.md §12 ("verkar kringgå Clean Arch-gränserna / ändrar security-kritisk kod utan tester → stoppa") avvisas den utan vidare avvägning.

---

## Variant A — vald, men som value object (inte lösa kolumner)

### A1. Lösa nullable-kolumner avvisas (primitive obsession)

Fem lösa `ManualTitle/ManualCompany/ManualSource/ManualUrl/ManualExpiresAt`-kolumner är **textbok primitive obsession** (CLAUDE.md §5.1; Fowler 2018 "Primitive Obsession"; Evans 2003 "Value Objects"). De fem fälten har:

- **Sammanhållen identitet** — de beskriver tillsammans *en sak*: "den manuellt angivna jobbreferensen".
- **Gemensam invariant** — antingen är referensen satt (titel måste finnas) eller helt frånvarande; URL måste vara giltig http(s) om angiven (samma TD-80-resonemang som `JobAd`); källa default `Manual`.
- **Value-equality + immutability** — exakt det `record`-VO:t är till för (CLAUDE.md §3.3).

ADR 0032 §4 etablerar redan **precis denna precedens**: `(Source, ExternalId)` blev VO `ExternalReference` "Primitive obsession förbjuden — har value-equality, immutability och invariant" (ADR 0032 §4 rad 177-180; avvisat alternativ D1 "strängpar direkt på JobAd"). Att lösa det annorlunda för manuell jobbref vore inkonsekvent mot egen precedens.

### A2. Form: `ManualPosting` value object, owned-type på `Application`

```
// Domain/Applications/ManualPosting.cs (skiss — implementeras i STOPP 3)
public sealed record ManualPosting
{
    public string Title { get; }
    public string? Company { get; }
    public string? Url { get; }
    public DateTimeOffset? ExpiresAt { get; }
    // Source utelämnas: en ManualPosting ÄR per definition Manual.
    // Lägg inte ett JobSource-fält som bara kan ha ett värde (YAGNI/dead axis).

    public static Result<ManualPosting> Create(
        string? title, string? company, string? url, DateTimeOffset? expiresAt)
    {
        // Title obligatorisk (annars finns ingen "posting" — då ska hela VO:t vara null)
        // Url: om angiven MÅSTE den passera samma http(s)-whitelist som JobAd.ValidateCore
        //      (TD-80 / OWASP A01 — återanvänd regeln, duplicera inte slarvigt)
        // Längd-caps i linje med JobAd (Title ≤300, Company ≤200)
    }
}
```

`Application.JobAdId? : ManualPosting?` — **nullable owned-type** (EF Core `OwnsOne`, kolumnerna NULL för existerande/JobAd-kopplade rader → migration-säker, additiv, ingen backfill).

### A3. Aggregat-invariant — JA, krävs (kod-verifierad lucka)

`Application.Create` (`Application.cs:46-66`) tar idag `jobAdId?` + `coverLetter?`. En ny `manualPosting?`-parameter MÅSTE ha en **aggregat-skyddad invariant** (CLAUDE.md §2.2 "aggregates skyddar sina invarianter i konstruktorer/metoder, inte i handlers"):

> **Invariant: `JobAdId` och `ManualPosting` är ömsesidigt uteslutande.**
> `(JobAdId is not null && ManualPosting is not null)` ⇒ `Result.Failure` i `Create`.
> Båda får vara null (degenererat "tom ansökan" — bevaras, det är dagens beteende `JobAdId==null && coverLetter`-only). En manuell ansökan = `JobAdId is null && ManualPosting is not null`. En kopplad = omvänt.

Detta hindrar exakt det motstridiga tillstånd uppdraget pekar på ("om JobAdId!=null OCH Manual-fält satta = motstridigt"). Invarianten hör i `Application.Create` (och en ev. framtida `AttachManualPosting`/`LinkJobAd`-metod), aldrig i handlern.

### A4. Read-väg-konsekvens (plan §1 / §7) — förenklas

Plan §1.2:s `JobAdSummaryDto`-fallback blir **renare** med VO: handlern projicerar `JobAdSummaryDto` från (a) joinad `JobAd` om `JobAdId != null`, annars (b) `Application.ManualPosting` om satt, annars (c) `null` → fallback "Ansökan #{kort-id}" (plan §7). Den planerade left-joinen (plan §1.2, ADR 0048-precedens) är **oförändrad** och berörs inte av detta beslut — VO:t ligger på `Application`-raden, ingen extra join. ADR 0048:s scope (read-side 1:0..1-join) påverkas **inte** av Variant A; det vidgas däremot av Variant B (skäl varför B även på den axeln är sämre).

### A5. Migration-säkerhet

Owned-type `OwnsOne` med nullable-kolumner = `ADD COLUMN ... NULL` för fyra kolumner på `applications`. Inga befintliga rader berörs (alla får NULL = "ingen manuell posting", semantiskt korrekt — de är antingen JobAd-kopplade eller degenererade tomma). Ingen backfill, ingen NOT NULL, ingen data-migration. Låg risk. Kräver `db-migration-writer`-gate (CLAUDE.md §9.2) — se scope-flagga nedan.

---

## Tredje-variant-övervägande (avvisad, för fullständighet)

**Separat `ManualPosting`-aggregat** (analogt med ADR 0032 avvisade D3 `SourcedJobAd`): avvisas av samma skäl som ADR 0032 D3 — YAGNI + bryter aggregate-design. En manuell jobbref har ingen egen livscykel, inga egna invarianter utöver `Application`-kontexten, ingen domän-identitet skild från sin ansökan. Den hör som VO *i* `Application`-aggregatet (Vernon 2013 "Effective Aggregate Design" — modellera inte som aggregat det som saknar egen consistency boundary).

---

## Flaggor till Klas

1. **Datamodell-valet (A med VO) är entydigt mot principer — Klas-STOPP krävs INTE** för själva A-vs-B-beslutet. CC kan gå direkt till STOPP 3-implementation av VO + invariant + migration efter denna rapport (CLAUDE.md §9.6 p.5: entydigt principmotiverat CTO/architect-beslut → ingen extra Klas-GO).

2. **Klas-STOPP kvarstår oförändrat för ADR 0048-precedensbeslutet** (plan §1.4 / CTO Beslut 4) — det är ett separat beslut och berörs inte av denna rapport.

3. **SCOPE-FLAGGA (kräver Klas-medvetenhet, ev. STOPP):** Planen (§1, rad 13) säger explicit **"Ingen migration — endast projektion (läsväg)"**. Variant A *kräver* en migration (A5) + ändrad `CreateApplicationCommand` + `/ansokningar/ny`-frontend (ny titel/företag/url/sista-dag-inmatning) + `db-migration-writer`-gate. Detta är **skrivväg**, vilket uppdraget korrekt identifierar som "halva problemet" — men det vidgar plan-scopet bortom den deklarerade no-migration-läsväg-gränsen. Detta bör läggas till planen som en explicit skrivväg-batch (egen STOPP-rapport) och korsrefereras mot **ADR 0046 Beslut 1 D** (DoD-verifiering av Application-vertikalen) eftersom `CreateApplicationCommand`-ändring rör Fas-1-byggd kod. Rekommendation: behandla skrivvägen som egen sub-batch med `db-migration-writer` + `test-writer` (ny invariant-test obligatorisk per CLAUDE.md §7) + `security-auditor` (URL-input från användare → samma TD-80-yta) FÖRE STOPP 3-implementation. **Klas bör bekräfta att skrivväg+migration ingår i FAS 3-scope** (det är en genuin scope-utvidgning mot plan rad 13, inte en in-block-detalj).

---

## Beslut (sammanfattat)

| Fråga | Svar |
|---|---|
| Variant A eller B? | **A** |
| A som lösa kolumner eller VO? | **VO (`ManualPosting`)** — lösa kolumner = primitive obsession (CLAUDE.md §5.1, konsekvent med ADR 0032 §4-precedens) |
| Aggregat-invariant behövs? | **JA** — `JobAdId` ⊕ `ManualPosting` ömsesidigt uteslutande, skyddad i `Application.Create` |
| Variant B — bryts ADR 0032 §8? | **JA** — + ADR 0032 §4 (Manual-exkludering) + kod-verifierad Url-invariant + Aggregate Consistency Boundary. Avvisad som invariant-brott. |
| GDPR-konsekvens Variant B? | Blandar två data-kategorier (job-seeker-data i extern-annons-katalog) — avvisad |
| ADR 0048-konsekvens? | Variant A: **ingen** (VO på Application-raden, ingen extra join). Variant B: vidgar ADR 0048 från read-join till write-side multi-aggregate-create — sämre |
| Klas-STOPP för datamodell-valet? | **Nej** — entydigt mot principer |
| Klas-STOPP/medvetenhet i övrigt? | **Ja** — scope-flagga (skrivväg+migration mot plan rad 13 "ingen migration"; ADR 0046 D-konsekvens) |

---

## Referenser

- CLAUDE.md §2.2 (aggregat skyddar invarianter), §3.3 (records/VO), §5.1 (primitive obsession; no-Repository), §9.2 (gates), §9.6 (in-block vs Klas-STOPP), §12 (stoppa vid Clean Arch-kringgång)
- ADR 0032 §4 (`ExternalReference` VO-precedens; `JobSource.Manual` exkluderad från ExternalReference; avvisat D1/D3), §8 + amendments 2026-05-12/05-13 (JobAd = extern annons-katalog med rekryterar-PII-pipeline, ej job-seeker-data)
- ADR 0009 (aggregate-per-DbSet; ingen Repository) · ADR 0043 Beslut C (cross-context-port-precedens, kontrast) · ADR 0046 Beslut 1 D (Application-vertikal DoD — skrivväg-konsekvens) · ADR 0048 (Proposed, plan §1.4 — read-join-precedens, opåverkad av Variant A)
- Verifierad kod: `Application.cs`, `JobAd.cs` (ValidateCore rad 155-189), `Company.cs`, `JobSource.cs`, `CreateApplicationCommand.cs`, `JobAdConfiguration.cs:82`, 3 read-handlers
- Eric Evans, *Domain-Driven Design* (2003) — Value Objects, Aggregates
- Vaughn Vernon, *Implementing Domain-Driven Design* (2013) — Effective Aggregate Design (consistency boundary)
- Robert C. Martin, *Clean Architecture* (2017) §5.1-motsvarande; Martin Fowler, *Refactoring* (2018) — Primitive Obsession
- OWASP A01:2021 / TD-80 (URL-scheme-whitelist — bevaras i `ManualPosting.Create`)
