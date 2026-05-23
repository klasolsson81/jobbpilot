using JobbPilot.Application.Common.Abstractions;
using Mediator;

namespace JobbPilot.Application.SavedJobAds.Queries.ListSavedJobAds;

public sealed record ListSavedJobAdsQuery
    : IQuery<IReadOnlyList<SavedJobAdDto>>, IAuthenticatedRequest;
