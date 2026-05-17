# CTO-rekommendation — rev2-pass på REVIDERAD /ansokningar-redesign-plan (skrivväg)

**Datum:** 2026-05-17
**Roll:** senior-cto-advisor (decision-maker, read-only)
**Pass:** rev2 — fokus på det NYA (skrivväg §1.5/§7, ADR 0048 Accepted-direkt, STOPP 3-takt). STOPP 2-passet (ac00cccfcd6962a67) antas giltigt där oförändrat.
**Underlag:** `docs/design/ansokningar-redesign-plan.md` (reviderad) + `docs/reviews/2026-05-17-fas3-ansokningar-datamodell-architect.md` (a4c1483aeaee7fcea).
**Kodverifierat denna review:** `Application.cs:8-66` (Create-signatur, ingen manuell metadata, ingen invariant idag), `CreateApplicationCommand.cs:8-12` (endast `JobAdId?, CoverLetter?`), ADR 0032 §4 rad 138-141 (`ExternalReference.Create` failar för `JobSource.Manual`), ADR 0032 D1/D3 (primitive obsession / separat-aggregat avvisade), ADR 0046 Beslut 1 = A+D, Beslut 1 D = "DoD-verifiering av befintlig 95%-vertikal körs först vid Fas 3-stängning".

---

## Beslut

**Planen rev2 godkänd för STOPP 3 — med tre bindande plan-justeringar (J1–J3) och en Klas-STOPP-flagga (oförändrad: ADR 0048-precedens, redan planerad i STOPP A-granskningen).** Architect-beslutet (Variant A som `ManualPosting` VO, invariant i `Application.Create`) är korrekt infoldat och jag bekräftar det fullt ut mot principer.

---

## Granskningspunkt 1 — ManualPosting VO + invariant `JobAdId ⊕ ManualPosting`

**Architect-beslutet är korrekt infoldat och jag delar det entydigt.**

- **Primitive obsession-domen står (Fowler 2018; Evans 2003 "Value Objects"; CLAUDE.md §5.1):** Lösa kolumner avvisade rätt. ADR 0032 §4 etablerade *exakt* denna precedens: `(Source, ExternalId)` → `ExternalReference` VO, med avvisat D1 ("strängpar direkt på JobAd = classic primitive obsession", ADR 0032:292). Att lösa manuell jobbref annorlunda vore inkonsekvent mot egen skriven precedens (REP/CCP, Martin 2017 kap. 13 — samma kunskapsdel, samma form).
- **Invariant-placeringen är korrekt (CLAUDE.md §2.2; Evans 2003 "Aggregates"):** `JobAdId ⊕ ManualPosting` hör i `Application.Create`, inte i handlern. Kodverifierat: `Application.Create` (`Application.cs:46-66`) är redan invariant-vakten (JobSeekerId, CoverLetter-längd) — invarianten läggs där det redan finns en konsistensgräns, inte i en ny otestbar handler-gren. Korrekt.
- **Inget DDD-problem med ManualPosting som VO på Application — tvärtom korrekt.** Den oro frågan reser ("denormaliserad jobbmetadata i fel aggregat") gäller *Variant B*, inte Variant A. När `JobAdId == null` finns ingen JobAd — `ManualPosting` är då **ansökningens egen data** (vad job-seekern själv skrev om jobbet hen sökte), inte en denormaliserad spegel av en extern katalogpost. Den har ingen egen livscykel, inga invarianter utanför Application-kontexten, ingen domän-identitet skild från sin ansökan → den hör som VO *i* Application-aggregatet (Vernon 2013, "Effective Aggregate Design": modellera inte som aggregat det som saknar egen consistency boundary). Architects tredje-variant-avvisning (separat ManualPosting-aggregat) är korrekt av samma skäl som ADR 0032 D3.
- **Degenererat tillstånd bevarat korrekt:** Både null = befintliga cover-letter-only-rader. Architect och plan §1.5/§7 hanterar detta som tillstånd 3 (fallback "Ansökan #{kort-id}"). Ingen tvångs-backfill — korrekt LSP/migration-säkerhet.

**Ingen ändring.** Punkt 1 är vattentät.

## Granskningspunkt 2 — §7 create-flöde + 3-tillstånds-projektion

**Logiskt komplett. En semantik-fråga och en lager-fördelnings-bekräftelse.**

