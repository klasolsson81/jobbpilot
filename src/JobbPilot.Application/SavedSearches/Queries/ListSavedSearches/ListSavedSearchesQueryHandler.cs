using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.SavedSearches.Queries;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace JobbPilot.Application.SavedSearches.Queries.ListSavedSearches;

public sealed class ListSavedSearchesQueryHandler(
    IAppDbContext db, ICurrentUser currentUser)
    : IQueryHandler<ListSavedSearchesQuery, IReadOnlyList<SavedSearchDto>>
{
    public async ValueTask<IReadOnlyList<SavedSearchDto>> Handle(
        ListSavedSearchesQuery query, CancellationToken cancellationToken)
    {
        if (!currentUser.UserId.HasValue)
            return [];

        var jobSeekerId = await db.JobSeekers
            .AsNoTracking()
            .Where(js => js.UserId == currentUser.UserId.Value)
            .Select(js => js.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (jobSeekerId == default)
            return [];

        var items = await db.SavedSearches
            .AsNoTracking()
            .Where(s => s.JobSeekerId == jobSeekerId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(s => new SavedSearchDto(
            s.Id.Value,
            s.Name,
            s.Criteria.Ssyk,
            s.Criteria.Region,
            s.Criteria.Q,
            s.Criteria.SortBy,
            s.NotificationEnabled,
            s.LastRunAt,
            s.CreatedAt,
            s.UpdatedAt)).ToList();
    }
}
