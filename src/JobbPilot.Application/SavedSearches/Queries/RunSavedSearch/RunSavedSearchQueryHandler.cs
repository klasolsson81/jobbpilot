using JobbPilot.Application.Common;
using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.Common.Auditing;
using JobbPilot.Application.JobAds.Queries;
using JobbPilot.Domain.SavedSearches;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace JobbPilot.Application.SavedSearches.Queries.RunSavedSearch;

public sealed class RunSavedSearchQueryHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IFailedAccessLogger failedAccessLogger)
    : IQueryHandler<RunSavedSearchQuery, PagedResult<JobAdDto>?>
{
    public async ValueTask<PagedResult<JobAdDto>?> Handle(
        RunSavedSearchQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
            return null;

        var jobSeekerId = await db.JobSeekers
            .AsNoTracking()
            .Where(js => js.UserId == currentUser.UserId.Value)
            .Select(js => js.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (jobSeekerId == default)
            return null;

        var savedSearchId = new SavedSearchId(query.Id);

        var criteria = await db.SavedSearches
            .AsNoTracking()
            .Where(s => s.Id == savedSearchId && s.JobSeekerId == jobSeekerId)
            .Select(s => s.Criteria)
            .FirstOrDefaultAsync(cancellationToken);

        if (criteria is null)
        {
            // Failed-access-detection (ADR 0031): skilj okänt id från cross-tenant.
            var exists = await db.SavedSearches
                .AsNoTracking()
                .AnyAsync(s => s.Id == savedSearchId, cancellationToken);
            if (exists)
            {
                failedAccessLogger.LogCrossUserAttempt(
                    "SavedSearch", savedSearchId.Value, currentUser.UserId.Value,
                    "RunSavedSearch");
            }
            return null;
        }

        // ADR 0039 Beslut 1 — samma JobAdSearch-komposition som ListJobAds.
        // ADR 0039 Beslut 2 — ingen last_run_at-skrivning (query, ej command).
        var baseQuery = JobAdSearch.ApplyCriteria(
            db.JobAds.AsNoTracking(), criteria.Ssyk, criteria.Region, criteria.Q);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var ordered = JobAdSearch.ApplySort(baseQuery, criteria.SortBy, criteria.Q);

        var items = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(j => new JobAdDto(
                j.Id.Value,
                j.Title,
                j.Company.Name,
                j.Description,
                j.Url,
                j.Source.Value,
                j.Status.Value,
                j.PublishedAt,
                j.ExpiresAt,
                j.CreatedAt,
                // ADR 0042 Beslut E — Since är ListJobAdsQuery-runtime-kontext,
                // ej del av SavedSearch; run exponerar därför aldrig IsNew=true.
                false))
            .ToListAsync(cancellationToken);

        return new PagedResult<JobAdDto>(items, totalCount, query.Page, query.PageSize);
    }
}
