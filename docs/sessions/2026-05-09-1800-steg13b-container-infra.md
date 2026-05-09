---
session: STEG 13b
datum: 2026-05-09
slug: steg13b-container-infra
status: KLAR (kod-skriven, ej applied)
commits:
  - (TBD - 3-split: feat(infra) + fix(api) + docs)
---

# STEG 13b — Container-infra: ECR + IAM + CloudWatch + ALB + ECS Fargate

## Mål

Komplettera STEG 13a (network + RDS + Redis) med container-infra så Api + Worker kan deployeras till ECS Fargate. Per A4-sekvens (A1 STEG 12 → A2 STEG 13a+b → A3 STEG 14). Inkluderar Dockerfiles + IAM-roller + ALB + CloudWatch LogGroups.

## Scope-uppdelning

Bundlat 13b efter Klas:s "Go alt X bundla allt i 13b". 5 nya Terraform-moduler + 2 Dockerfiles + Api-kod-edit + ADR + 3 TDs + 4 agent-reviews. Större review-yta men logiskt sammanhållet.

Edge + DNS + HTTPS (Route53 + ACM + HTTPS-flip) lyfts till **STEG 13c** kopplad till TD-30/ADR 0026-trigger 1.

## Block 1 — `modules/ecr/`

2 separata repos (`jobbpilot-dev-api`, `jobbpilot-dev-worker`):
- `image_tag_mutability = "MUTABLE"` i lean dev (latest återanvänds); IMMUTABLE i staging/prod
- `scan_on_push = true` (ECR vulnerability-scanning gratis)
- Lifecycle policy: behåll senaste 10 images
- KMS-encryption via master-key

## Block 2 — `modules/cloudwatch_logs/`

3 LogGroups med `retention_in_days = 30` + KMS (per ADR 0024 D7):
- `/aws/ecs/jobbpilot-dev/api` — Api-task-output
- `/aws/ecs/jobbpilot-dev/worker` — Worker-task-output
- `/aws/ecs/jobbpilot-dev/ecs-exec` — `aws ecs execute-command`-sessions

Explicit deklaration hindrar AWS-default `Never expire` som bryter ADR 0024 D7.

## Block 3 — `modules/iam_ecs/`

Tre IAM-roller med strikt least-privilege:

**Execution-role** (delas av alla tasks — startar containrarna):
- ECR pull (mot specifika repos + `GetAuthorizationToken *`)
- CloudWatch put (mot specifika log-groups + streams)
- Secrets Manager get-value (mot dev-secrets för injection)
- KMS Decrypt med `kms:ViaService = secretsmanager.eu-north-1.amazonaws.com`

**Task-role-api** (Api-runtime):
- Bedrock InvokeModel via attach av baseline `JobbPilotBedrockInvoke`-policy (data-lookup, ej cross-stack remote_state)
- Secrets Manager runtime-read
- KMS Decrypt med `kms:ViaService` för secretsmanager/rds/elasticache
- ECS Exec messaging (`ssmmessages:*` på `*` — region-globalt API)

**Task-role-worker** (Worker-runtime):
- Samma som task-role-api **MINUS Bedrock** (ADR 0023 + Fas 1-disciplin — Worker har inga AI-jobb i Fas 1; Bedrock lyfts vid Fas 4 via separat task-role)

Defense-in-depth: `aws:SourceAccount`-condition på AssumeRole-policy förhindrar confused-deputy-attacker.

## Block 4 — `modules/alb/`

Internet-facing ALB i public subnets:
- `drop_invalid_header_fields = true` (HTTP smuggling-skydd)
- `idle_timeout = 60` (höjs vid streaming/long-polling)
- Target-group-api: port 8080, target_type=ip (Fargate awsvpc), health-check `/api/ready` (30s interval, 2 healthy / 3 unhealthy thresholds)
- HTTP-listener default forward → target-group-api
- HTTPS-listener gated på `var.https_listener_enabled` + ACM-cert + TLS 1.2/1.3 SSL-policy
- `lifecycle.precondition` på `acm_certificate_arn` när https_listener_enabled (M2-fix från code-reviewer — fail-fast vid plan-tid)

