using JobbPilot.Application.JobAds.Queries.ListJobAds;
using JobbPilot.Domain.JobAds;
using Shouldly;

namespace JobbPilot.Application.UnitTests.JobAds.Queries.ListJobAds;

// Batch 3 — ADR 0042 Beslut B: ListJobAdsQuery.Ssyk/.Region string? →
// IReadOnlyList<string>?. Validator-yta: per-element regex via RuleForEach,
// maxantal-cap (10), generaliserad tom-invariant. Speglar SearchCriteria-
// invarianterna (defense-in-depth, samma yta som dagens concept-id-regex).
//
// RÖD tills ListJobAdsQuery + ListJobAdsQueryValidator implementerar list-formen.
public class ListJobAdsQueryValidatorTests
{
    private readonly ListJobAdsQueryValidator _validator = new();

    [Fact]
    public void Validate_WithDefaults_IsValid()
    {
        var result = _validator.Validate(new ListJobAdsQuery());
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageBelowOne_IsInvalid(int page)
    {
        var result = _validator.Validate(new ListJobAdsQuery(Page: page));
        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_PageSizeOutsideRange_IsInvalid(int pageSize)
    {
        var result = _validator.Validate(new ListJobAdsQuery(PageSize: pageSize));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_SortByUnknownEnum_IsInvalid()
    {
        var result = _validator.Validate(new ListJobAdsQuery(SortBy: (JobAdSortBy)999));
        result.IsValid.ShouldBeFalse();
    }

    // ---------------------------------------------------------------
    // Per-element regex ^[A-Za-z0-9_-]{1,32}$ via RuleForEach
    // Single-element-lista ⇒ samma resultat som gammalt single-värde.
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("MVqp_eS8_kDZ")]
    [InlineData("abc-123")]
    [InlineData("Z")]
    [InlineData("0123456789ABCDEFabcdef_-_-_-_-12")] // 32 tecken (max)
    public void Validate_Ssyk_SingleValidConceptId_Passes(string ssyk)
    {
        var result = _validator.Validate(new ListJobAdsQuery(Ssyk: [ssyk]));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Ssyk_MultipleValidConceptIds_Passes()
    {
        var result = _validator.Validate(
            new ListJobAdsQuery(Ssyk: ["MVqp_eS8_kDZ", "abc-123", "Z"]));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("åäö")]
    [InlineData("<script>")]
    [InlineData("dot.notation")]
    [InlineData("plus+sign")]
    [InlineData("0123456789ABCDEFabcdef_-_-_-_-123")] // 33 tecken
    public void Validate_Ssyk_AnyInvalidElement_Fails(string bad)
    {
        // RuleForEach: ett ogiltigt element bland giltiga ⇒ hela query ogiltig.
        var result = _validator.Validate(new ListJobAdsQuery(Ssyk: ["12345", bad]));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Ssyk_Null_Passes()
    {
        var result = _validator.Validate(new ListJobAdsQuery(Ssyk: null));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Ssyk_EmptyList_Passes()
    {
        // Tom lista = inget filter (generaliserad tom-invariant).
        var result = _validator.Validate(new ListJobAdsQuery(Ssyk: []));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("MVqp_eS8_kDZ")]
    [InlineData("abc-123")]
    public void Validate_Region_SingleValidConceptId_Passes(string region)
    {
        var result = _validator.Validate(new ListJobAdsQuery(Region: [region]));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Region_MultipleValidConceptIds_Passes()
    {
        var result = _validator.Validate(
            new ListJobAdsQuery(Region: ["stockholm", "uppsala"]));
        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("åäö")]
    [InlineData("<script>")]
    [InlineData("0123456789ABCDEFabcdef_-_-_-_-123")]
    public void Validate_Region_AnyInvalidElement_Fails(string bad)
    {
        var result = _validator.Validate(new ListJobAdsQuery(Region: ["stockholm", bad]));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Region_Null_Passes()
    {
        var result = _validator.Validate(new ListJobAdsQuery(Region: null));
        result.IsValid.ShouldBeTrue();
    }

    // ---------------------------------------------------------------
    // Maxantal-cap = 10 per lista (speglar SearchCriteria-invariant 2)
    // ---------------------------------------------------------------

    [Fact]
    public void Validate_Ssyk_ExactlyTen_Passes()
    {
        var ten = Enumerable.Range(1, 10).Select(i => $"ssyk{i}").ToArray();
        var result = _validator.Validate(new ListJobAdsQuery(Ssyk: ten));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Ssyk_Eleven_IsInvalid()
    {
        var eleven = Enumerable.Range(1, 11).Select(i => $"ssyk{i}").ToArray();
        var result = _validator.Validate(new ListJobAdsQuery(Ssyk: eleven));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Region_Eleven_IsInvalid()
    {
        var eleven = Enumerable.Range(1, 11).Select(i => $"reg{i}").ToArray();
        var result = _validator.Validate(new ListJobAdsQuery(Region: eleven));
        result.IsValid.ShouldBeFalse();
    }

    // ---------------------------------------------------------------
    // Q oförändrat — 2-100 tecken
    // ---------------------------------------------------------------

    [Fact]
    public void Validate_Q_TooShort_Fails()
    {
        var result = _validator.Validate(new ListJobAdsQuery(Q: "a"));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Q_TooLong_Fails()
    {
        var result = _validator.Validate(new ListJobAdsQuery(Q: new string('x', 101)));
        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("developer")]
    public void Validate_Q_ValidLength_Passes(string q)
    {
        var result = _validator.Validate(new ListJobAdsQuery(Q: q));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Q_AtMaxLength_Passes()
    {
        var result = _validator.Validate(new ListJobAdsQuery(Q: new string('x', 100)));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Q_Null_Passes()
    {
        var result = _validator.Validate(new ListJobAdsQuery(Q: null));
        result.IsValid.ShouldBeTrue();
    }

    // ---------------------------------------------------------------
    // Kombinerat
    // ---------------------------------------------------------------

    [Fact]
    public void Validate_AllFiltersNull_Passes()
    {
        var result = _validator.Validate(new ListJobAdsQuery(
            Page: 1, PageSize: 20, SortBy: JobAdSortBy.PublishedAtDesc,
            Ssyk: null, Region: null, Q: null));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AllFiltersValid_Passes()
    {
        var result = _validator.Validate(new ListJobAdsQuery(
            Page: 1, PageSize: 20, SortBy: JobAdSortBy.PublishedAtDesc,
            Ssyk: ["MVqp_eS8_kDZ", "abc-123"], Region: ["stockholm"], Q: "developer"));
        result.IsValid.ShouldBeTrue();
    }

    // ADR 0042 Beslut D — Relevance kräver Q (speglar SearchCriteria-invarianten).

    [Fact]
    public void Validate_RelevanceSortWithoutQ_IsInvalid()
    {
        var result = _validator.Validate(new ListJobAdsQuery(
            SortBy: JobAdSortBy.Relevance, Ssyk: ["12345"], Q: null));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_RelevanceSortWithQ_IsValid()
    {
        var result = _validator.Validate(new ListJobAdsQuery(
            SortBy: JobAdSortBy.Relevance, Q: "developer"));
        result.IsValid.ShouldBeTrue();
    }
}
