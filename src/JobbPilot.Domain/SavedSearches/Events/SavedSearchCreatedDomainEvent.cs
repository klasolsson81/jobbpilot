using JobbPilot.Domain.Common;
using JobbPilot.Domain.JobSeekers;

namespace JobbPilot.Domain.SavedSearches.Events;

public sealed record SavedSearchCreatedDomainEvent(
    SavedSearchId SavedSearchId,
    JobSeekerId JobSeekerId,
    string Name,
    DateTimeOffset OccurredAt) : IDomainEvent;