- **3-tillstånds-projektionsordningen (JobAd → ManualPosting → fallback) är komplett och deterministisk.** Tillstånd 1/2/3 är ömsesidigt uttömmande givet invarianten i punkt 1. Korrekt.
- **`PublishedAt`-frågan — `Application.CreatedAt` som visnings-datum: ACCEPTABELT, men kräver plan-justering J1.** Det är *inte* en semantik-läcka i datamodellen — `ManualPosting` får medvetet inget `PublishedAt`-fält (det vore en data-lögn: job-seekern vet inte när annonsen publicerades). Att i *read-projektionen* visa `Application.CreatedAt` är ett legitimt presentationsval. **MEN:** `JobAdSummaryDto.PublishedAt` är idag `DateTimeOffset` (non-null) med semantiken "annonsens publiceringsdatum". Att tysta fylla den med `Application.CreatedAt` för manuella poster gör DTO-fältet semantiskt överlastat — en konsument kan inte skilja "annons publicerad X" från "ansökan skapad X". Detta är en SoC-läcka i DTO-kontraktet (Dijkstra 1974; Martin 2017 kap. 7 — fältet bär då två change-reasons).
  → **J1 (bindande): DTO-kontraktet måste göra härkomsten explicit.** Antingen (a) `PublishedAt` blir `DateTimeOffset?` och är `null` för manuell (frontend visar "—" eller utelämnar "Publicerad"-raden, konsekvent med befintlig `ExpiresAt?`-hantering i §3), ELLER (b) ett explicit `DateKind`/`SourceKind`-fält. Variant (a) är minsta ingrepp och konsekvent med planens egen `ExpiresAt?`-nullable-mönster — **jag beslutar (a)**. Frontend §3 JobInfoPanel: visa inte "Publicerad {datum}" för manuell post; den raden utelämnas (samma mönster som "Sista ansökningsdag —"). Att visa `Application.CreatedAt` *märkt som* "Publicerad" är den faktiska semantik-läckan och avvisas.
- **Defense-in-depth-lagerfördelningen är korrekt (Martin 2017 kap. 7 SRP; Evans 2003):** formulär (UX-snabbåterkoppling) + command-validator (applikationskontrakt, FluentValidation) + VO-factory (`ManualPosting.Create` = domän-invariant, sista sanningen). De tre lagren validerar **olika oro** (UX / kontrakt / invariant), inte samma regel tre gånger — det är legitim defense-in-depth, inte DRY-brott. URL-scheme-whitelist (TD-80/OWASP A01) ska bo i VO-factoryn som enda sanning; formulär/validator får spegla för UX men domänen är auktoritativ. Korrekt.

## Granskningspunkt 3 — skrivväg-scope + ADR 0046 Beslut 1 D

**Architects scope-flagga är legitim och korrekt höjd. Klas har för-auktoriserat skrivväg+migration i STOPP A-direktivet — men ADR 0046-konsekvensen kräver en explicit notering (J2).**

- **Skrivvägen rör Fas-1-byggd kod.** Kodverifierat: `CreateApplicationCommand` (`CreateApplicationCommand.cs:8-12`) och `Application.Create` (`Application.cs:46-66`) är befintlig, levererad Application-vertikal. ADR 0046 Beslut 1 = **A + D**, där **D = "DoD-verifiering av befintlig 95%-vertikal körs först vid Fas 3-stängning"** (ADR 0046:36,103). Att ändra `CreateApplicationCommand` + `Application.Create`-signatur + lägga ny invariant ändrar *själva den vertikal D ska DoD-verifiera*. Detta är inte en blockerare — Klas för-auktoriserade skrivvägen i STOPP A-direktivets scope-lista — men det skapar en ADR 0046-konsekvens som inte får vara tyst.
  → **J2 (bindande): Planen §1.5/§9 + ADR 0048 ska innehålla en explicit ADR 0046-konsekvensnotering:** "Skrivvägen ändrar Fas-1-byggd `CreateApplicationCommand`/`Application.Create` (ny `ManualPosting`-parameter + invariant). ADR 0046 Beslut 1 D:s DoD-verifiering av Application-vertikalen vid Fas 3-stängning **måste omfatta den utvidgade create-vägen** (ny invariant-test + create-command happy/validation-fail/cross-user obligatoriska per CLAUDE.md §7), inte bara den ursprungliga vertikalen." Detta är en notering, inte en ADR 0046-amendment — beslutet (skrivväg i scope) är Klas:s redan fattade; vi gör konsekvensen spårbar för Fas 3-stängnings-DoD. adr-keeper noterar i ADR 0048 (in-touch) + steg-tracker.
