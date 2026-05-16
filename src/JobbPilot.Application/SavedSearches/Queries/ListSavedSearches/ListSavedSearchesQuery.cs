using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.SavedSearches.Queries;
using Mediator;

namespace JobbPilot.Application.SavedSearches.Queries.ListSavedSearches;

/// <summary>
/// "Mina sparade sökningar". JobSeeker-scoped. Ej paginerad — en användare
/// har i praktiken få sparade sökningar (en handfull spår); paginering vore
/// över-engineering här (KISS, mätt mot domänvolymen).
/// </summary>
public sealed record ListSavedSearchesQuery
    : IQuery<IReadOnlyList<SavedSearchDto>>, IAuthenticatedRequest;
