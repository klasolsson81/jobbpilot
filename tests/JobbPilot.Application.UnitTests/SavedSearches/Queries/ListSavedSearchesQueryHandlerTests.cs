using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.SavedSearches.Queries.ListSavedSearches;
using JobbPilot.Application.UnitTests.Common;
using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.JobSeekers;
using JobbPilot.Domain.SavedSearches;
using NSubstitute;
using Shouldly;

namespace JobbPilot.Application.UnitTests.SavedSearches.Queries;

public class ListSavedSearchesQueryHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();

    public ListSavedSearchesQueryHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
    }

    private static SavedSearch NewSaved(JobSeekerId seekerId, string name) =>
        SavedSearch.Create(
            seekerId, name,
            SearchCriteria.Create(["12345"], null, null, JobAdSortBy.PublishedAtDesc).Value,
            false, FakeDateTimeProvider.Default).Value;

    [Fact]
    public async Task Handle_ReturnsOnlyOwnSavedSearches()
    {
        var db = TestAppDbContextFactory.Create();
        var seeker = JobSeeker.Register(_userId, "Owner", FakeDateTimeProvider.Default).Value;
        db.JobSeekers.Add(seeker);
        var other = JobSeeker.Register(Guid.NewGuid(), "Other", FakeDateTimeProvider.Default).Value;
        db.JobSeekers.Add(other);

        db.SavedSearches.Add(NewSaved(seeker.Id, "Min A"));
        db.SavedSearches.Add(NewSaved(seeker.Id, "Min B"));
        db.SavedSearches.Add(NewSaved(other.Id, "Annans"));
        await db.SaveChangesAsync(CancellationToken.None);

        var handler = new ListSavedSearchesQueryHandler(db, _currentUser);
        var result = await handler.Handle(new ListSavedSearchesQuery(), CancellationToken.None);

        result.Count.ShouldBe(2);
        result.ShouldAllBe(s => s.Name == "Min A" || s.Name == "Min B");
    }

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ReturnsEmpty()
    {
        var db = TestAppDbContextFactory.Create();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns((Guid?)null);
        var handler = new ListSavedSearchesQueryHandler(db, currentUser);

        var result = await handler.Handle(new ListSavedSearchesQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenNoJobSeeker_ReturnsEmpty()
    {
        var db = TestAppDbContextFactory.Create();
        var handler = new ListSavedSearchesQueryHandler(db, _currentUser);

        var result = await handler.Handle(new ListSavedSearchesQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
