# ---------------------------------------------------------------------------
# Budget Actions — F2-P3 / ADR 0005 second amendment 2026-05-12.
#
# Etablerar kostnadsskydd-mekanism per ADR 0005-amendment Alternativ C (invite-
# only public beta med hård cap). Vid $50/mån-threshold-breach (100% ACTUAL):
#
#   PRIMÄRSKYDD: APPLY_IAM_POLICY → bifoga JobbPilotBedrockDeny-overlay på
#                api-task-role. Explicit Deny vinner över Allow per IAM-
#                evaluation-logik — blockerar all bedrock:Invoke* / Converse*.
#                Reversibel: Budget Action auto-detachar vid budget-recover.
#
#   SEKUNDÄRSKYDD: Manuell ECS scale-down enligt F2-P4-runbook
#                  (`docs/runbooks/aws-cost-recovery.md`). AWS Budget Actions
#                  API stödjer endast STOP_EC2_INSTANCES/STOP_RDS_INSTANCES
#                  för SSM-sub-action — ingen native ECS-stop. ECS Fargate
#                  ~$30/mån fast kostnad är inte skenrisk; Bedrock är enda
#                  realistiska blowout-vektorn (täckt av primärskyddet).
#                  Se ADR 0005 second amendment för full motivering.
#
# Modulen är konto-scope-aware (Budget = AWS-konto-resurs) men lever i
# dev-stacken eftersom target_role_name pekar på dev-ECS-task-role. Hybrid
# placement-pattern A4 per senior-cto-advisor 2026-05-12 (Bounded Contexts —
# Evans 2003): Budget-resursen ägs av baseline (jobbpilot-monthly), Actions
# ägs av miljöstacken som hostar target-resurserna.
# ---------------------------------------------------------------------------

data "aws_caller_identity" "current" {}
data "aws_partition" "current" {}

# Lookup baseline-budgeten via name. Beslut: data-source > hard-coded ARN
# (looser coupling, fail-fast om budget saknas vid apply). Per CTO 2026-05-12
# rond 1: looser coupling tillåter budget-rename i baseline utan att bryta
# dev-stack så länge namnet bevaras.
data "aws_budgets_budget" "target" {
  name = var.budget_name
}

# ---------------------------------------------------------------------------
# SNS topic — cost-anomaly events. Dedikerad topic (D2-beslut), separat från
# secops-anomaly (TD-68/ADR 0031). Cost-events är ops-events, secops-events
# är security-events — olika mottagare, olika åtgärder, olika alarm-fatigue-
# budget. SRP på SNS-topic-nivå.
# ---------------------------------------------------------------------------

resource "aws_sns_topic" "cost_anomaly" {
  name              = "${var.name_prefix}-cost-anomaly"
  kms_master_key_id = var.kms_key_id

  tags = merge(var.tags, {
    Purpose = "cost-anomaly-alerts"
  })
}

# Least-privilege topic-policy: endast Budget Action-service-principal från
# samma konto får publicera. Default SNS-policy tillåter alla principals i
# kontot vilket öppnar för alarm-suppression-attack (defense-in-depth pattern
# från TD-68 secops-anomaly-topic).
resource "aws_sns_topic_policy" "cost_anomaly" {
  arn = aws_sns_topic.cost_anomaly.arn

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "AllowBudgetsToPublish"
        Effect = "Allow"
        Principal = {
          Service = "budgets.amazonaws.com"
        }
        Action   = "SNS:Publish"
        Resource = aws_sns_topic.cost_anomaly.arn
        Condition = {
          StringEquals = {
            "AWS:SourceAccount" = data.aws_caller_identity.current.account_id
          }
        }
      }
    ]
  })
}

# Email-subscription (optional). Tom alert_email = ingen subscription skapas.
# Konfirmeringsmail från AWS måste opt-in:as manuellt oavsett.
resource "aws_sns_topic_subscription" "cost_email" {
  count = var.alert_email != "" ? 1 : 0

  topic_arn = aws_sns_topic.cost_anomaly.arn
  protocol  = "email"
  endpoint  = var.alert_email
}

# ---------------------------------------------------------------------------
# JobbPilotBedrockDeny — explicit deny-overlay för Bedrock-invocation.
#
# OCP-pattern (Martin 2017): JobbPilotBedrockInvoke-policy (i baseline) är
# normal-state Allow. Denna deny-policy läggs OVANPÅ vid budget-breach utan
# att modifiera originalet. Original-policy stängd för modifiering, systemet
# öppet för utökning via overlay.
#
# IAM evaluation-logik: explicit Deny vinner alltid över Allow. Robustare än
# att detacha Allow-policyn (race-condition + Lambda-fel-risk skulle lämna
# systemet öppet).
#
# Resource = "*" är acceptabelt här eftersom semantiken är "deny ALL bedrock-
# invocation oavsett resource". Inverterad least-privilege: vid breach vill
# vi blockera bredt, inte snävt.
# ---------------------------------------------------------------------------

