using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.JobAds.Events;
using Shouldly;

namespace JobbPilot.Domain.UnitTests.JobAds;

public class JobAdTests
{
    private static readonly FakeDateTimeProvider Clock = FakeDateTimeProvider.Default;

    private static (string title, Company company, string description, string url, JobSource source, DateTimeOffset publishedAt) ValidParams()
    {
        var company = Company.Create("Klarna").Value;
        return (
            "Senior Backend Engineer",
            company,
            "Vi söker en senior backend-utvecklare.",
            "https://jobs.klarna.com/job/123",
            JobSource.Manual,
            Clock.UtcNow
        );
    }

    [Fact]
    public void Create_WithValidData_ReturnsSuccess()
    {
        var (title, company, desc, url, source, publishedAt) = ValidParams();
        var result = JobAd.Create(title, company, desc, url, source, publishedAt, null, Clock);
        result.IsSuccess.ShouldBeTrue();
        result.Value.Title.ShouldBe(title);
        result.Value.Status.ShouldBe(JobAdStatus.Active);
    }

    [Fact]
    public void Create_WithEmptyTitle_ReturnsFailure()
    {
        var (_, company, desc, url, source, publishedAt) = ValidParams();
        var result = JobAd.Create("", company, desc, url, source, publishedAt, null, Clock);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("JobAd.TitleRequired");
    }

    [Fact]
    public void Create_WithInvalidUrl_ReturnsFailure()
    {
        var (title, company, desc, _, source, publishedAt) = ValidParams();
        var result = JobAd.Create(title, company, desc, "not-a-url", source, publishedAt, null, Clock);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("JobAd.UrlInvalid");
    }

    [Fact]
    public void Create_WithExpiresAtBeforePublishedAt_ReturnsFailure()
    {
        var (title, company, desc, url, source, publishedAt) = ValidParams();
        var expiresAt = publishedAt.AddDays(-1);
        var result = JobAd.Create(title, company, desc, url, source, publishedAt, expiresAt, Clock);
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("JobAd.InvalidDates");
    }

    [Fact]
    public void Create_WithValidData_RaisesJobAdCreatedDomainEvent()
    {
        var (title, company, desc, url, source, publishedAt) = ValidParams();
        var result = JobAd.Create(title, company, desc, url, source, publishedAt, null, Clock);
        result.IsSuccess.ShouldBeTrue();
        result.Value.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<JobAdCreatedDomainEvent>()
            .Title.ShouldBe(title);
    }

    [Fact]
    public void Archive_WhenActive_TransitionsToArchived()
    {
        var (title, company, desc, url, source, publishedAt) = ValidParams();
        var jobAd = JobAd.Create(title, company, desc, url, source, publishedAt, null, Clock).Value;
        jobAd.ClearDomainEvents();

        var result = jobAd.Archive(Clock);

        result.IsSuccess.ShouldBeTrue();
        jobAd.Status.ShouldBe(JobAdStatus.Archived);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ReturnsFailure()
    {
        var (title, company, desc, url, source, publishedAt) = ValidParams();
        var jobAd = JobAd.Create(title, company, desc, url, source, publishedAt, null, Clock).Value;
        jobAd.Archive(Clock);

        var result = jobAd.Archive(Clock);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("JobAd.AlreadyArchived");
    }

    [Fact]
    public void Archive_RaisesJobAdArchivedDomainEvent()
    {
        var (title, company, desc, url, source, publishedAt) = ValidParams();
        var jobAd = JobAd.Create(title, company, desc, url, source, publishedAt, null, Clock).Value;
        jobAd.ClearDomainEvents();

        jobAd.Archive(Clock);

        jobAd.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<JobAdArchivedDomainEvent>()
            .JobAdId.ShouldBe(jobAd.Id);
    }
}
