using JobbPilot.Domain.Common;

namespace JobbPilot.Domain.JobAds.Events;

public sealed record JobAdCreatedDomainEvent(
    JobAdId JobAdId,
    string Title,
    DateTimeOffset OccurredAt) : IDomainEvent;
