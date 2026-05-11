# Security-audit: Fas 1 Block A4 — TD-38 TLS-hardening

**Status:** APPROVED (med apply-fas-checklist + en Sec-Minor follow-up till tech-debt)
**Granskat:** 2026-05-11
**Auktoritet:** GDPR Art. 32 (säkerhet vid behandling — data i transit), CLAUDE.md §5.4, ADR 0026 (TLS/HTTPS-policy om relevant), TD-38

---

## Sammanfattning

A4 stänger TD-38 korrekt. Implementationen löser MITM-ytan inom VPC genom att
splitta connection-string-konstruktion i två semantiskt distinkta hjälpare:
bootstrap (Migrate själv → `Trust=true`) och persisterade tjänster (Api/Worker
via Secrets Manager → `VerifyFull` + Root Certificate). RDS global CA-bundle
copy:as in i Api/Worker-containrar på Linux standard-path. Skiftet från
"encrypted but not authenticated" till "encrypted + authenticated" TLS är
defense-in-depth för data-i-transit och förbättrar Art. 32-postur väsentligt
inför staging/prod.

Inga GDPR-blockers. Inga Sec-Major. Implementationen är mergeklar för
dev-apply. En Sec-Minor follow-up identifierad (bundle-rotations-bevakning,
ej blocker).

---

## Fynd

### Critical
Inga.

### Sec-Major
Inga.

### Sec-Minor

**S-Minor-1: Saknas operativ bevakning av RDS CA-bundle-rotation.**
AWS publicerar bundle:n på `https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem`
och roterar vid behov (historiskt rds-ca-2015 → 2019 → 2024). Nuvarande
bundle (`eu-north-1` G1) gäller till 2061 (RSA2048) respektive 2121
(ECC384/RSA4096) — inget akut. Men: om AWS introducerar `G2`/`rds-ca-2029-bundle`
eller om RDS-instansen rotateras till nyare CA innan bundle uppdateras →
`VerifyFull` fail → Api/Worker tappar DB-anslutning hårdvärt. Saknas:
scheduled-task eller CI-job som hashar bundle:n mot AWS-källan kvartalsvis.

**Föreslagen åtgärd:** Logga som TD-45 i `docs/tech-debt.md` —
månadlig/kvartals-cron som diffar `infra/certs/rds-global-bundle.pem` mot
upstream-URL och larmar vid skillnad. Inte block för A4.

**S-Minor-2: Migrate behåller `Trust=true` — acceptabelt men dokumentera
explicit i ADR-spår.**
Argumentet (short-lived, VPC-only, ECS-SG-ingress, ingen persistens) håller
säkerhetsmässigt. MITM inom dev-VPC kräver att en angripare redan har laterala
rörelser i private subnet — då är trust-cert minst av problemen.
**Acceptabelt.** Men: när vi går till staging/prod bör vi omvärdera. Föreslå:
lägg in `Trust=true` i Migrate som accepted risk i ADR-spår eller utvidga
TD-38-stängningskommentar med "staging/prod-eskalering: ge även Migrate
bundle:n om dotnet/runtime-image som baseimage; samma `COPY`-pattern".

### Nit

**N-1: Container-path-konvention.** `/etc/ssl/certs/rds-global-bundle.pem` är
OK och kolliderar inte med Debian/Ubuntu system-hash-länkar (de använder
`<hash>.0`-symlinks i samma katalog, inte vår filnamn). Alternativ:
`/usr/local/share/ca-certificates/rds-global-bundle.crt` +
`update-ca-certificates` skulle integrera bundle:n i system trust store, men
nuvarande approach (explicit `Root Certificate=`-pek från CS) är mer
förutsägbar — Npgsql tittar exakt där vi säger. Behåll.

**N-2: Bundle committed i repo (165 KB, untracked → snart tracked).**
Säkerhetsmässigt nollrisk: publik AWS-fil, ingen secret, ingen PII.
Reproducerbarhet > "snyggt" här. Behåll.

---

## Per fråga från beställningen

1. **VerifyFull vs VerifyCA:** Korrekt val. `VerifyFull` validerar (a) CA-signature
   och (b) hostname-match mot `Host=`-parametern. `VerifyCA` skulle bara
   validera (a) → en angripare med ett legitimt RDS-cert för *annan* instans
   i samma region kunde MITM:a. För Postgres-driver:n via Npgsql ger detta
   full TLS-validering motsvarande webbläsare. **Approved.**

