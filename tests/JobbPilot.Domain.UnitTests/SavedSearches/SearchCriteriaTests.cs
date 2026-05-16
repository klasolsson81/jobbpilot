using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.SavedSearches;
using Shouldly;

namespace JobbPilot.Domain.UnitTests.SavedSearches;

// VO-invarianter speglar ListJobAdsQueryValidator (ADR 0039 Beslut 3) så en
// sparad sökning aldrig kan vara mer tillåtande än motsvarande live-sökning.
public class SearchCriteriaTests
{
    // ---------------------------------------------------------------
    // Happy path + minst-ett-kriterium
    // ---------------------------------------------------------------

    [Fact]
    public void Create_WithSsykOnly_ReturnsSuccess()
    {
        var result = SearchCriteria.Create("12345", null, null, JobAdSortBy.PublishedAtDesc);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Ssyk.ShouldBe("12345");
        result.Value.Region.ShouldBeNull();
        result.Value.Q.ShouldBeNull();
        result.Value.SortBy.ShouldBe(JobAdSortBy.PublishedAtDesc);
    }

    [Fact]
    public void Create_WithRegionOnly_ReturnsSuccess()
    {
        var result = SearchCriteria.Create(null, "stockholm_AB", null, JobAdSortBy.PublishedAtDesc);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Region.ShouldBe("stockholm_AB");
    }

    [Fact]
    public void Create_WithQOnly_ReturnsSuccess()
    {
        var result = SearchCriteria.Create(null, null, "backend", JobAdSortBy.PublishedAtDesc);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Q.ShouldBe("backend");
    }

    [Fact]
    public void Create_WithAllCriteria_ReturnsSuccess()
    {
        var result = SearchCriteria.Create("12345", "stockholm", "backend", JobAdSortBy.ExpiresAtAsc);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Ssyk.ShouldBe("12345");
        result.Value.Region.ShouldBe("stockholm");
        result.Value.Q.ShouldBe("backend");
        result.Value.SortBy.ShouldBe(JobAdSortBy.ExpiresAtAsc);
    }

    // ---------------------------------------------------------------
    // Tom-validering — alla null/whitespace
    // ---------------------------------------------------------------

    [Fact]
    public void Create_WithAllNull_ReturnsFailure()
    {
        var result = SearchCriteria.Create(null, null, null, JobAdSortBy.PublishedAtDesc);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SearchCriteria.Empty");
    }

    [Theory]
    [InlineData("", "", "")]
    [InlineData("   ", "  ", " ")]
    [InlineData(null, "  ", null)]
    public void Create_WithOnlyWhitespaceCriteria_ReturnsEmptyFailure(
        string? ssyk, string? region, string? q)
    {
        var result = SearchCriteria.Create(ssyk, region, q, JobAdSortBy.PublishedAtDesc);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SearchCriteria.Empty");
    }

