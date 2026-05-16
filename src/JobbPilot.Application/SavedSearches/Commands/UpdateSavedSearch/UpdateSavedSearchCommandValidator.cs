using FluentValidation;
using JobbPilot.Domain.SavedSearches;

namespace JobbPilot.Application.SavedSearches.Commands.UpdateSavedSearch;

public sealed class UpdateSavedSearchCommandValidator
    : AbstractValidator<UpdateSavedSearchCommand>
{
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

        // Criteria-invarianter ägs av SearchCriteria.Create (Domain). SortBy
        // valideras här som tidig defense-in-depth när kriterier medskickas.
        When(c => c.Criteria is not null, () =>
        {
            RuleFor(c => c.Criteria!.SortBy).IsInEnum();
        });
    }
}
