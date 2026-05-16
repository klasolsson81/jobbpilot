using JobbPilot.Domain.JobAds;

namespace JobbPilot.Application.SavedSearches.Queries;

public sealed record SavedSearchDto(
    Guid Id,
    string Name,
    string? Ssyk,
    string? Region,
    string? Q,
    JobAdSortBy SortBy,
    bool NotificationEnabled,
    DateTimeOffset? LastRunAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