    // ---------------------------------------------------------------
    // Ssyk-format (concept-id-regex ^[A-Za-z0-9_-]{1,32}$)
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("has space")]
    [InlineData("åäö")]
    [InlineData("semi;colon")]
    [InlineData("123456789012345678901234567890123")] // 33 tecken > 32
    public void Create_WithInvalidSsyk_ReturnsFailure(string ssyk)
    {
        var result = SearchCriteria.Create(ssyk, null, null, JobAdSortBy.PublishedAtDesc);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SearchCriteria.InvalidSsyk");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ABC-123_xyz")]
    [InlineData("12345678901234567890123456789012")] // exakt 32 tecken
    public void Create_WithValidSsykFormat_ReturnsSuccess(string ssyk)
    {
        var result = SearchCriteria.Create(ssyk, null, null, JobAdSortBy.PublishedAtDesc);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Ssyk.ShouldBe(ssyk);
    }

    // ---------------------------------------------------------------
    // Region-format (samma concept-id-regex)
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("region space")]
    [InlineData("åäö")]
    [InlineData("123456789012345678901234567890123")] // 33 tecken
    public void Create_WithInvalidRegion_ReturnsFailure(string region)
    {
        var result = SearchCriteria.Create(null, region, null, JobAdSortBy.PublishedAtDesc);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SearchCriteria.InvalidRegion");
    }

    // ---------------------------------------------------------------
    // Q-längd (2-100 tecken)
    // ---------------------------------------------------------------

    [Fact]
    public void Create_WithQTooShort_ReturnsFailure()
    {
        var result = SearchCriteria.Create(null, null, "a", JobAdSortBy.PublishedAtDesc);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SearchCriteria.InvalidQ");
    }

    [Fact]
    public void Create_WithQTooLong_ReturnsFailure()
    {
        var result = SearchCriteria.Create(null, null, new string('x', 101), JobAdSortBy.PublishedAtDesc);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SearchCriteria.InvalidQ");
    }

    [Theory]
    [InlineData("ab")]                  // exakt min 2
    [InlineData("backend developer")]
    public void Create_WithQAtBoundaries_ReturnsSuccess(string q)
    {
        var result = SearchCriteria.Create(null, null, q, JobAdSortBy.PublishedAtDesc);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Q.ShouldBe(q);
    }

    [Fact]
    public void Create_WithQAtMaxLength_ReturnsSuccess()
    {
        var q = new string('x', 100); // exakt max 100
        var result = SearchCriteria.Create(null, null, q, JobAdSortBy.PublishedAtDesc);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Q!.Length.ShouldBe(100);
    }

    // ---------------------------------------------------------------
    // SortBy — Enum.IsDefined
    // ---------------------------------------------------------------

    [Fact]
    public void Create_WithUndefinedSortBy_ReturnsFailure()
    {
        var result = SearchCriteria.Create("12345", null, null, (JobAdSortBy)999);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("SearchCriteria.InvalidSortBy");
    }

    // ---------------------------------------------------------------
    // Trim-normalisering
    // ---------------------------------------------------------------

    [Fact]
    public void Create_TrimsAllCriteria()
    {
        var result = SearchCriteria.Create("  12345  ", "  stockholm  ", "  backend  ",
            JobAdSortBy.PublishedAtDesc);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Ssyk.ShouldBe("12345");
        result.Value.Region.ShouldBe("stockholm");
        result.Value.Q.ShouldBe("backend");
    }

    [Fact]
    public void Create_NormalizesWhitespaceFieldsToNull()
    {
        // Region whitespace → null, men ssyk satt → fortfarande giltig
        var result = SearchCriteria.Create("12345", "   ", null, JobAdSortBy.PublishedAtDesc);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Region.ShouldBeNull();
    }

    // ---------------------------------------------------------------
    // Värde-likhet (record → Evans 2003 kap. 5)
    // ---------------------------------------------------------------

    [Fact]
    public void TwoIdenticalCriteria_AreValueEqual()
    {
        var a = SearchCriteria.Create("12345", "stockholm", "backend", JobAdSortBy.PublishedAtDesc).Value;
        var b = SearchCriteria.Create("12345", "stockholm", "backend", JobAdSortBy.PublishedAtDesc).Value;

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void TwoDifferentCriteria_AreNotValueEqual()
    {
        var a = SearchCriteria.Create("12345", null, null, JobAdSortBy.PublishedAtDesc).Value;
        var b = SearchCriteria.Create("99999", null, null, JobAdSortBy.PublishedAtDesc).Value;

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }

    [Fact]
    public void CriteriaDifferingOnlyBySortBy_AreNotValueEqual()
    {
        // SortBy ingår i identiteten (determinerar paginerat resultat).
        var a = SearchCriteria.Create("12345", null, null, JobAdSortBy.PublishedAtDesc).Value;
        var b = SearchCriteria.Create("12345", null, null, JobAdSortBy.PublishedAtAsc).Value;

        a.ShouldNotBe(b);
    }

    [Fact]
    public void TrimNormalizedCriteria_AreValueEqualToUntrimmed()
    {
        // Normalisering gör att "  12345  " och "12345" producerar lika VO:n.
        var a = SearchCriteria.Create("  12345  ", null, null, JobAdSortBy.PublishedAtDesc).Value;
        var b = SearchCriteria.Create("12345", null, null, JobAdSortBy.PublishedAtDesc).Value;

        a.ShouldBe(b);
    }
}
