using JobbPilot.Domain.JobAds;

namespace JobbPilot.Application.SavedSearches.Queries;

// ADR 0042 Beslut B — Ssyk/Region single→multi (IReadOnlyList; aldrig null
// från VO:t — tom lista = inget filter).
public sealed record SavedSearchDto(
    Guid Id,
    string Name,
    IReadOnlyList<string> Ssyk,
    IReadOnlyList<string> Region,
    string? Q,
    JobAdSortBy SortBy,
    bool NotificationEnabled,
    DateTimeOffset? LastRunAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
