using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.Common.Auditing;
using JobbPilot.Domain.Common;
using JobbPilot.Domain.JobAds;
using Mediator;

namespace JobbPilot.Application.SavedSearches.Commands.CreateSavedSearch;

public sealed record CreateSavedSearchCommand(
    string Name,
    string? Ssyk,
    string? Region,
    string? Q,
    JobAdSortBy SortBy,
    bool NotificationEnabled)
    : ICommand<Result<Guid>>, IAuthenticatedRequest, IAuditableCommand<Result<Guid>>
{
    public string EventType => "SavedSearch.Created";
    public string AggregateType => "SavedSearch";
    public Guid ExtractAggregateId(Result<Guid> response) => response.Value;
}