## Block 5 — `modules/ecs/`

Fargate cluster + 2 task-defs + 2 services:
- Cluster med Container Insights + FARGATE + FARGATE_SPOT capacity providers (default SPOT i dev = ~70% rabatt)
- Task-def-api: 0.5 vCPU + 1 GB, port 8080, secrets-injection, klartext-env (Alb__HttpsEnabled, ForwardedHeaders__KnownNetworks__0=10.0.0.0/16), inga Docker HEALTHCHECK (Fargate ignorerar dem ändå)
- Task-def-worker: 0.25 vCPU + 0.5 GB, HTTP-fri (ADR 0023), bara Postgres + HangfireStorage-secrets (ingen Redis), stopTimeout=30s
- Service-api: ALB-target-group-attached, deployment_circuit_breaker + rollback, ECS Exec aktivt
- Service-worker: ingen ALB-koppling
- Auto-scaling gated på `var.enable_autoscaling` (default false i dev, true i staging/prod)

## Block 6 — Dockerfiles

**`src/JobbPilot.Api/Dockerfile`:** multi-stage .NET 10 (sdk → aspnet-runtime). Non-root `USER app` (uid 1654, default i .NET 8+). EXPOSE 8080.
- **Borttaget i fix-runda:** `RUN apt-get install curl` + `HEALTHCHECK`-direktiv (Fargate ignorerar Docker HEALTHCHECK; ALB target-group är auktoritativ; mindre attack-yta + image-storlek)

**`src/JobbPilot.Worker/Dockerfile`:** multi-stage. Använder `aspnet:10.0` runtime-image tills TD-19 stänger Hangfire.AspNetCore-dependency (kommentar förtydligad efter dotnet-architect-feedback). Inga ports, ingen HEALTHCHECK.

**`.dockerignore`:** exkluderar bin/obj/tests/web/.env/secrets/.git/docs/infra/.claude.

## Block 7 — Api/Program.cs-edits

**`/api/ready`-endpoint** vid sidan av befintliga `/health` (BUILD.md §15.4-spec):
```csharp
app.MapGet("/api/ready", () => Results.Ok(new { status = "ready", service = "JobbPilot.Api" }));
```
Med TODO TD-29 ovanför som flaggar att detta är liveness, inte strict readiness.

**`UseHttpsRedirection` env-gate** (Sec-Major-2-fix från security-auditor):
```csharp
var albOptions = builder.Configuration.GetSection(AlbOptions.SectionName).Get<AlbOptions>() ?? new AlbOptions();
if (builder.Environment.IsDevelopment() || albOptions.HttpsEnabled)
{
    app.UseHttpsRedirection();
}
```
Bakom HTTP-only-ALB skulle `app.UseHttpsRedirection()` triggat 307→443 (stängd port) → ALB-health-check fail → ECS deployment_circuit_breaker rollback.

**`AlbOptions`-record** i `src/JobbPilot.Api/Configuration/AlbOptions.cs` (M1-fix från code-reviewer): symmetri med ForwardedHeadersConfig + RateLimitingOptions + HangfireWorkerOptions. Magic string borttagen.

## Säkerhets-fynd från review-rundan

Full rapport: `docs/reviews/2026-05-09-steg13b-security.md` (2 Major + 9 Minor + 6 Nit).

### ADR-accepterad — Sec-Major-1

**ALB HTTP-only utan ADR-beslut.** Bearer-tokens (Redis-baserad opaque session-id, ADR 0017) i klartext över internet → GDPR Art. 32-implikation.

Fix: **ADR 0026** (`docs/decisions/0026-alb-http-only-fas0.md`) accepterar HTTP-only under Fas 0 med:
- 30 dagars tidsfönster (deadline **2026-06-08**)
- 5 triggers: domän+ACM, multi-tenant 24h, tidsgräns, säkerhetsincident, Fas 2
- Dokumenterad mitigation-stack (rate-limiting + IP-anonymisering + audit-cascade + CloudTrail + restriktiv egress + DNS-disciplin)
- TD-30 lagd för operativa supersession-steg

