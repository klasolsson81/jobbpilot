# Runbook — AWS Cost Recovery (F2-P3 / F2-P4)

**Status:** STUB — full utbyggnad i F2-P4-batch. Denna stub etablerar ankare
för ADR 0005 second amendment (2026-05-12) som refererar manuell
ECS-stop-procedur som sekundärskydd.

**Senast uppdaterad:** 2026-05-12 (F2-P3-leverans, stub-version)

---

## Syfte

Procedur när AWS Budget Action triggat på $50/mån-tröskeln eller när manuell
incident-response krävs vid skenande kostnader.

Per ADR 0005 second amendment:

- **Primärskydd (automatisk):** APPLY_IAM_POLICY Budget Action bifogar
  `JobbPilotBedrockDeny` på `jobbpilot-dev-ecs-task-api` vid 100% ACTUAL.
  Blockerar omedelbart all Bedrock-invocation.
- **Sekundärskydd (manuell):** ECS scale-down enligt steg nedan.

---

## När triggar primärskyddet

Budget Action publicerar event till `jobbpilot-dev-cost-anomaly` SNS-topic
vid 100% ACTUAL av $50/mån-budgeten. Om Klas opt-in:at email-subscription:
inkommande AWS-mail med rubrik "AWS Budgets Action - Policy Applied".

**Verifiering:**

```powershell
# Lista attachade policies på api-task-rollen
aws iam list-attached-role-policies `
  --role-name jobbpilot-dev-ecs-task-api `
  --profile jobbpilot

# Förväntat efter trigger: bland AttachedPolicies syns
# "JobbPilotBedrockDeny" med ARN arn:aws:iam::710427215829:policy/JobbPilotBedrockDeny
```

---

## Manuell ECS scale-down (sekundärskydd)

Om Bedrock-disable inte räcker (osannolikt — Bedrock är enda blowout-vektorn)
eller om Klas vill stoppa ALL compute manuellt:

```powershell
# Stoppa Api-service
aws ecs update-service `
  --cluster jobbpilot-dev `
  --service jobbpilot-dev-api `
  --desired-count 0 `
  --profile jobbpilot

# Stoppa Worker-service
aws ecs update-service `
  --cluster jobbpilot-dev `
  --service jobbpilot-dev-worker `
  --desired-count 0 `
  --profile jobbpilot
```

**Verifiering:**

```powershell
aws ecs describe-services `
  --cluster jobbpilot-dev `
  --services jobbpilot-dev-api jobbpilot-dev-worker `
  --profile jobbpilot `
  --query 'services[*].[serviceName,runningCount,desiredCount]'
```

Förväntat: desiredCount=0, runningCount sjunker mot 0 inom ~60s.

---

## Återställning efter incident

**Steg 1 — Identifiera kostnads-driver:**

```powershell
# Senaste 7 dagarnas kostnad per service
aws ce get-cost-and-usage `
  --time-period Start=$((Get-Date).AddDays(-7).ToString('yyyy-MM-dd')),End=$(Get-Date -Format 'yyyy-MM-dd') `
  --granularity DAILY `
  --metrics UnblendedCost `
  --group-by Type=DIMENSION,Key=SERVICE `
  --profile jobbpilot
```

**Steg 2 — Detacha JobbPilotBedrockDeny (om budget-cycle inte resettat):**

```powershell
aws iam detach-role-policy `
  --role-name jobbpilot-dev-ecs-task-api `
  --policy-arn arn:aws:iam::710427215829:policy/JobbPilotBedrockDeny `
  --profile jobbpilot
```

**Steg 3 — Skala upp ECS-services:**

```powershell
aws ecs update-service `
  --cluster jobbpilot-dev `
  --service jobbpilot-dev-api `
  --desired-count 1 `
  --profile jobbpilot

aws ecs update-service `
  --cluster jobbpilot-dev `
  --service jobbpilot-dev-worker `
  --desired-count 1 `
  --profile jobbpilot
```

**Steg 4 — Smoke-test:**

```powershell
curl -s https://dev.jobbpilot.se/api/ready
# Förväntat: 200 OK med JSON-status
```

---

## TODO (F2-P4-batch — utbyggnad)

- [ ] Detaljerad incident-response-checklista (vem kontaktar vem, eskalering)
- [ ] CloudWatch Insights-queries för cost-per-feature-attribution
- [ ] Decision-tree: när är Bedrock-disable nog vs full ECS-stop
- [ ] Post-mortem-template för budget-blowout-incidents
- [ ] Återställnings-script (PowerShell + bash) checked-in
- [ ] Test-procedur för att verifiera primärskyddet utan att brännas $50

## Källor

- ADR 0005 amendment 2026-05-12 + second amendment 2026-05-12
- `infra/terraform/modules/budget_actions/` — APPLY_IAM_POLICY-mekanism
- AWS Budget Actions API: stödjer inte custom SSM-documents eller Lambda
  för Fargate-services (verifierat 2026-05-12 via AWS CLI v2.29.1 docs)
