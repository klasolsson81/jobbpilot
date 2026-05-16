using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.Common.Auditing;
using JobbPilot.Domain.Common;
using JobbPilot.Domain.JobAds;
using Mediator;

namespace JobbPilot.Application.SavedSearches.Commands.UpdateSavedSearch;

/// <summary>
/// PATCH /api/v1/saved-searches/{id}. Partiell uppdatering: endast medskickade
/// fält ändras. <see cref="Name"/> → Rename, <see cref="NotificationEnabled"/>
/// → SetNotification (ADR 0039 Beslut 4 — lagras, ingen dispatch i Fas 2),
/// <see cref="Criteria"/> (icke-null) → UpdateCriteria (hela kriteriet ersätts;
/// SearchCriteria-VO:t kan inte vara partiellt giltigt).
/// </summary>
public sealed record UpdateSavedSearchCommand(
    Guid Id,
    string? Name,
    bool? NotificationEnabled,
    SavedSearchCriteriaInput? Criteria)
    : ICommand<Result>, IAuthenticatedRequest, IAuditableCommand<Result>
{
    public string EventType => "SavedSearch.Updated";
    public string AggregateType => "SavedSearch";
    public Guid ExtractAggregateId(Result response) => Id;
}

public sealed record SavedSearchCriteriaInput(
    string? Ssyk,
    string? Region,
    string? Q,
    JobAdSortBy SortBy);
