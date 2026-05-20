using JobbPilot.Application.Common.Abstractions;
using Mediator;

namespace JobbPilot.Application.RecentJobSearches.Queries.ListRecentSearches;

public sealed record ListRecentSearchesQuery
    : IQuery<IReadOnlyList<RecentJobSearchDto>>, IAuthenticatedRequest;
