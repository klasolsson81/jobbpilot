using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.SavedSearches.Queries;
using Mediator;

namespace JobbPilot.Application.SavedSearches.Queries.GetSavedSearch;

public sealed record GetSavedSearchQuery(Guid Id)
    : IQuery<SavedSearchDto?>, IAuthenticatedRequest;
