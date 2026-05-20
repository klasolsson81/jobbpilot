using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.JobSeekers;
using JobbPilot.Domain.RecentJobSearches;
using JobbPilot.Domain.RecentJobSearches.Events;
using JobbPilot.Domain.SavedSearches;
using JobbPilot.Domain.UnitTests.JobAds;
using Shouldly;

namespace JobbPilot.Domain.UnitTests.RecentJobSearches;

// AR — Capture/Bump skyddar invarianter (CLAUDE.md §2.2). Auto-capture-domän
// (ADR 0060), skild från SavedSearch (manuell-spara). Identitet via FilterHash
// per JobSeeker; Bump muterar bara LastViewedAt + LastSeenCount.
public class RecentJobSearchTests
{
    private static readonly FakeDateTimeProvider Clock = FakeDateTimeProvider.Default;
    private static readonly JobSeekerId ValidJobSeekerId = new(Guid.NewGuid());

    private static SearchCriteria ValidCriteria() =>
        SearchCriteria.Create(["12345"], ["stockholm"], "backend", JobAdSortBy.PublishedAtDesc).Value;

    private static SearchCriteria AlternateCriteria() =>
        SearchCriteria.Create(["67890"], ["goteborg"], "frontend", JobAdSortBy.PublishedAtAsc).Value;

    // ---------------------------------------------------------------
    // Capture — happy path
    // ---------------------------------------------------------------

    [Fact]
    public void Capture_WithValidData_ProjectsCriteriaFields()
    {
        var criteria = ValidCriteria();

        var aggregate = RecentJobSearch.Capture(
            ValidJobSeekerId, criteria, currentCount: 42, Clock.UtcNow);

        aggregate.JobSeekerId.ShouldBe(ValidJobSeekerId);
        aggregate.Q.ShouldBe("backend");
        aggregate.Ssyk.ShouldHaveSingleItem().ShouldBe("12345");
        aggregate.Region.ShouldHaveSingleItem().ShouldBe("stockholm");
        aggregate.SortBy.ShouldBe(JobAdSortBy.PublishedAtDesc);
        aggregate.LastSeenCount.ShouldBe(42);
        aggregate.LastViewedAt.ShouldBe(Clock.UtcNow);
        aggregate.CreatedAt.ShouldBe(Clock.UtcNow);
    }

    [Fact]
    public void Capture_SetsFilterHashFromCalculator()
    {
        var criteria = ValidCriteria();

        var aggregate = RecentJobSearch.Capture(
            ValidJobSeekerId, criteria, currentCount: 1, Clock.UtcNow);

        aggregate.FilterHash.ShouldBe(FilterHashCalculator.Compute(criteria));
    }

    [Fact]
    public void Capture_RaisesCapturedDomainEvent()
    {
        var criteria = ValidCriteria();

        var aggregate = RecentJobSearch.Capture(
            ValidJobSeekerId, criteria, currentCount: 1, Clock.UtcNow);

        var evt = aggregate.DomainEvents.OfType<RecentJobSearchCapturedDomainEvent>()
            .ShouldHaveSingleItem();
        evt.RecentJobSearchId.ShouldBe(aggregate.Id);
        evt.JobSeekerId.ShouldBe(ValidJobSeekerId);
        evt.FilterHash.ShouldBe(aggregate.FilterHash);
        evt.OccurredAt.ShouldBe(Clock.UtcNow);
    }

    [Fact]
    public void Capture_GeneratesUniqueIds_ForEachInvocation()
    {
        var a = RecentJobSearch.Capture(ValidJobSeekerId, ValidCriteria(), 1, Clock.UtcNow);
        var b = RecentJobSearch.Capture(ValidJobSeekerId, ValidCriteria(), 1, Clock.UtcNow);

        a.Id.ShouldNotBe(b.Id);
    }

    [Fact]
    public void Capture_TwoDifferentCriteria_ProduceDifferentHashes()
    {
        var a = RecentJobSearch.Capture(ValidJobSeekerId, ValidCriteria(), 1, Clock.UtcNow);
        var b = RecentJobSearch.Capture(ValidJobSeekerId, AlternateCriteria(), 1, Clock.UtcNow);

        a.FilterHash.ShouldNotBe(b.FilterHash);
    }

