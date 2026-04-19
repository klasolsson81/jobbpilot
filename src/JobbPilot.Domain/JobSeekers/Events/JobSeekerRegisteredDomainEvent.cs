using JobbPilot.Domain.Common;

namespace JobbPilot.Domain.JobSeekers.Events;

public sealed record JobSeekerRegisteredDomainEvent(
    JobSeekerId JobSeekerId,
    Guid UserId,
    string DisplayName,
    DateTimeOffset OccurredAt) : IDomainEvent;