2. **Bundle-strategi (global vs region-specific):** Global bundle är rätt här.
   Portability dev (`eu-north-1`) → ev. multi-region-disaster-recovery i
   framtiden behövs inte container-rebuild. Storleksoverhead 165 KB är
   försumbart i en .NET-runtime-image (~200 MB). Region-specific skulle
   spara <1‰ av image-storleken till priset av fragmentering. **Approved.**

3. **CA-rotation:** Bundle täcker eu-north-1 fram till 2061/2121. Inga
   G2-certs i nuvarande bundle (AWS har inte annonserat G2). Risken är att
   AWS introducerar G2 *innan* nuvarande G1 expirar och roterar instans-certs
   till G2 → då behöver vi uppdaterad bundle. Bevakning saknas → S-Minor-1.

4. **Migrate behåller Trust=true:** Acceptabelt för dev. Se S-Minor-2.

5. **Bundle committed:** OK — publik fil, ingen risk. Inte git-ignorerat
   (korrekt).

6. **Container-path:** OK, ingen kollision. Se N-1.

7. **Re-deploy-flow:** Se Apply-fas-checklist nedan.

8. **GDPR/ADR-koppling:** Ingen PII-flow ändras. Strikt TLS-validering är
   defense-in-depth för Art. 32. **Ingen ny ADR krävs** — TD-38-stängningskommentar
   i `tech-debt.md` med hänvisning till denna review räcker.

---

## Apply-fas-checklist (säkerhets-checks innan dev-apply)

1. **Verifiera bundle-integritet före apply:** Jämför SHA256 av
   `infra/certs/rds-global-bundle.pem` mot upstream
   `https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem`. En
   diff = avbryt.
2. **Image-build-verifiering:** Efter Api/Worker-image-build, kör
   `docker run --rm <image> ls -la /etc/ssl/certs/rds-global-bundle.pem` och
   bekräfta storlek ~165 KB.
3. **Smoke-test före Secrets-skrivning:** Kör Migrate-task med
   `--dry-run`-motsvarighet (eller tail loggar) — verifiera att Phase A/B/C
   går igenom med `ForMigrate`. Detta bevisar att RDS faktiskt presenterar
   cert som global-bundle kan validera.
4. **Secrets-rotation-verifiering:** Efter `PutSecretValue` × 2, hämta secrets
   manuellt (`aws secretsmanager get-secret-value`) och granska att CS
   innehåller `SSL Mode=VerifyFull;Root Certificate=/etc/ssl/certs/rds-global-bundle.pem`
   — inte fortfarande `Trust Server Certificate=true`. Block-kriterium: om
   en CS innehåller `Trust=true` → rollback.
5. **Api/Worker force-redeploy:** Trigga ECS force-new-deployment. Bevaka
   CloudWatch-loggar för Npgsql-handshake-errors (e.g. `remote certificate
   is invalid`, `hostname mismatch`). Inga sådana = approved.
6. **Connection-rollback-plan:** Om Api/Worker inte ansluter post-deploy:
   revert till föregående task-def (förra image-tag har Trust=true via gamla
   Secrets), debugga offline. Plan dokumenterad innan apply.
7. **Audit-log-spår:** Migrate-task-körningen ska producera entry i
   CloudWatch + (om det finns) `audit-log` om secret-mutation. Verifiera att
   händelsen "TLS-policy escalated dev/<datum>" är spårbar.
8. **TD-38-stängning:** Efter grön apply → markera TD-38 stängd i
   `docs/tech-debt.md` med review-link och commit-SHA. Logga S-Minor-1 som
   TD-45.

---

## Approve-status

**Approved för dev-apply** under förutsättning att Apply-fas-checklist
(punkterna 1–7) körs och S-Minor-1 loggas som TD-45.

**Pre-staging-revisit:** Inför staging-promotion (Fas 1 → Fas 2 eller
motsvarande) — omvärdera S-Minor-2 (Migrate Trust=true). Då bör Migrate
också få bundle:n eller åtminstone ADR-dokumenteras som accepted risk för
staging/prod.

**GDPR-status:** Förbättrar Art. 32-postur. Ingen ny PII-flow, ingen ny
sub-processor, ingen ny region. Defense-in-depth för data-i-transit.
