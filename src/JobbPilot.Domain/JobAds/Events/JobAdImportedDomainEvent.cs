using JobbPilot.Domain.Common;

namespace JobbPilot.Domain.JobAds.Events;

public sealed record JobAdImportedDomainEvent(
    JobAdId JobAdId,
    string Source,
    string ExternalId,
    string Title,
    DateTimeOffset OccurredAt) : IDomainEvent;
