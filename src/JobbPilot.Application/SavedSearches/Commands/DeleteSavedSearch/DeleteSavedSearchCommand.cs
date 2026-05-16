using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.Common.Auditing;
using JobbPilot.Domain.Common;
using Mediator;

namespace JobbPilot.Application.SavedSearches.Commands.DeleteSavedSearch;

public sealed record DeleteSavedSearchCommand(Guid Id)
    : ICommand<Result>, IAuthenticatedRequest, IAuditableCommand<Result>
{
    public string EventType => "SavedSearch.Deleted";
    public string AggregateType => "SavedSearch";
    public Guid ExtractAggregateId(Result response) => Id;
}