    [Fact]
    public void Capture_SameCriteriaWithDifferentInputOrdering_ProducesSameHash()
    {
        // SearchCriteria.NormalizeList sorterar/dedupar → samma hash trots olika input-ordning.
        var c1 = SearchCriteria.Create(["zzz", "aaa"], ["bbb", "ccc"], "xx", JobAdSortBy.PublishedAtDesc).Value;
        var c2 = SearchCriteria.Create(["aaa", "zzz"], ["ccc", "bbb"], "xx", JobAdSortBy.PublishedAtDesc).Value;

        var a = RecentJobSearch.Capture(ValidJobSeekerId, c1, 1, Clock.UtcNow);
        var b = RecentJobSearch.Capture(ValidJobSeekerId, c2, 1, Clock.UtcNow);

        a.FilterHash.ShouldBe(b.FilterHash);
    }

    // ---------------------------------------------------------------
    // Capture — invarianter
    // ---------------------------------------------------------------

    [Fact]
    public void Capture_WithNullCriteria_Throws()
    {
        Should.Throw<ArgumentNullException>(() =>
            RecentJobSearch.Capture(ValidJobSeekerId, null!, 1, Clock.UtcNow));
    }

    [Fact]
    public void Capture_WithNegativeCount_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            RecentJobSearch.Capture(ValidJobSeekerId, ValidCriteria(), -1, Clock.UtcNow));
    }

    // ---------------------------------------------------------------
    // Bump — happy path + invarianter
    // ---------------------------------------------------------------

    [Fact]
    public void Bump_UpdatesLastViewedAtAndLastSeenCount()
    {
        var aggregate = RecentJobSearch.Capture(
            ValidJobSeekerId, ValidCriteria(), currentCount: 10, Clock.UtcNow);
        var bumpedAt = Clock.UtcNow.AddMinutes(15);

        aggregate.Bump(currentCount: 25, bumpedAt);

        aggregate.LastViewedAt.ShouldBe(bumpedAt);
        aggregate.LastSeenCount.ShouldBe(25);
    }

    [Fact]
    public void Bump_DoesNotMutateCriteriaFields()
    {
        var aggregate = RecentJobSearch.Capture(
            ValidJobSeekerId, ValidCriteria(), 1, Clock.UtcNow);
        var originalHash = aggregate.FilterHash;
        var originalQ = aggregate.Q;
        var originalSsyk = aggregate.Ssyk;
        var originalRegion = aggregate.Region;
        var originalSortBy = aggregate.SortBy;
        var originalCreatedAt = aggregate.CreatedAt;

        aggregate.Bump(50, Clock.UtcNow.AddHours(1));

        aggregate.FilterHash.ShouldBe(originalHash);
        aggregate.Q.ShouldBe(originalQ);
        aggregate.Ssyk.ShouldBe(originalSsyk);
        aggregate.Region.ShouldBe(originalRegion);
        aggregate.SortBy.ShouldBe(originalSortBy);
        aggregate.CreatedAt.ShouldBe(originalCreatedAt);
    }

    [Fact]
    public void Bump_RaisesBumpedDomainEvent()
    {
        var aggregate = RecentJobSearch.Capture(
            ValidJobSeekerId, ValidCriteria(), 1, Clock.UtcNow);
        aggregate.ClearDomainEvents();
        var bumpedAt = Clock.UtcNow.AddMinutes(5);

        aggregate.Bump(5, bumpedAt);

        var evt = aggregate.DomainEvents.OfType<RecentJobSearchBumpedDomainEvent>()
            .ShouldHaveSingleItem();
        evt.RecentJobSearchId.ShouldBe(aggregate.Id);
        evt.JobSeekerId.ShouldBe(ValidJobSeekerId);
        evt.OccurredAt.ShouldBe(bumpedAt);
    }

    [Fact]
    public void Bump_WithNegativeCount_Throws()
    {
        var aggregate = RecentJobSearch.Capture(
            ValidJobSeekerId, ValidCriteria(), 1, Clock.UtcNow);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            aggregate.Bump(-1, Clock.UtcNow));
    }

    // ---------------------------------------------------------------
    // MaxPerSeeker-konstant
    // ---------------------------------------------------------------

    [Fact]
    public void MaxPerSeeker_IsTwenty()
    {
        // CTO 2026-05-20 Q3 villkor — cap för YAGNI-dom på Variant A (re-query per row).
        RecentJobSearch.MaxPerSeeker.ShouldBe(20);
    }
}
