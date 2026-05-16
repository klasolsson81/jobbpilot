using JobbPilot.Domain.Common;

namespace JobbPilot.Domain.SavedSearches.Events;

public sealed record SavedSearchRenamedDomainEvent(
    SavedSearchId SavedSearchId,
    string Name,
    DateTimeOffset OccurredAt) : IDomainEvent;