### Fixad in-block — Sec-Major-2

**`UseHttpsRedirection()` × HTTP-only-ALB** = ALB target-group fail → deployment-rollback. Fix: env-gate via `AlbOptions.HttpsEnabled` som matchar `var.alb_https_enabled` i Terraform. Single source of truth: när Klas flippar Terraform-variabeln händer både ALB-listener-konvertering och redirect-middleware-aktivering atomiskt.

### Kritisk fix (dotnet-architect)

**Redis ConnectionString-mismatch.** Initial impl injicerade `Redis_Host` + `Redis_Port` + `Redis_AuthToken` som 3 separata env-vars/secrets. `Infrastructure/DependencyInjection.cs:90+118+131` läser `GetConnectionString("Redis")` som single string → `ConnectionMultiplexer.Connect()`-format → appen hade kraschat vid första prod-uppstart med null-string.

Fix: komponera StackExchange.Redis-CS i Terraform till single secret:
```hcl
secret_string = format("%s:%d,password=%s,ssl=True,abortConnect=False",
  primary_endpoint_address, port, auth_token)
```
Behåller också raw `auth_token`-secret för debug/rotation (dual-secret-strategi).

**Worker-konfig minimering** (verifierat via discovery): Worker använder INTE Redis — `AddIdentityAndSessions` är HTTP-only. Tog bort alla Redis-secrets/env-vars från `worker_secrets`/`worker_environment`. Mindre secret-yta = mindre läckage-yta.

### Code-reviewer M1 + M2 fixade in-block

- **M1:** Magic string `"Alb:HttpsEnabled"` ersatt med `AlbOptions.SectionName` + record (symmetri med STEG 12-pattern)
- **M2:** ALB-modul `lifecycle.precondition` på `acm_certificate_arn` — fail-fast vid plan-tid om operatör sätter `https_listener_enabled = true` utan cert-ARN

### Defererade till TDs

- **TD-29:** Strict readiness-probe vid Fas 2 (separera liveness från readiness via `AddHealthChecks().AddDbContextCheck<AppDbContext>().AddRedis(...)`)
- **TD-30:** Domänköp + Route53 + ACM-cert (kopplad till ADR 0026-trigger 1, deadline 2026-06-08)
- **TD-31:** Test för UseHttpsRedirection env-gate (anti-regression-skydd)

## Beslut

- **Bundlat 13b över split** (Klas:s val) — logiskt sammanhållet trots stor review-yta
- **HTTP-only initialt + ADR 0026** (Klas:s val Alt B mot Alt A "registrera domän nu") — pragmatisk Fas 0 med tidsgräns
- **`AlbOptions`-record** följer JobbPilot-konvention för config-bundlade options
- **Single composed Redis-CS** över split Host/Port/AuthToken — matchar StackExchange.Redis-format + befintlig `GetConnectionString("Redis")`-pattern
- **Worker-Redis-borttagning** — Worker använder inte Redis, secret-yta minimerad
- **`lifecycle.precondition` över `validation`-block** — Terraform-`validation` kan inte cross-reference andra variables; precondition på resource-resource fungerar
- **FARGATE_SPOT default i dev** — ~70% rabatt, AWS kan ta tillbaka tasks vid kapacitet-brist (acceptabel för dev)
- **Inga Docker HEALTHCHECK-direktiv** — Fargate respekterar dem inte; ECS task-def + ALB target-group är auktoritativa
- **Bedrock-policy via data-lookup** över cross-stack remote_state — undviker state-permission-koppling

## Quirks/spec-drift upptäckt

- **`UseHttpsRedirection()` är destruktivt bakom HTTP-only-ALB** — fail-loud vid första apply hade troligen upptäckts via deployment_circuit_breaker, men miljöer utan auto-rollback hade fastnat. Konfig-driven gate är defensiv.
- **Fargate ignorerar Docker `HEALTHCHECK`-direktiv** — verifierat i AWS-doc; container-level health-check i image är meningslöst för Fargate-tasks.
- **Bedrock har ingen managed prefix-list för cross-region-egress** — verifierat under ADR 0026-skrivning. Påverkar både ADR 0025 (egress-mitigation) och ADR 0026 (mitigation-stack).
- **ASP.NET Configuration läser env-vars med `__` som section-separator** — `Alb__HttpsEnabled` i ECS task-def → `Alb:HttpsEnabled` i `IConfiguration`. Verifierat-pattern.

