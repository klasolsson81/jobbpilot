# Arkitektur-analys — TD-38 Fas 1 Block A4 (TLS-hardening)

**Datum:** 2026-05-11
**Granskare:** dotnet-architect
**Scope:** `src/JobbPilot.Migrate/Program.cs`, `src/JobbPilot.Api/Dockerfile`,
`src/JobbPilot.Worker/Dockerfile`, `infra/certs/rds-global-bundle.pem`

## Sammanfattning

**Approve med två mindre förslag.** Implementationen är arkitektur-mässigt
sund: split mellan `BuildMigrateConnectionString` (kortlivad bootstrap) och
`BuildPersistedConnectionString` (långlivade tjänster) är välmotiverad,
kommentarerna förklarar trade-offs explicit, och CA-bundle-placeringen via
Dockerfile-COPY är portabel dev→staging→prod. Inga Clean Arch-brott, inga
anti-patterns från CLAUDE.md §5.1. Keyword-syntax mot Npgsql 9.x verifierad
mot officiell dokumentation.

## Fynd

### Viktigt
Inga.

### Mindre

**[Mindre 1]** `src/JobbPilot.Migrate/Program.cs:185-200`
**Vad:** Två `static` top-level-funktioner i Program.cs. Acceptabelt för
bootstrap-tool, men inte enhets-testbart utan att exekvera hela Program.
**Varför:** CLAUDE.md §2.4 — "Om du inte kan testa det utan att starta
ASP.NET → designen är fel." Migrate är inte ASP.NET, men samma princip:
connection-string-konstruktionen är ren funktion som bör täckas av ett
enhetstest (verifierar att `VerifyFull` + `Root Certificate` finns med, att
`Trust=true` *inte* finns i persisted-varianten).
**Föreslagen åtgärd:** Lyft till `internal static class ConnectionStringFactory`
i samma projekt — minimal refactor, möjliggör test som verifierar regression
mot oavsiktlig downgrade. **Inom scope för A4 (5 raders flytt).**

**[Mindre 2]** Architecture-test saknas
**Vad:** Ingen architecture-test verifierar att `Trust Server Certificate=true`
inte läcker till Api/Worker-projekt.
**Varför:** CLAUDE.md §7 — architecture-tests är del av Definition of Done.
TD-38 är säkerhets-hardening — regression bör fångas mekaniskt, inte via
code-review.
**Föreslagen åtgärd:** **Skjut till separat TD** (scope-creep i A4). Lägg som
`TD-46` i tech-debt.md: NetArchTest-regel som scannar `JobbPilot.Api` +
`JobbPilot.Worker` assemblies för string-constant `"Trust Server Certificate=true"`.
Migrate exkluderas.

## Frågespecifika bedömningar

1. **Keyword-syntax `SSL Mode=VerifyFull` (med space):** Korrekt för
   Npgsql 9.x. Npgsql connection-string-parser är case-insensitive och
   space-tolerant (`SslMode` är ekvivalent alias). Inget att åtgärda.

2. **`Root Certificate`-parameter:** Korrekt kanoniskt namn enligt
   npgsql.org/doc/connection-string-parameters.html. `RootCertificate` (utan
   space) accepteras som alias. `sslrootcert` är libpq-syntax — Npgsql
   accepterar det inte direkt. Behåll `Root Certificate`.

3. **System-truststore vs explicit `Root Certificate`:** Npgsql med
   `VerifyFull` *kan* använda system-truststore om `Root Certificate`
   utelämnas — då används .NET:s default `X509Chain`-validering mot
   `/etc/ssl/certs/ca-certificates.crt`. **Behåll explicit path.** Anledningar:
   (a) AWS RDS global-bundle innehåller AWS-specifika intermediates som inte
   alltid finns i Ubuntu-base, (b) explicit path är auditerbar och
   self-documenting, (c) `update-ca-certificates`-flödet kräver extra
   Dockerfile-steg + .crt-rename (.pem → .crt + symlink). Trade-off-analysen
   är gjord rätt.

4. **Connection-string-split:** Se "Mindre 1" ovan.

5. **Migrate behåller `Trust=true`:** Arkitektur-mässigt acceptabelt.
   Trade-off-kommentaren i Program.cs:185-189 är explicit och korrekt:
   kortlivad task, container saknar bundle, MITM-yta minimal inom VPC
   SG-isolation. Att ge Migrate egen bundle = 3 extra Dockerfile-rader +
   konsekvent strikt-policy — **överväg som TD-38c** men inte blocker.

6. **Architecture-test:** Se "Mindre 2" ovan.

7. **EF Core / Identity DbContext:** Båda tar connection-string-värdet direkt
   från Secrets Manager via DI/`IOptions<DatabaseOptions>` (Fas 0-pattern,
   ej ändrat). Ingen filtrering — `Root Certificate`-parametern passerar
   oförändrad till Npgsql-providern. Inget att åtgärda; detta är önskvärt
   beteende.

## Approve-status

**APPROVE.** Båda mindre fynden är icke-blockerande. Förslag på in-block-fix:
lyft `ConnectionStringFactory` (10 min). Architecture-test → TD-46. Migrate
`VerifyFull` → eventuellt TD vid staging-promotion.

## Referenser

- CLAUDE.md §2.4 — Testbart först
- CLAUDE.md §5.1 — Anti-patterns (inga brott)
- CLAUDE.md §7 — Testing-krav (architecture-tests)
- Npgsql 9.x connection-string-parameters: https://www.npgsql.org/doc/connection-string-parameters.html (verifierad 2026-05-11)
- AWS RDS global CA-bundle: https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem
