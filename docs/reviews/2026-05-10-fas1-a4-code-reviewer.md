# Code-review: TD-38 A4 — Trust Server Certificate hardening

**Status:** ⚠ Changes requested
**Granskat:** 2026-05-10
**Auktoritet:** CLAUDE.md §3 (stil/namngivning), §5.1 (anti-patterns), §7 (testing)
**Scope:** `src/JobbPilot.Migrate/Program.cs`, `src/JobbPilot.Api/Dockerfile`,
`src/JobbPilot.Worker/Dockerfile`, `infra/certs/rds-global-bundle.pem` (ny).

## Sammanfattning

A4 splittar `BuildConnectionString` i två semantiskt distinkta varianter
(`BuildMigrateConnectionString` med Trust=true, `BuildPersistedConnectionString`
med VerifyFull+Root Certificate) och kopierar in RDS-CA-bundle i Api/Worker-runtime-images.
Designen är korrekt: short-lived bootstrap separerat från persisterade CS:er, VerifyFull
för det som hamnar i Secrets Manager, tydliga kommentarer om varje variants säkerhetsmotiv.
Ett blocker-fynd: `.dockerignore` exkluderar både `*.pem` och `infra/`, vilket gör att
`COPY infra/certs/rds-global-bundle.pem ...` faller direkt vid `docker build`.

## Fynd

### Blocker

**B1. `.dockerignore` exkluderar bundle från build-context**
Fil: `.dockerignore:40,44`
Nuvarande:
- Rad 40: `*.pem`
- Rad 44: `infra/`
Effekt: Båda mönstren exkluderar `infra/certs/rds-global-bundle.pem`. `docker build`
mot Api/Worker Dockerfile kommer faila med
`failed to compute cache key: "/infra/certs/rds-global-bundle.pem": not found`.
Krävs: undantag i `.dockerignore` — föreslag två rader sist i fil:
```
!infra/certs/
!infra/certs/*.pem
```
Motivering: CLAUDE.md §5.4 — secrets får inte in i image, men `rds-global-bundle.pem`
är publik AWS-CA, inte secret. Negation måste vara explicit för båda mönstren.
Delegera till: implementation-agent.

### Major

**M1. Saknat test för CS-format**
Klas:s egen punkt 4 är korrekt. Två nu-divergerande string-templates utan unit-test —
en refaktor (t.ex. byta separator, lägga till `Pooling=true`) kan tyst bryta Migrate
eller Api utan att något test fångar det. Förslag: en `BuildConnectionStringTests`
med två fall som asserter att Migrate-CS innehåller `Trust Server Certificate=true`
och Persisted-CS innehåller `SSL Mode=VerifyFull` + rätt Root-cert-path.
Motivering: CLAUDE.md §7 — handlers utan test, men här är det fritt-stående statiska
funktioner som styr säkerhets-postur. Lyft som **TD-39** om Klas inte vill ta inom A4.
Delegera till: test-writer (eller TD-39).

### Minor

**m1. `BuildMigrateConnectionString` — namn döljer Trust=true-postur**
Fil: `src/JobbPilot.Migrate/Program.cs:190`
Namnet säger "Migrate" men säkerhets-poängen är att den är *unsafe-with-justification*.
Alternativ: `BuildBootstrapConnectionString` (tydligare semantik) eller behåll och förlita
sig på header-kommentar. Inte blocker — kommentaren rad 185-189 är tydlig nog. Accept som
är.

**m2. Bundle-storlek vs kommentar-påstående**
Klas-prompt säger "~270KB". Faktisk fil: 165KB. Inte ett kod-fynd, men dokumentera
korrekt storlek i STEG-rapport.

## Approve-status per Klas-fråga

1. **§3.1/§3.2 namngivning** — OK. Async/sync-paret är beskrivande, kommentarsblocken
   bär kontexten. Minor m1 ovan.
2. **§5.1 Trust Server Certificate=true för Migrate** — **OK med dokumenterad
   motivering**. Kommentaren rad 185-189 anger short-lived, no-persistence,
   ECS-SG-only-ingress. Anti-pattern listad i §5.1 är persisterade Trust=true
   i Api/Worker — det är åtgärdat. För Migrate är trade-off accepterad: ingen
   CA-bundle i Migrate-image, dev-RDS-CA kan vara internal. Bör täckas av ADR
   eller minst en TD-not så att senare läsare hittar resonemanget utan att
   gräva i commits.
3. **Två funktioner vs `bool persisted`-flag** — **Två funktioner är rätt val**.
   En bool-flag flyttar säkerhets-beslutet till call-site där fel kan smyga in
   (`BuildConnectionString(..., persisted: false)` i en path som persisterar).
   Två funktioner gör fel-vägen omöjlig att uttrycka kort. Korrekt scope-disciplin.
4. **Test-coverage** — Major M1 ovan. Min rekommendation: TD-39 om Fas 1 är
   tight, annars in-block.
5. **`infra/certs/`-placering** — **OK**. `infra/` är tydligast — det är
   miljö-/distributionsspecifikt material som hör ihop med Terraform-roten,
   inte src-kod. `src/shared/certs/` skulle felaktigt antyda att .NET-projekten
   delar koden. Behåll.
6. **Dockerfile-COPY syntax + layer-cache** — Syntax korrekt. Layer-cache-impact:
   COPY ligger som första instruktion i runtime-stage (rad 50 Api, rad 43 Worker)
   — det betyder att `COPY --from=build /app/publish .` (som ändras vid varje
   kod-ändring) ligger efter cert-COPY, så cert-laget cachas över alla builds där
   bara koden ändras. Optimalt. Bundle 165KB → tre-fyra MB image-tillägg över
   tid är icke-mätbart.
7. **Edge case: bundle saknas → fail-loud** — Korrekt val att inte guarda.
   Saknad bundle = security-regression som **måste** stoppa build. Inget att
   ändra. Blocker B1 ovan är just denna fail-loud i verkligheten — bara att
   `.dockerignore` triggar den oavsiktligt.

## Föreslagna in-block-fixar

1. **Lägg till `.dockerignore`-undantag** (Blocker B1) — krävs för att A4 ska
   bygga överhuvudtaget.
2. **CS-format-test** (Major M1) — om scope tillåter, annars öppna TD-39 i samma
   commit som A4.
3. **Storleks-fix i STEG-rapport** (Minor m2) — kosmetiskt.

Övrigt: A4-implementationen är genomtänkt och välkommenterad. Säkerhets-resonemanget
är synligt i kod (inline-kommentarer + Dockerfile-kommentarer), vilket är exakt rätt
för en posture-ändring som inte kan testas trivialt. Efter B1-fix: approve.