- **Avgränsningen mot "metadata-edit på existerande ansökningar" är korrekt och tight (YAGNI, Fowler 2018 kap. 3).** Plan §1.5/§4-not/§7 håller `AttachManualPosting`/metadata-edit-på-äldre-rader som egen framtida touch. Create-vägen + read-fallback är ett komplett, koherent snitt; metadata-edit är genuint annan fas (CLAUDE.md §9.6 kriterium 1 — featuren finns inte, ska inte byggas spekulativt). Korrekt avgränsat. Befintliga cover-letter-only-rader behåller fallback (tillstånd 3) — ingen tvångskoppling. Korrekt.
- **Gate-uppsättningen (§1.4) är korrekt för skrivväg-utvidgning (CLAUDE.md §9.2):** db-migration-writer (ny), test-writer TDD (invariant + create-command + ManualPosting-fallback + soft-deleted-via-default-join), security-auditor BLOCKING (URL-input = TD-80-yta + cross-user), dotnet-architect (VO + join SQL), code-reviewer. Fullständig. Ingen ändring.

## Granskningspunkt 4 — ADR 0048 Accepted-direkt + §1.4(d) write-side-avgränsning

- **§1.4(d)-formuleringen är vattentät — ingen smyg-vidgning via ManualPosting.** Architect-rapporten (A4, samt Variant B-avvisningsskäl 4) fastställer kodgrundat att Variant A håller skrivvägen **single-aggregate** (VO på Application-raden, ingen extra persistens, ingen cross-aggregat-write). Variant B avvisades *delvis just för* att den hade vidgat ADR 0048 från read-join till write-side multi-aggregate-create. §1.4(d) ("ADR 0048 vidgas EJ till write-side multi-aggregate-create — Variant B avvisades just för att inte vidga detta") binder detta explicit i ADR-texten. ADR 0048 förblir korrekt **read-join-scoped (1:0..1, samma DbContext)**. Bekräftat vattentätt.
- **"Accepted direkt" är försvarbart — men det är ett Klas-beslut, inte ett CTO-beslut, och det är korrekt så.** ADR-lifecycle-disciplinen (CLAUDE.md §1.6; adr-keeper Proposed→Accepted; Nygard 2011) finns för att en *teknisk cooldown* ska fånga invändningar innan ett precedensbeslut låses. Här är invändnings-fönstret redan konsumerat: STOPP 2-CTO-passet höjde ADR 0048-kravet, architect-rev:n verifierade write-side-avgränsningen kodgrundat, och **Klas är beslutsfattaren som äger Proposed→Accepted-transitionen** (det är Klas-STOPP-punkten, inte adr-keepers). När beslutsfattaren själv fattar beslutet med full review-trail framme är en separat Proposed-cooldown formell ceremoni utan tillförd granskning (YAGNI applicerad på process; Nygard 2011 kräver att beslutet är *dokumenterat och spårbart*, inte att det passerar ett visst antal tillstånd). **Försvarbart.** Villkor: ADR 0048 måste i Accepted-texten bära hela motiveringskedjan (kontrast mot ADR 0043, query-filter-disciplin §1.2, write-side-avgränsning §1.4(d)) så Accepted-direkt inte blir Accepted-tunt. Plan §1.4 beskriver redan detta innehåll — bindande att adr-keeper skriver det fullt, inte en stub.

**Ingen ändring av plan-substansen i punkt 4. ADR 0048-precedensbeslutet förblir Klas-STOPP** — oförändrat från STOPP 2 (CTO Beslut 4), redan inplanerat i STOPP A-granskningen. Detta är *inte* en ny Klas-STOPP.

## Granskningspunkt 5 — STOPP 3-takt + broken intermediate state

**Den föreslagna takten är korrekt MEN behöver en explicit atomicitets-regel för backend-leden (J3).**

