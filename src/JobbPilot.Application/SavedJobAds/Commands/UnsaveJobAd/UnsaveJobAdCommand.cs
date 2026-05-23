using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.Common.Auditing;
using JobbPilot.Domain.Common;
using Mediator;

namespace JobbPilot.Application.SavedJobAds.Commands.UnsaveJobAd;

/// <summary>
/// F6 P5 Punkt 2 Del A — ta bort bokmärke för aktuell JobSeeker.
/// Idempotent: redan-borttagen → Success utan domain-event.
/// </summary>
public sealed record UnsaveJobAdCommand(Guid JobAdId)
    : ICommand<Result>, IAuthenticatedRequest, IAuditableCommand<Result>
{
    public string EventType => "SavedJobAd.Unsaved";
    public string AggregateType => "SavedJobAd";
    public Guid ExtractAggregateId(Result response) => JobAdId;
}
