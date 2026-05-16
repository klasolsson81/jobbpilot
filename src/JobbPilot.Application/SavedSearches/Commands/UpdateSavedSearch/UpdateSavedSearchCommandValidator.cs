using FluentValidation;
using JobbPilot.Domain.SavedSearches;

namespace JobbPilot.Application.SavedSearches.Commands.UpdateSavedSearch;

public sealed class UpdateSavedSearchCommandValidator
    : AbstractValidator<UpdateSavedSearchCommand>
{
    // Speglar SearchCriteria.ConceptIdPattern + ListJobAdsQueryValidator
    // (defense-in-depth). Domän-faktorn är sanningskällan.
    private const string ConceptIdPattern = @"^[A-Za-z0-9_-]{1,32}$";

    public UpdateSavedSearchCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();

        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Namn får inte vara tomt.")
            .MaximumLength(SavedSearch.NameMaxLength)
            .WithMessage($"Namn får vara max {SavedSearch.NameMaxLength} tecken.")
            .When(c => c.Name is not null);

        // Minst ett fält måste ändras — annars är PATCH:en en no-op som ändå
        // skulle skriva audit-rad (vilseledande granskningstrail).
        RuleFor(c => c)
            .Must(c => c.Name is not null || c.NotificationEnabled is not null || c.Criteria is not null)
            .WithMessage("Minst ett fält (namn, notisflagga eller kriterier) måste anges.");

        // Criteria-invarianter ägs av SearchCriteria.Create (Domain). SortBy +
        // maxantal-cap/per-element-regex valideras här som tidig
        // defense-in-depth när kriterier medskickas (paritet med
        // ListJobAdsQueryValidator; security-auditor M1 2026-05-16, §9.6
        // in-block-fix). Cap refererar Domain-konstanten (single source).
        When(c => c.Criteria is not null, () =>
        {
            RuleFor(c => c.Criteria!.SortBy).IsInEnum();

            RuleFor(c => c.Criteria!.Ssyk!)
                .Must(l => l.Count <= SearchCriteria.MaxConceptIds)
                .When(c => c.Criteria!.Ssyk is not null)
                .WithMessage($"Max {SearchCriteria.MaxConceptIds} yrkesområden per sökning.");

            RuleForEach(c => c.Criteria!.Ssyk)
                .Matches(ConceptIdPattern)
                .When(c => c.Criteria!.Ssyk is not null)
                .WithMessage("Ssyk måste vara en giltig JobTech concept-id (1-32 tecken, alfanumeriskt + _-).");

            RuleFor(c => c.Criteria!.Region!)
                .Must(l => l.Count <= SearchCriteria.MaxConceptIds)
                .When(c => c.Criteria!.Region is not null)
                .WithMessage($"Max {SearchCriteria.MaxConceptIds} regioner per sökning.");

            RuleForEach(c => c.Criteria!.Region)
                .Matches(ConceptIdPattern)
                .When(c => c.Criteria!.Region is not null)
                .WithMessage("Region måste vara en giltig JobTech location-concept-id (1-32 tecken, alfanumeriskt + _-).");
        });
    }
}
