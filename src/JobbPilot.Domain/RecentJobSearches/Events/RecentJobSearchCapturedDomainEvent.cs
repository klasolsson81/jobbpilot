using JobbPilot.Domain.Common;
using JobbPilot.Domain.JobSeekers;

namespace JobbPilot.Domain.RecentJobSearches.Events;

public sealed record RecentJobSearchCapturedDomainEvent(
    RecentJobSearchId RecentJobSearchId,
    JobSeekerId JobSeekerId,
    string FilterHash,
    DateTimeOffset OccurredAt) : IDomainEvent;
