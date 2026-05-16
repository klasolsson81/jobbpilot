using JobbPilot.Application.Common;
using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.JobAds.Queries;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace JobbPilot.Application.JobAds.Queries.ListJobAds;

public sealed class ListJobAdsQueryHandler(IAppDbContext db)
    : IQueryHandler<ListJobAdsQuery, PagedResult<JobAdDto>>
{
    public async ValueTask<PagedResult<JobAdDto>> Handle(
        ListJobAdsQuery query, CancellationToken cancellationToken)
    {
        // ADR 0039 Beslut 1 — filter/sort-logiken ägs av JobAdSearch (delad
        // med RunSavedSearchQueryHandler). Handlern är en tunn adapter som
        // mappar sitt query-record till den delade komposition.
        var baseQuery = JobAdSearch.ApplyCriteria(
            db.JobAds.AsNoTracking(), query.Ssyk, query.Region, query.Q);

        // Separat count-query per CLAUDE.md §3.6. Filter appliceras före count
        // så totalen reflekterar filtrerad mängd, inte totalt antal annonser.
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var ordered = JobAdSearch.ApplySort(baseQuery, query.SortBy);

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
                j.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<JobAdDto>(items, totalCount, query.Page, query.PageSize);
    }
}
