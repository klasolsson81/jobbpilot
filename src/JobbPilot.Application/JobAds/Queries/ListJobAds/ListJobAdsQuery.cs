using JobbPilot.Application.Common;
using JobbPilot.Application.RecentJobSearches.Common;
using JobbPilot.Domain.JobAds;
using Mediator;

namespace JobbPilot.Application.JobAds.Queries.ListJobAds;

// ADR 0042 Beslut B — Ssyk/Region single→multi (IReadOnlyList). Nullable
// behålls för "ej angivet" (handler översätter null → tom lista innan
// JobAdSearch.ApplyCriteria). Page/PageSize/SortBy/Q oförändrade.
// ADR 0060 — ICapturesRecentSearch markerar queryn för auto-capture-behavior
// (record-properties matchar interface-shape automatiskt).
public sealed record ListJobAdsQuery(
    int Page = 1,
    int PageSize = 20,
    JobAdSortBy SortBy = JobAdSortBy.PublishedAtDesc,
    IReadOnlyList<string>? Ssyk = null,
    IReadOnlyList<string>? Region = null,
    string? Q = null,
    // ADR 0042 Beslut E — "Ny sedan"-fönster (runtime-kontext, ej i
    // SearchCriteria; analog Page/PageSize). Driver JobAdDto.IsNew.
    DateTimeOffset? Since = null) : IQuery<PagedResult<JobAdDto>>, ICapturesRecentSearch;
