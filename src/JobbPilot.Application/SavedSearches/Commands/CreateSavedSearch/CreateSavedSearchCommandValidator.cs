using FluentValidation;
using JobbPilot.Domain.SavedSearches;

namespace JobbPilot.Application.SavedSearches.Commands.CreateSavedSearch;

public sealed class CreateSavedSearchCommandValidator
    : AbstractValidator<CreateSavedSearchCommand>
{
    public CreateSavedSearchCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Namn är obligatoriskt.")
            .MaximumLength(SavedSearch.NameMaxLength)
            .WithMessage($"Namn får vara max {SavedSearch.NameMaxLength} tecken.");

        // Criteria-invarianter (minst ett kriterium, concept-id-format, q-längd)
        // ägs av SearchCriteria.Create (Domain) — handlern returnerar dess
        // DomainError som 400. SortBy valideras här som tidig defense-in-depth.
        RuleFor(c => c.SortBy).IsInEnum();
    }
}
