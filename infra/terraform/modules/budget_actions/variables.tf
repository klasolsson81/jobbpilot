variable "name_prefix" {
  description = "Prefix för resurs-namn (t.ex. \"jobbpilot-dev\")."
  type        = string
}

variable "budget_name" {
  description = "Namn på AWS Budget som Budget Action ska binda mot. Lookas upp via data-source i samma konto. Måste matcha `aws_budgets_budget.monthly.name` i prod/baseline-stack (default \"jobbpilot-monthly\")."
  type        = string
}

variable "target_role_name" {
  description = "IAM-rollnamn som JobbPilotBedrockDeny attachas på vid budget-threshold-breach. Typiskt api-task-role (`<name_prefix>-ecs-task-api`). OBS: name, inte ARN — AWS Budget Action `iam_action_definition.roles` tar role-NAMN."
  type        = string
}

variable "kms_key_id" {
  description = "KMS-nyckel för SNS-topic-encryption (in-transit + at-rest). Återanvänd master-key per BUILD.md §14."
  type        = string
}

variable "alert_email" {
  description = "Email-adress för SNS-subscription av cost-anomaly-events. Tom sträng = ingen subscription skapas (Klas konfigurerar manuellt via console eller separat tf-changeset). Subscription-confirmation kräver mottagar-opt-in via AWS-mail."
  type        = string
  default     = ""
}

variable "action_threshold_percentage" {
  description = "Threshold-procent av budget där Budget Action triggar APPLY_IAM_POLICY. Default 100 per ADR 0005-amendment (\"vid 100% av tröskeln auto-disable\"). Inte konfigurerbart från call-site i normalfall — variabeln finns för testbarhet."
  type        = number
  default     = 100
}

variable "tags" {
  description = "Common tags."
  type        = map(string)
  default     = {}
}
