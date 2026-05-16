using JobbPilot.Domain.Common;
using JobbPilot.Domain.JobSeekers;

namespace JobbPilot.Domain.SavedSearches.Events;

public sealed record SavedSearchDeletedDomainEvent(
    SavedSearchId SavedSearchId,
    JobSeekerId JobSeekerId,
    DateTimeOffset OccurredAt) : IDomainEvent;
