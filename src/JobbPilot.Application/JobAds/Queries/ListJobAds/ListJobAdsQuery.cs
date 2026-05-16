using JobbPilot.Application.Common;
using JobbPilot.Domain.JobAds;
using Mediator;

namespace JobbPilot.Application.JobAds.Queries.ListJobAds;

// ADR 0042 Beslut B — Ssyk/Region single→multi (IReadOnlyList). Nullable
// behålls för "ej angivet" (handler översätter null → tom lista innan
// JobAdSearch.ApplyCriteria). Page/PageSize/SortBy/Q oförändrade.
public sealed record ListJobAdsQuery(
    int Page = 1,
    int PageSize = 20,
    JobAdSortBy SortBy = JobAdSortBy.PublishedAtDesc,
    IReadOnlyList<string>? Ssyk = null,
    IReadOnlyList<string>? Region = null,
    string? Q = null) : IQuery<PagedResult<JobAdDto>>;