## Inte applied

`terraform apply` kräver:
1. SSO-login (`aws sso login --profile jobbpilot`) om token utgånget
2. `terraform init` (om nya providers)
3. **Targeted apply på foundation först:** `terraform apply -target=module.ecr -target=module.cloudwatch_logs -target=module.iam_ecs -target=module.network -target=module.rds -target=module.redis`
4. **Manuell `docker build` + `docker push`** till ECR (annars `image_pull_failure` när ECS startar tasks). Detaljerad procedur i `environments/dev/README.md`.
5. **Full apply:** `terraform apply` (skapar ALB + ECS + auto-scaling)
6. Smoke-test: `curl http://<alb-dns>/api/ready` → `{"status":"ready"}`
7. Verifiera ECS service-status + CloudWatch logs

Allt operativt — Klas kör manuellt. STEG 14 automatiserar via GitHub Actions (`v*-dev`-tag → build → push → ECS service-update).

## Commits

| SHA | Beskrivning |
|-----|-------------|
| (TBD) | feat(infra): STEG 13b — container-infra (ECR + IAM + CloudWatch + ALB + ECS) + Dockerfiles |
| (TBD) | fix(api): STEG 13b — UseHttpsRedirection env-gate + /api/ready endpoint + AlbOptions |
| (TBD) | docs: STEG 13b — ADR 0026 + TD-29/30/31 + 4 agent-reviews + docs-sync |

## Tester totalt

- **Backend:** 537 (oförändrade — ingen Domain/Application/Architecture-kod rörd; Api/Program.cs-edits inte täckta av befintliga tester men `dotnet build` exit 0)
- **Frontend:** 65 Vitest + 19 Playwright E2E (oförändrade)

## Reviews

| Rapport | Status |
|---|---|
| `docs/reviews/2026-05-09-steg13b-security.md` (initial) | Approved with Major-findings (Sec-Major-1 + 2) |
| `docs/reviews/2026-05-09-13b-fixes-security-auditor.md` (verifying fixes) | **Sec-Major-1 + 2 stängda — STEG 13b approved för commit** |
| `docs/reviews/2026-05-09-13b-fixes-dotnet-architect.md` | OK på alla fixar; två nice-to-haves (kosmetiska) |
| `docs/reviews/2026-05-09-13b-fixes-adr-keeper.md` | ADR 0026 godkänd; disambiguation-not lagd efter Klas:s GO |
| `docs/reviews/2026-05-09-13b-fixes-code-reviewer.md` | Approved; M1+M2 fixade in-block; M3 → TD-31; M4 = commit-split (3-split godkänd av Klas) |

## Lärdomar STEG 13b

