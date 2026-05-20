using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.RecentJobSearches;
using JobbPilot.Domain.SavedSearches;
using Shouldly;

namespace JobbPilot.Domain.UnitTests.RecentJobSearches;

// FilterHashCalculator — deterministic SHA-256 över canonical-JSON av filter-shape
// (q, ssyk-sorted, region-sorted, sortBy). Bär uniqueness-kontraktet på persistens-
// ytan UNIQUE(job_seeker_id, filter_hash) (ADR 0060).
public class FilterHashCalculatorTests
{
    private static SearchCriteria Criteria(
        IEnumerable<string>? ssyk = null,
        IEnumerable<string>? region = null,
        string? q = "backend",
        JobAdSortBy sortBy = JobAdSortBy.PublishedAtDesc) =>
        SearchCriteria.Create(ssyk ?? ["12345"], region ?? ["stockholm"], q, sortBy).Value;

    [Fact]
    public void Compute_ReturnsLowerCaseHex64Chars()
    {
        var hash = FilterHashCalculator.Compute(Criteria());

        hash.ShouldNotBeNull();
        hash.Length.ShouldBe(64);
        hash.ShouldMatch("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Compute_IsDeterministic_SameInputProducesSameHash()
    {
        var a = FilterHashCalculator.Compute(Criteria());
        var b = FilterHashCalculator.Compute(Criteria());

        a.ShouldBe(b);
    }

    [Fact]
    public void Compute_DifferentQ_ProducesDifferentHash()
    {
        var a = FilterHashCalculator.Compute(Criteria(q: "backend"));
        var b = FilterHashCalculator.Compute(Criteria(q: "frontend"));

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Compute_DifferentSortBy_ProducesDifferentHash()
    {
        var a = FilterHashCalculator.Compute(Criteria(sortBy: JobAdSortBy.PublishedAtDesc));
        var b = FilterHashCalculator.Compute(Criteria(sortBy: JobAdSortBy.PublishedAtAsc));

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Compute_DifferentSsyk_ProducesDifferentHash()
    {
        var a = FilterHashCalculator.Compute(Criteria(ssyk: ["12345"]));
        var b = FilterHashCalculator.Compute(Criteria(ssyk: ["99999"]));

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Compute_NullQ_ProducesDifferentHashThanEmptyQ()
    {
        // Q=null vs Q="backend" → olika hash (null = inget filter)
        var withQ = FilterHashCalculator.Compute(Criteria(q: "backend"));
        var withoutQ = FilterHashCalculator.Compute(Criteria(q: null));

        withQ.ShouldNotBe(withoutQ);
    }

    [Fact]
    public void Compute_UnsortedSsykInput_ProducesSameHashAsSorted()
    {
        // SearchCriteria.NormalizeList sorterar ordinalt — två logiskt lika
        // kriterie-uppsättningar med olika input-ordning ska producera SAMMA hash.
        var a = FilterHashCalculator.Compute(Criteria(ssyk: ["zzz", "aaa", "mmm"]));
        var b = FilterHashCalculator.Compute(Criteria(ssyk: ["aaa", "mmm", "zzz"]));

        a.ShouldBe(b);
    }

    [Fact]
    public void Compute_DuplicateSsykInput_ProducesSameHashAsDeduplicated()
    {
        var a = FilterHashCalculator.Compute(Criteria(ssyk: ["12345", "12345", "67890"]));
        var b = FilterHashCalculator.Compute(Criteria(ssyk: ["12345", "67890"]));

        a.ShouldBe(b);
    }

    [Fact]
    public void Compute_DifferentRegion_ProducesDifferentHash()
    {
        var a = FilterHashCalculator.Compute(Criteria(region: ["stockholm"]));
        var b = FilterHashCalculator.Compute(Criteria(region: ["goteborg"]));

        a.ShouldNotBe(b);
    }

    [Fact]
    public void Compute_NullCriteria_Throws()
    {
        Should.Throw<ArgumentNullException>(() => FilterHashCalculator.Compute(null!));
    }
}