- **Sekvensen STOPP 3a backend → Klas+SQL-verifiering → STOPP 3b frontend → Klas live-verify är rätt grovstruktur.** Backend måste landa och verifieras (SQL = en LEFT JOIN, migration applicerad) innan frontend byggs ovanpå — annars verifierar Klas frontend mot ett ospårat backend-kontrakt.
- **Broken-intermediate-state-risken (memory `feedback_di_with_handlers_same_commit`-andan) är reell inom STOPP 3a och måste hanteras med atomicitetsregel.** STOPP 3a innehåller: (i) Domain VO + invariant, (ii) EF-mappning + migration, (iii) `CreateApplicationCommand` + handler + validator, (iv) read-handler ManualPosting-fallback-projektion. Dessa får **inte** splittas i commits som lämnar trädet i ett tillstånd där:
  - migration finns men `Application.Create` saknar `ManualPosting`-parametern (eller omvänt) → schema/kod-divergens, CI-rött (samma klass som DI-utan-handler);
  - `CreateApplicationCommand` tar `ManualPostingInput` men read-handlers projicerar inte ManualPosting → manuell ansökan skapas men visas som tillstånd-3-fallback ("Ansökan #kort-id") = exakt den defekt Klas underkände två gånger, återinförd i ett mellanläge.
  → **J3 (bindande): STOPP 3a backend levereras som EN atomisk batch** (Domain + EF/migration + Command/handler/validator + read-handler-fallback-projektion + deras tester), en push, innan STOPP-rapport till Klas. Naturlig split-batch *inom* 3a tillåts endast om varje commit är CI-grön och self-consistent (migration + matchande domänkod alltid i samma commit; read-fallback i samma commit som write-vägen den speglar). Frontend (3b) får splittas från backend (3a) — det är det säkra snittet (server-kontraktet är då fryst och SQL-verifierat). Det är *inom 3a* atomiciteten är icke-förhandlingsbar, inte mellan 3a och 3b.
- **3a/3b-splitten är annars korrekt och bör inte slås ihop.** Klas:s "ej non-stop, STOPP mellan" är rätt: read+write-backend hänger ihop kontraktsmässigt och verifieras tillsammans (SQL + skapa-manuell-ansökan → syns rätt i läsväg); frontend är en separat verifierbar yta (live-verify, render-VETO ADR 0047 Area 5). Att tvinga ihop allt till en non-stop-leverans skulle göra Klas:s SQL-verifierings-gate verkningslös. Sekvensen står.

---

## Plan-ändringar jag beslutar (bindande, foldas in före STOPP 3)

- **J1 — `JobAdSummaryDto.PublishedAt` blir `DateTimeOffset?`**, `null` för manuell post. Frontend (§3 JobInfoPanel, §2 list-rad) utelämnar "Publicerad {datum}"-raden för manuell post (samma mönster som `ExpiresAt?` → "—"/utelämnad). `Application.CreatedAt` får **inte** renderas märkt som "Publicerad". Uppdatera plan §1.1 (DTO-signatur), §1.5 (read-väg-integration), §7 (resultat-beskrivning), §1.3 (Zod `publishedAt` nullable). *Skäl: SoC/SRP — DTO-fält får inte bära två change-reasons (Martin 2017 kap. 7; Dijkstra 1974).*
- **J2 — ADR 0046-konsekvensnotering** läggs i plan §1.5/§9 + ADR 0048-texten: skrivvägen ändrar Fas-1-byggd `CreateApplicationCommand`/`Application.Create`; ADR 0046 Beslut 1 D:s Fas-3-stängnings-DoD-verifiering måste omfatta den utvidgade create-vägen (ny invariant-test + create-command happy/validation-fail/cross-user obligatoriska, CLAUDE.md §7). Notering, ej ADR 0046-amendment. *Skäl: spårbarhet — Fas-1-kod-touch ska inte vara tyst mot ADR 0046 D-DoD (ADR 0046:36,103; CLAUDE.md §8 DoD).*
- **J3 — STOPP 3a-atomicitet** skrivs in i plan §9/§1.4: backend (Domain VO + invariant + EF/migration + Command/handler/validator + read-handler ManualPosting-fallback + tester) = en atomisk batch, en push, ingen split som lämnar schema/kod-divergens eller write-utan-matchande-read. 3a↔3b-split bevaras. *Skäl: broken-intermediate-state-skydd (memory `feedback_di_with_handlers_same_commit`; CLAUDE.md §6.3 CI-gate).*

## Avvisade alternativ (rev2)