data "aws_iam_policy_document" "bedrock_deny" {
  statement {
    sid    = "DenyAllBedrockInvocation"
    effect = "Deny"

    actions = [
      "bedrock:InvokeModel",
      "bedrock:InvokeModelWithResponseStream",
      "bedrock:Converse",
      "bedrock:ConverseStream",
    ]

    resources = ["*"]
  }
}

resource "aws_iam_policy" "bedrock_deny" {
  name        = "JobbPilotBedrockDeny"
  description = "Deny-overlay för Bedrock-invocation. Attachas av AWS Budget Action vid $50/mån-threshold-breach (ADR 0005-amendment). Reversibel — Budget Action gör auto-detach när budget-cycle resettar."
  policy      = data.aws_iam_policy_document.bedrock_deny.json

  tags = merge(var.tags, {
    Purpose = "cost-controls-bedrock-killswitch"
  })
}

# ---------------------------------------------------------------------------
# Budget Action execution-role — Budget Service assumes denna roll för att
# attacha/detacha JobbPilotBedrockDeny på target-rollen.
#
# Confused-deputy-skydd: aws:SourceAccount-condition säkerställer att bara
# vårt egna konto kan trigga assume. AWS Budget Service kör cross-region men
# inte cross-account — condition är defensiv hygien.
# ---------------------------------------------------------------------------

data "aws_iam_policy_document" "budget_action_assume" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["budgets.amazonaws.com"]
    }

    condition {
      test     = "StringEquals"
      variable = "aws:SourceAccount"
      values   = [data.aws_caller_identity.current.account_id]
    }
  }
}

resource "aws_iam_role" "budget_action" {
  name               = "${var.name_prefix}-budget-action-role"
  description        = "Execution-role for AWS Budget Action - attach/detach JobbPilotBedrockDeny on api-task-role at threshold-breach. AWS IAM description disallows U+2014 em-dash."
  assume_role_policy = data.aws_iam_policy_document.budget_action_assume.json

  tags = merge(var.tags, {
    Purpose = "cost-controls-budget-action"
  })
}

# Least-privilege permissions: AttachRolePolicy + DetachRolePolicy på EXAKT
# target_role_name, MED PolicyARN-condition som låser till JobbPilotBedrockDeny.
# Defense-in-depth: även om budget-action-rollen kompromissas kan den bara
# attach:a/detach:a den specifika deny-policyn, inte godtycklig managed-policy.
data "aws_iam_policy_document" "budget_action_permissions" {
  statement {
    sid    = "AttachOrDetachBedrockDenyOnTargetRole"
    effect = "Allow"

    actions = [
      "iam:AttachRolePolicy",
      "iam:DetachRolePolicy",
    ]

    resources = [
      "arn:${data.aws_partition.current.partition}:iam::${data.aws_caller_identity.current.account_id}:role/${var.target_role_name}"
    ]

    condition {
      test     = "ArnEquals"
      variable = "iam:PolicyARN"
      values   = [aws_iam_policy.bedrock_deny.arn]
    }
  }
}

resource "aws_iam_role_policy" "budget_action" {
  name   = "${var.name_prefix}-budget-action-permissions"
  role   = aws_iam_role.budget_action.id
  policy = data.aws_iam_policy_document.budget_action_permissions.json
}

# ---------------------------------------------------------------------------
# Budget Action — APPLY_IAM_POLICY, AUTOMATIC approval, vid 100% ACTUAL.
#
# AUTOMATIC vald per ADR 0005-amendment "hård cap". 100% threshold är ACTUAL
# (inte FORECASTED) — kostnad MÅSTE redan ha skett innan trigger. Reversibel:
# auto-detach när cycle resettar (eller manuell `aws iam detach-role-policy`).
#
# Subscriber publicerar event till cost-anomaly-topic vid trigger. Klas får
# notifiering antingen via email-subscription eller via topic-subscriber
# tillagd separat.
# ---------------------------------------------------------------------------

resource "aws_budgets_budget_action" "attach_bedrock_deny" {
  budget_name = data.aws_budgets_budget.target.name
  account_id  = data.aws_caller_identity.current.account_id

  action_type        = "APPLY_IAM_POLICY"
  approval_model     = "AUTOMATIC"
  notification_type  = "ACTUAL"
  execution_role_arn = aws_iam_role.budget_action.arn

  action_threshold {
    action_threshold_type  = "PERCENTAGE"
    action_threshold_value = var.action_threshold_percentage
  }

  definition {
    iam_action_definition {
      policy_arn = aws_iam_policy.bedrock_deny.arn
      roles      = [var.target_role_name]
    }
  }

  subscriber {
    address           = aws_sns_topic.cost_anomaly.arn
    subscription_type = "SNS"
  }
}