- **Konfig-mismatch mellan IaC och .NET-konsumtion är klassisk fotgropfälla.** Initial impl injicerade Redis Host/Port/AuthToken som 3 separata env-vars; Infrastructure-koden läste single string. Hade kraschat vid första prod-uppstart. dotnet-architect parallellt med security-auditor fångade det. Lärdom: vid IaC-konsumtion av .NET-config — verifiera mot existerande config-läsning innan plan-design.
- **Fargate ignorerar Docker `HEALTHCHECK`-direktiv** — bara EC2-launch-type respekterar. Container-level HEALTHCHECK i image är meningslöst för Fargate-tasks. ECS task-def `healthCheck`-block ELLER ALB target-group-check är auktoritativa.
- **`UseHttpsRedirection()` × HTTP-only-ALB är fail-loud-falla** — redirect 307→443 mot stängd port → ALB target-group failer → ECS deployment_circuit_breaker rollback. Konfig-driven gate via env-var som flippas synkront med ALB-listener-config = atomisk konsistens.
- **AWS-managed prefix-list saknas för Bedrock-egress** — cross-region-trafik från eu-north-1 → eu-central-1/eu-west-1 går via NAT eftersom Bedrock-tjänsten inte har region-prefix-list. Validerat i ADR 0025 + ADR 0026 hardening-väg.
- **`lifecycle.precondition` är fail-fast vid plan-tid** (Terraform 1.x+) — kraftfullt mot operatör-misstag som "https_listener_enabled=true men acm_certificate_arn=null". Failar i plan istället för i apply.
- **`data "aws_iam_policy"`-lookup mellan stackar** > `data "terraform_remote_state"`. Inga tfstate-permissions att hantera, ingen koppling till baseline-stackens version.
- **`aws:SourceAccount`-condition + `kms:ViaService`-restriction** är defense-in-depth-pattern värt att replikera för alla cross-service IAM-roller.
- **Worker-konfig-disciplin (ADR 0023 HTTP-fri)** ska reflekteras i task-def-konfig: ingen ALB, inga ports, inga HTTP-relaterade env-vars (DOTNET_ENVIRONMENT istället för ASPNETCORE_), Bedrock-attach reserveras för Fas 4.
- **Multi-stage Dockerfile-cache-disciplin** är värt komplexiteten — kopiera csproj först (cache restore), sen källkod (cache miss bara vid kod-ändring). `--no-restore` i publish-step undviker dubbel-restore.

## Nästa session

**Två alternativa nästa-steg:**

**Alt 1 — Operativ apply** (rekommenderat efter 13b):
1. `aws sso login --profile jobbpilot`
2. `terraform init` i `environments/dev/`
3. Targeted apply: foundation först (ecr + cloudwatch_logs + iam_ecs + network + rds + redis)
4. `docker build` + `docker push` till båda ECR-repos
5. Full apply (ALB + ECS startar)
6. Smoke-test `/api/ready` via ALB-default-DNS
7. Cost från och med apply: ~$79/mån (RDS + Redis + NAT + ALB + ECS + ECR + CW)

**Alt 2 — STEG 13c parallellt** (Route53 + ACM + HTTPS-flip):
- TD-30/ADR 0026-trigger 1 hard deadline 2026-06-08
- Domän-registrering + Route53 + ACM + flippa `var.alb_https_enabled = true`
- Skriv supersession-ADR 0027
- ~30 min kod + DNS-propagering ~30 min + ACM-validering ~15 min

Min rek: **Alt 1 först** så vi får apply-feedback. STEG 13c kan göras närsomhelst före 2026-06-08.

## Open follow-ups

**Operativa AWS-uppgifter:**
- Apply STEG 13b
- ECR `docker push` (manual initialt; STEG 14 automatiserar via GitHub Actions)
- Hangfire schema-DDL via Install.sql + REVOKE PUBLIC i RDS (STEG 14, `hangfire-schema.md §3-4`)
- ConnectionStrings split (jobbpilot_app + jobbpilot_worker) — `aws secretsmanager put-secret-value` på dev-secrets-placeholders post-DDL (STEG 14)
- Bootstrap-IAM-user cleanup — STEG 14 sista steg per `aws-setup.md §3.4`

**Defererade Sec-Minor-fynd från STEG 13b (icke-blockerande):**
- Sec-Minor-1: aws:RequestedRegion-condition på SSM Messages-permission
- Sec-Minor-3: postcondition på Bedrock-policy-data-lookup för silent-drift-skydd
- Sec-Minor-5: Redis CloudWatch-export aktivering (STEG 13c)
- Sec-Minor-7: ALB security headers (HSTS etc) — vid HTTPS-flip i STEG 13c
- Sec-Nit-1: Worker `Postgres` + `HangfireStorage`-rationale dokumenterad i kod

**Kvarvarande TDs:**
- TD-13/14/15/18/19/20/23/24/25/26/27/28 (oförändrade)
- TD-29 (strict readiness — Fas 2)
- TD-30 (domänköp — deadline 2026-06-08)
- TD-31 (UseHttpsRedirection-test — opportunistic)
