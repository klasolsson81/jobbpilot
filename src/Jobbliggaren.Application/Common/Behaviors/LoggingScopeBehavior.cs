using Jobbliggaren.Application.Common.Abstractions;
using Jobbliggaren.Application.Common.Auditing;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Jobbliggaren.Application.Common.Behaviors;

/// <summary>
/// TD-104 / Pre-4 STEG 6 — opens a logging scope carrying the structured fields BUILD.md
/// §14.1 mandates on every log record: <c>CorrelationId</c>, <c>UserId</c>,
/// <c>OperationType</c>. Registered OUTERMOST in the Mediator pipeline so the scope wraps
/// every downstream behavior (including <see cref="LoggingBehavior{TMessage,TResponse}"/>)
/// and the handler, plus anything they log (EF, etc.). With the Seq sink (senior-cto-advisor
/// Variant B) these scope properties surface as structured Seq fields; the console provider
/// shows them when <c>IncludeScopes</c> is enabled.
///
/// Mechanism per senior-cto-advisor (2026-06-14, <c>a459197161c622d4c</c>): reuse the
/// existing ports <see cref="ICorrelationIdProvider"/> + <see cref="ICurrentUser"/> (Api
/// HTTP impls + Worker stubs) — no new port, no new abstraction (YAGNI; <c>OperationType</c>
/// = Mediator message name). The CTO illustrated the carrier as an "Api middleware + Worker
/// job-wrapper"; a single shared pipeline behavior is the DRY refinement — it runs
/// identically in both composition roots, and the message name is inherently a Mediator-level
/// concept. Only non-PII identifiers are scoped: never <c>Email</c> or <c>SessionId</c>
/// (security-auditor STEG 6 — no plaintext PII to the persistent sink).
/// </summary>
public sealed class LoggingScopeBehavior<TMessage, TResponse>(
    ICorrelationIdProvider correlationIdProvider,
    ICurrentUser currentUser,
    ILogger<LoggingScopeBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var scope = new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationIdProvider.Current,
            ["UserId"] = currentUser.UserId,
            ["OperationType"] = typeof(TMessage).Name,
        };

        using (logger.BeginScope(scope))
        {
            return await next(message, cancellationToken);
        }
    }
}
