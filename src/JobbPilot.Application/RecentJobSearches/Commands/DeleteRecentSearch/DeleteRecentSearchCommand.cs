using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.Common.Auditing;
using JobbPilot.Domain.Common;
using Mediator;

namespace JobbPilot.Application.RecentJobSearches.Commands.DeleteRecentSearch;

public sealed record DeleteRecentSearchCommand(Guid Id)
    : ICommand<Result>, IAuthenticatedRequest, IAuditableCommand<Result>
{
    public string EventType => "RecentJobSearch.Deleted";
    public string AggregateType => "RecentJobSearch";
    public Guid ExtractAggregateId(Result response) => Id;
}