- **`Application.CreatedAt` renderat som "Publicerad" utan DTO-kontraktsändring (status quo i rev1-planens §7-formulering):** avvisad — semantik-läcka i DTO-kontraktet, fält med två change-reasons (J1 ersätter).
- **Separat Proposed-cooldown för ADR 0048 (strikt adr-keeper-lifecycle):** avvisad som tillförd granskning här — review-trailen är komplett framme och beslutsfattaren (Klas) äger transitionen; Accepted-direkt med full motiveringskedja uppfyller Nygard 2011 (dokumenterat + spårbart). Formell ceremoni utan granskningsvärde i detta specifika fall.
- **Slå ihop STOPP 3a+3b till en non-stop-leverans:** avvisad — gör Klas:s SQL-verifierings-gate och render-VETO verkningslösa; backend/frontend är olika verifierbara ytor.

## Trade-offs accepterade

- **`PublishedAt?` nullable bryter ett tidigare non-null-kontrakt** — accepterat: det är ärligare semantik (manuell post *har* inget publiceringsdatum), konsekvent med planens egen `ExpiresAt?`-nullable-linje, och additivt (befintliga JobAd-kopplade rader får alltid värde). Migrations-säkert.
- **STOPP 3a blir en större atomisk batch** — accepterat: batch-storlek är inget designvärde (Ford/Parsons/Kua 2017); korrekt konsistensgräns är. Splittad backend här vore exakt den defekt Klas underkände, återinförd i ett mellanläge.

## In-block-fixar (CLAUDE.md §9.6 — ingen TD)

J1–J3 är plan-justeringar, inte TDs. Inget fynd i rev2 uppfyller §9.6-kriterierna (annan fas / saknad funktion-dependency) — allt hör till denna touch och fixas in-block i plan-revisionen + STOPP 3-implementationen. Inga genuina TDs lyfts.

## Klas-STOPP-status (explicit)

- **Ingen NY Klas-STOPP utöver den redan planerade STOPP A-granskningen.** J1–J3 är entydigt principmotiverade (SoC, ADR 0046-spårbarhet, broken-state-skydd) → CC foldar in i planen och går till STOPP 3 utan extra Klas-GO (CLAUDE.md §9.6 p.5; architect-rapport flagga 1-andan).
- **Klas-STOPP kvarstår oförändrat för ADR 0048-precedensbeslutet** (CTO Beslut 4, STOPP 2) — det är redan delen av STOPP A-granskningen Klas utför nu. "Accepted direkt" är Klas:s beslut att fatta vid denna STOPP; jag bekräftar att det är ADR-lifecycle-försvarbart givet full review-trail. Ingen separat eskalering.
- **Architect scope-flagga (skrivväg+migration i Fas 3):** Klas har redan för-auktoriserat i STOPP A-direktivet; J2 gör ADR 0046-konsekvensen spårbar. Ingen ytterligare Klas-medvetenhet krävs utöver att notera J2 vid STOPP A-godkännandet.

## Referenser

- Robert C. Martin, *Clean Architecture* (2017) — kap. 7 (SRP/SoC, DTO-fält change-reason), kap. 13 (REP/CCP component cohesion, VO-precedens-konsekvens)
- Eric Evans, *Domain-Driven Design* (2003) — Value Objects, Aggregates
- Vaughn Vernon, *Implementing Domain-Driven Design* (2013) — Effective Aggregate Design (consistency boundary, ManualPosting ⊄ eget aggregat)
- Martin Fowler, *Refactoring* 2nd ed (2018) — kap. 3 Primitive Obsession, spekulativ generalitet (metadata-edit-avgränsning)
- E. W. Dijkstra (1974) — Separation of Concerns (DTO-kontrakt)
- Michael Nygard (2011) — "Documenting Architecture Decisions" (Accepted-direkt: dokumenterat + spårbart, ej tillstånds-ceremoni)
- Ford/Parsons/Kua, *Building Evolutionary Architectures* (2017) — fas-/batch-storlek ej designvärde
- CLAUDE.md §2.2, §3.3, §5.1, §6.3, §7, §8, §9.2, §9.6, §12 · ADR 0032 §4 (rad 138-141, D1/D3) · ADR 0046 Beslut 1 D (rad 36,103) · ADR 0048 (plan §1.4, read-join-scoped) · ADR 0043 Beslut C (kontrast)
- Kodverifierat: `Application.cs:8-66`, `CreateApplicationCommand.cs:8-12`, `0032-jobtech-integration.md:138-141,292-293`, `0046-*.md:36,103`
- Föregående pass: senior-cto-advisor STOPP 2 (ac00cccfcd6962a67), dotnet-architect datamodell (a4c1483aeaee7fcea)
