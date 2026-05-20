using JobbPilot.Application.JobAds.Queries.GetTaxonomyTree;
using JobbPilot.Domain.JobAds;

namespace JobbPilot.Application.RecentJobSearches.Queries;

/// <summary>
/// ADR 0060 — read-projection för /sokningar-listan + Senaste-hero-chip.
/// Server-side label-resolve via <see cref="ITaxonomyReadModel"/> (paritet
/// med SavedSearchDto). <see cref="Label"/> server-härleds från Q eller
/// första label för UI-affordance utan FE-logik. <see cref="CurrentCount"/>
/// är live-räknat per row (cap=20 håller N+1 under kontroll, CTO Variant A).
/// <see cref="NewCount"/> = <c>max(0, CurrentCount - LastSeenCount)</c>.
/// </summary>
public sealed record RecentJobSearchDto(
    Guid Id,
    string? Q,
    IReadOnlyList<string> SsykList,
    IReadOnlyList<string> RegionList,
    IReadOnlyList<TaxonomyLabelDto> SsykLabels,
    IReadOnlyList<TaxonomyLabelDto> RegionLabels,
    JobAdSortBy SortBy,
    string Label,
    int CurrentCount,
    int NewCount,
    DateTimeOffset LastViewedAt);
