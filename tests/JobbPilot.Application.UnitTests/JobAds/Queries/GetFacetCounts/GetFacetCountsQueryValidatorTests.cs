using JobbPilot.Application.JobAds.Abstractions;
using JobbPilot.Application.JobAds.Queries.GetFacetCounts;
using JobbPilot.Domain.SavedSearches;
using Shouldly;

namespace JobbPilot.Application.UnitTests.JobAds.Queries.GetFacetCounts;

// Fas E2c — validator-ytan speglar ListJobAdsQueryValidator (defense-in-depth;
// Domain SearchCriteria = sanningskälla för konstanterna). IsInEnum() på
// Dimension är primärskyddet mot numeriska out-of-range-bindningar
// (?dimension=7) — utan den blir Infrastructure-switchens throw ett 500.
public class GetFacetCountsQueryValidatorTests
{
    private readonly GetFacetCountsQueryValidator _validator = new();

    [Fact]
    public void Valid_minimal_query_passes()
    {
        var result = _validator.Validate(
            new GetFacetCountsQuery(FacetDimension.OccupationGroup));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Out_of_range_numeric_dimension_fails()
    {
        var result = _validator.Validate(
            new GetFacetCountsQuery((FacetDimension)7));
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Dimension");
    }

    [Fact]
    public void List_exceeding_MaxConceptIds_fails()
    {
        var tooMany = Enumerable.Range(0, SearchCriteria.MaxConceptIds + 1)
            .Select(i => $"id{i}")
            .ToList();
        var result = _validator.Validate(
            new GetFacetCountsQuery(FacetDimension.Municipality, Region: tooMany));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Invalid_concept_id_format_fails()
    {
        var result = _validator.Validate(
            new GetFacetCountsQuery(
                FacetDimension.Municipality, Municipality: ["bad id!"]));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Q_below_min_length_fails()
    {
        var result = _validator.Validate(
            new GetFacetCountsQuery(FacetDimension.Region, Q: "a"));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Q_above_max_length_fails()
    {
        var result = _validator.Validate(
            new GetFacetCountsQuery(
                FacetDimension.Region, Q: new string('a', SearchCriteria.QMaxLength + 1)));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Q_within_bounds_passes()
    {
        var result = _validator.Validate(
            new GetFacetCountsQuery(FacetDimension.Region, Q: "lärare"));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Max_size_lists_on_all_dimensions_pass()
    {
        var maxList = Enumerable.Range(0, SearchCriteria.MaxConceptIds)
            .Select(i => $"id{i}")
            .ToList();
        var result = _validator.Validate(
            new GetFacetCountsQuery(
                FacetDimension.OccupationGroup,
                OccupationGroup: maxList,
                Municipality: maxList,
                Region: maxList));
        result.IsValid.ShouldBeTrue();
    }
}
