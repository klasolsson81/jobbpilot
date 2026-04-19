using JobbPilot.Domain.Common;

namespace JobbPilot.Domain.JobAds.Events;

public sealed record JobAdArchivedDomainEvent(
    JobAdId JobAdId,
    DateTimeOffset OccurredAt) : IDomainEvent;
