output "cost_anomaly_topic_arn" {
  description = "ARN för cost-anomaly SNS-topic. Återanvänds vid framtida cost-related-events (Cost Anomaly Detection, etc.). Refereras från F2-P4-runbook."
  value       = aws_sns_topic.cost_anomaly.arn
}

output "bedrock_deny_policy_arn" {
  description = "ARN för JobbPilotBedrockDeny IAM-policy. Manuell rollback efter incident: `aws iam detach-role-policy --role-name <api-task-role> --policy-arn <denna>`. Refereras från F2-P4-runbook."
  value       = aws_iam_policy.bedrock_deny.arn
}

output "budget_action_id" {
  description = "ID för Budget Action APPLY_IAM_POLICY-resursen. Användbar för CLI-inspektering: `aws budgets describe-budget-action --account-id <id> --budget-name <name> --action-id <denna>`."
  value       = aws_budgets_budget_action.attach_bedrock_deny.action_id
}

output "budget_action_execution_role_arn" {
  description = "ARN för budget-action-execution-rollen. Visas i AWS Budget-konsolen under \"Action\". Användbar för audit/troubleshoot."
  value       = aws_iam_role.budget_action.arn
}
