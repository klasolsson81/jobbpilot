using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.Common.Auditing;
using JobbPilot.Application.SavedSearches.Queries.RunSavedSearch;
using JobbPilot.Application.UnitTests.Common;
using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.JobSeekers;
using JobbPilot.Domain.SavedSearches;
using NSubstitute;
using Shouldly;

namespace JobbPilot.Application.UnitTests.SavedSearches.Queries;

public class RunSavedSearchQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();

    public RunSavedSearchQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
    }

    private static JobAd NewJobAd(string title) =>
        JobAd.Create(
            title,
            Company.Create("Klarna").Value,
            "Vi söker en backend-utvecklare.",
            "https://jobs.klarna.com/job/1",
            JobSource.Manual,
            FakeDateTimeProvider.Default.UtcNow.AddDays(-1),
            FakeDateTimeProvider.Default.UtcNow.AddDays(30),
            FakeDateTimeProvider.Default).Value;

    private static async Task<(JobSeeker seeker, SavedSearch saved)> SeedAsync(
        JobbPilot.Infrastructure.Persistence.AppDbContext db, Guid userId,
        string? ssyk = null, string? region = null, string? q = null)
    {
        var seeker = JobSeeker.Register(userId, "Test User", FakeDateTimeProvider.Default).Value;
        db.JobSeekers.Add(seeker);
        // Default: q=null,region=null kräver minst ett kriterium → ssyk default.
        var criteria = SearchCriteria.Create(
            ssyk ?? (q is null && region is null ? "12345" : null),
            region, q, JobAdSortBy.PublishedAtDesc).Value;
        var saved = SavedSearch.Create(seeker.Id, "Kör mig", criteria, false,
            FakeDateTimeProvider.Default).Value;
        db.SavedSearches.Add(saved);
        await db.SaveChangesAsync(CancellationToken.None);
        return (seeker, saved);
    }

    [Fact]
    public async Task Handle_WhenOwned_ReturnsPagedResult()
    {
        var db = TestAppDbContextFactory.Create();
        var (_, saved) = await SeedAsync(db, _userId, q: "backend");
        db.JobAds.Add(NewJobAd("Backend-utvecklare"));
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new RunSavedSearchQueryHandler(
            db, _currentUser, Substitute.For<IFailedAccessLogger>());

        var result = await handler.Handle(
            new RunSavedSearchQuery(saved.Id.Value), CancellationToken.None);

        result.ShouldNotBeNull();
        result!.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task Handle_DoesNotWriteLastRunAt()
    {
        // ADR 0039 Beslut 2: run är en QUERY utan skriv-sidoeffekt.
        // last_run_at-skrivlogiken tillhör Fas 5 — får inte sättas i Fas 2.
        var db = TestAppDbContextFactory.Create();
        var (_, saved) = await SeedAsync(db, _userId);
        saved.LastRunAt.ShouldBeNull(); // utgångsläge

        var handler = new RunSavedSearchQueryHandler(
            db, _currentUser, Substitute.For<IFailedAccessLogger>());

        await handler.Handle(new RunSavedSearchQuery(saved.Id.Value), CancellationToken.None);

        // Reload från context — ingen skrivning ska ha skett.
        var reloaded = db.SavedSearches.Single(s => s.Id == saved.Id);
        reloaded.LastRunAt.ShouldBeNull();
        // UpdatedAt får inte heller röras av en ren körning.
        reloaded.UpdatedAt.ShouldBe(FakeDateTimeProvider.Default.UtcNow);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ReturnsNull()
    {
        var db = TestAppDbContextFactory.Create();
        var (_, saved) = await SeedAsync(db, _userId);
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var handler = new RunSavedSearchQueryHandler(
            db, currentUser, Substitute.For<IFailedAccessLogger>());

        var result = await handler.Handle(
            new RunSavedSearchQuery(saved.Id.Value), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WhenIdUnknown_ReturnsNullAndDoesNotLog()
    {
        var db = TestAppDbContextFactory.Create();
        await SeedAsync(db, _userId);
        var failedAccessLogger = Substitute.For<IFailedAccessLogger>();
        var handler = new RunSavedSearchQueryHandler(db, _currentUser, failedAccessLogger);

        var result = await handler.Handle(
            new RunSavedSearchQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldBeNull();
        failedAccessLogger.DidNotReceive().LogCrossUserAttempt(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_WhenBelongsToOtherUser_ReturnsNullAndLogs()
    {
        var db = TestAppDbContextFactory.Create();
        var otherUserId = Guid.NewGuid();
        var (_, otherSaved) = await SeedAsync(db, otherUserId);
        var ownSeeker = JobSeeker.Register(_userId, "Current", FakeDateTimeProvider.Default).Value;
        db.JobSeekers.Add(ownSeeker);
        await db.SaveChangesAsync(CancellationToken.None);

        var failedAccessLogger = Substitute.For<IFailedAccessLogger>();
        var handler = new RunSavedSearchQueryHandler(db, _currentUser, failedAccessLogger);

        var result = await handler.Handle(
            new RunSavedSearchQuery(otherSaved.Id.Value), CancellationToken.None);

        result.ShouldBeNull();
        failedAccessLogger.Received(1).LogCrossUserAttempt(
            "SavedSearch", otherSaved.Id.Value, _userId, "RunSavedSearch");
    }
}
