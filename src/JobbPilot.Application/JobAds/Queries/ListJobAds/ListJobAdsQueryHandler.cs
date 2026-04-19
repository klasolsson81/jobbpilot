using JobbPilot.Application.Common.Abstractions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace JobbPilot.Application.JobAds.Queries.ListJobAds;

public sealed class ListJobAdsQueryHandler(IAppDbContext db)
    : IQueryHandler<ListJobAdsQuery, IReadOnlyList<JobAdDto>>
{
    public async ValueTask<IReadOnlyList<JobAdDto>> Handle(
        ListJobAdsQuery query, CancellationToken cancellationToken)
    {
        return await db.JobAds
            .AsNoTracking()
            .OrderByDescending(j => j.PublishedAt)
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
    }
}
