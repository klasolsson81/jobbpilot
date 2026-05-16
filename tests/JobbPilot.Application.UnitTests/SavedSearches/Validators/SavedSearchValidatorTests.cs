using JobbPilot.Application.SavedSearches.Commands.CreateSavedSearch;
using JobbPilot.Application.SavedSearches.Commands.UpdateSavedSearch;
using JobbPilot.Application.SavedSearches.Queries.RunSavedSearch;
using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.SavedSearches;
using Shouldly;

namespace JobbPilot.Application.UnitTests.SavedSearches.Validators;

public class CreateSavedSearchCommandValidatorTests
{
    private readonly CreateSavedSearchCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _validator.Validate(new CreateSavedSearchCommand(
            "Mitt sök", "12345", null, null, JobAdSortBy.PublishedAtDesc, false));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_IsInvalid(string name)
    {
        var result = _validator.Validate(new CreateSavedSearchCommand(
            name, "12345", null, null, JobAdSortBy.PublishedAtDesc, false));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithNameTooLong_IsInvalid()
    {
        var result = _validator.Validate(new CreateSavedSearchCommand(
            new string('x', SavedSearch.NameMaxLength + 1), "12345", null, null,
            JobAdSortBy.PublishedAtDesc, false));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithUndefinedSortBy_IsInvalid()
    {
        var result = _validator.Validate(new CreateSavedSearchCommand(
            "Mitt sök", "12345", null, null, (JobAdSortBy)999, false));
        result.IsValid.ShouldBeFalse();
    }
}

public class UpdateSavedSearchCommandValidatorTests
{
    private readonly UpdateSavedSearchCommandValidator _validator = new();

    [Fact]
    public void Validate_WithSingleFieldChange_IsValid()
    {
        var result = _validator.Validate(new UpdateSavedSearchCommand(
            Guid.NewGuid(), "Nytt namn", null, null));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithNoFieldsChanged_IsInvalid()
    {
        // PATCH med inga fält = vilseledande no-op (skulle skriva audit-rad).
        var result = _validator.Validate(new UpdateSavedSearchCommand(
            Guid.NewGuid(), null, null, null));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithEmptyId_IsInvalid()
    {
        var result = _validator.Validate(new UpdateSavedSearchCommand(
            Guid.Empty, "Namn", null, null));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithEmptyNameWhenProvided_IsInvalid()
    {
        var result = _validator.Validate(new UpdateSavedSearchCommand(
            Guid.NewGuid(), "", null, null));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_WithUndefinedSortByInCriteria_IsInvalid()
    {
        var result = _validator.Validate(new UpdateSavedSearchCommand(
            Guid.NewGuid(), null, null,
            new SavedSearchCriteriaInput("12345", null, null, (JobAdSortBy)999)));
        result.IsValid.ShouldBeFalse();
    }
}

public class RunSavedSearchQueryValidatorTests
{
    private readonly RunSavedSearchQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaults_IsValid()
    {
        var result = _validator.Validate(new RunSavedSearchQuery(Guid.NewGuid()));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_IsInvalid()
    {
        var result = _validator.Validate(new RunSavedSearchQuery(Guid.Empty));
        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageBelowOne_IsInvalid(int page)
    {
        var result = _validator.Validate(new RunSavedSearchQuery(Guid.NewGuid(), Page: page));
        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutsideRange_IsInvalid(int pageSize)
    {
        var result = _validator.Validate(
            new RunSavedSearchQuery(Guid.NewGuid(), PageSize: pageSize));
        result.IsValid.ShouldBeFalse();
    }
}
