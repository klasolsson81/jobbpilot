using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.JobAds.Abstractions;
using JobbPilot.Application.JobAds.Queries.GetTaxonomyTree;
using JobbPilot.Application.RecentJobSearches.Queries.ListRecentSearches;
using JobbPilot.Application.UnitTests.Common;
using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.JobSeekers;
using JobbPilot.Domain.RecentJobSearches;
using JobbPilot.Domain.SavedSearches;
using NSubstitute;
using Shouldly;

namespace JobbPilot.Application.UnitTests.RecentJobSearches.Queries;

public class ListRecentSearchesQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITaxonomyReadModel _taxonomy = Substitute.For<ITaxonomyReadModel>();
    private readonly Guid _userId = Guid.NewGuid();

    public ListRecentSearchesQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
#pragma warning disable CA2012 // ValueTask från NSubstitute-stub konsumeras varje gång av handlern
        _taxonomy.ResolveLabelsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var ids = call.Arg<IReadOnlyList<string>>();
                IReadOnlyList<TaxonomyLabelDto> labels = ids
                    .Select(id => new TaxonomyLabelDto(id, $"Label-{id}"))
                    .ToList();
                return ValueTask.FromResult(labels);
            });
#pragma warning restore CA2012
    }

    private async Task<JobSeeker> SeedSeekerAsync(
        JobbPilot.Infrastructure.Persistence.AppDbContext db)
    {
        var seeker = JobSeeker.Register(_userId, "Test User", FakeDateTimeProvider.Default).Value;
        db.JobSeekers.Add(seeker);
        await db.SaveChangesAsync(CancellationToken.None);
        return seeker;
    }

    private static RecentJobSearch CaptureRow(
        JobSeekerId seekerId,
        string? q,
        DateTimeOffset viewedAt,
        int lastSeenCount = 0)
    {
        var criteria = SearchCriteria.Create(["12345"], ["stockholm"], q, JobAdSortBy.PublishedAtDesc).Value;
        return RecentJobSearch.Capture(seekerId, criteria, lastSeenCount, viewedAt);
    }

    [Fact]
    public async Task Handle_WhenUserIdNull_ReturnsEmpty()
    {
        var db = TestAppDbContextFactory.Create();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var handler = new ListRecentSearchesQueryHandler(db, currentUser, _taxonomy);

        var result = await handler.Handle(new ListRecentSearchesQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNoSeeker_ReturnsEmpty()
    {
        var db = TestAppDbContextFactory.Create();
        var handler = new ListRecentSearchesQueryHandler(db, _currentUser, _taxonomy);

        var result = await handler.Handle(new ListRecentSearchesQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsItemsSortedByLastViewedAtDesc()
    {
        var db = TestAppDbContextFactory.Create();
        var seeker = await SeedSeekerAsync(db);
        var now = FakeDateTimeProvider.Default.UtcNow;

        db.RecentJobSearches.Add(CaptureRow(seeker.Id, "oldest", now.AddHours(-3)));
        db.RecentJobSearches.Add(CaptureRow(seeker.Id, "newest", now));
        db.RecentJobSearches.Add(CaptureRow(seeker.Id, "middle", now.AddHours(-1)));
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ListRecentSearchesQueryHandler(db, _currentUser, _taxonomy);
        var result = await handler.Handle(new ListRecentSearchesQuery(), CancellationToken.None);

        result.Count.ShouldBe(3);
        result[0].Q.ShouldBe("newest");
        result[1].Q.ShouldBe("middle");
        result[2].Q.ShouldBe("oldest");
    }

    [Fact]
    public async Task Handle_ProjectsNewCountAsCurrentMinusLastSeen_CappedAtZero()
    {
        var db = TestAppDbContextFactory.Create();
        var seeker = await SeedSeekerAsync(db);
        var now = FakeDateTimeProvider.Default.UtcNow;

        // CurrentCount kommer från live-count mot JobAds (= 0 i InMemory test).
        // LastSeenCount lagrat på aggregatet — om större än CurrentCount så NewCount = 0.
        db.RecentJobSearches.Add(CaptureRow(seeker.Id, "with-seen", now, lastSeenCount: 5));
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ListRecentSearchesQueryHandler(db, _currentUser, _taxonomy);
        var result = await handler.Handle(new ListRecentSearchesQuery(), CancellationToken.None);

        var dto = result.ShouldHaveSingleItem();
        dto.CurrentCount.ShouldBe(0);          // tom DB
        dto.NewCount.ShouldBe(0);              // max(0, 0 - 5)
    }

    [Fact]
    public async Task Handle_DerivesLabelFromQuery_WhenQPresent()
    {
        var db = TestAppDbContextFactory.Create();
        var seeker = await SeedSeekerAsync(db);
        db.RecentJobSearches.Add(CaptureRow(seeker.Id, "backend dev", FakeDateTimeProvider.Default.UtcNow));
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ListRecentSearchesQueryHandler(db, _currentUser, _taxonomy);
        var result = await handler.Handle(new ListRecentSearchesQuery(), CancellationToken.None);

        result.ShouldHaveSingleItem().Label.ShouldBe("backend dev");
    }

    [Fact]
    public async Task Handle_DerivesLabelFromFirstSsykLabel_WhenQNull()
    {
        var db = TestAppDbContextFactory.Create();
        var seeker = await SeedSeekerAsync(db);

        // Q=null + ssyk + region → label från ssykLabel
        var criteria = SearchCriteria.Create(["77777"], ["stockholm"], null, JobAdSortBy.PublishedAtDesc).Value;
        db.RecentJobSearches.Add(
            RecentJobSearch.Capture(seeker.Id, criteria, 0, FakeDateTimeProvider.Default.UtcNow));
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ListRecentSearchesQueryHandler(db, _currentUser, _taxonomy);
        var result = await handler.Handle(new ListRecentSearchesQuery(), CancellationToken.None);

        result.ShouldHaveSingleItem().Label.ShouldBe("Label-77777");
    }

    [Fact]
    public async Task Handle_FiltersToOwnerOnly_CrossUserRowsExcluded()
    {
        var db = TestAppDbContextFactory.Create();
        var seeker = await SeedSeekerAsync(db);
        var now = FakeDateTimeProvider.Default.UtcNow;

        // Egen rad
        db.RecentJobSearches.Add(CaptureRow(seeker.Id, "mine", now));

        // Annan användare
        var otherSeeker = JobSeeker.Register(Guid.NewGuid(), "Other", FakeDateTimeProvider.Default).Value;
        db.JobSeekers.Add(otherSeeker);
        db.RecentJobSearches.Add(CaptureRow(otherSeeker.Id, "theirs", now));
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ListRecentSearchesQueryHandler(db, _currentUser, _taxonomy);
        var result = await handler.Handle(new ListRecentSearchesQuery(), CancellationToken.None);

        result.ShouldHaveSingleItem().Q.ShouldBe("mine");
    }
}
