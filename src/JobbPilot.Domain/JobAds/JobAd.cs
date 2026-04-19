using JobbPilot.Domain.Common;
using JobbPilot.Domain.JobAds.Events;

namespace JobbPilot.Domain.JobAds;

public sealed class JobAd : AggregateRoot<JobAdId>
{
    public string Title { get; private set; } = null!;
    public Company Company { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Url { get; private set; } = null!;
    public JobSource Source { get; private set; } = null!;
    public JobAdStatus Status { get; private set; } = null!;
    public DateTimeOffset PublishedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    // EF Core constructor
    private JobAd() { }

    private JobAd(
        JobAdId id,
        string title,
        Company company,
        string description,
        string url,
        JobSource source,
        DateTimeOffset publishedAt,
        DateTimeOffset? expiresAt,
        DateTimeOffset createdAt) : base(id)
    {
        Title = title;
        Company = company;
        Description = description;
        Url = url;
        Source = source;
        Status = JobAdStatus.Active;
        PublishedAt = publishedAt;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public static Result<JobAd> Create(
        string? title,
        Company company,
        string? description,
        string? url,
        JobSource source,
        DateTimeOffset publishedAt,
        DateTimeOffset? expiresAt,
        IDateTimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure<JobAd>(
                DomainError.Validation("JobAd.TitleRequired", "Titel är obligatorisk."));
        if (title.Length > 300)
            return Result.Failure<JobAd>(
                DomainError.Validation("JobAd.TitleTooLong", "Titel får vara max 300 tecken."));
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<JobAd>(
                DomainError.Validation("JobAd.DescriptionRequired", "Beskrivning är obligatorisk."));
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
            return Result.Failure<JobAd>(
                DomainError.Validation("JobAd.UrlInvalid", "URL måste vara en giltig absolut URL."));
        if (expiresAt.HasValue && expiresAt.Value <= publishedAt)
            return Result.Failure<JobAd>(
                DomainError.Validation("JobAd.InvalidDates", "ExpiresAt måste vara efter PublishedAt."));

        var now = clock.UtcNow;
        var id = JobAdId.New();
        var jobAd = new JobAd(id, title.Trim(), company, description.Trim(),
                              url, source, publishedAt, expiresAt, now);
        jobAd.RaiseDomainEvent(new JobAdCreatedDomainEvent(id, title.Trim(), now));
        return Result.Success(jobAd);
    }

    public Result Archive(IDateTimeProvider clock)
    {
        if (Status == JobAdStatus.Archived)
            return Result.Failure(
                DomainError.Validation("JobAd.AlreadyArchived", "Annonsen är redan arkiverad."));

        Status = JobAdStatus.Archived;
        RaiseDomainEvent(new JobAdArchivedDomainEvent(Id, clock.UtcNow));
        return Result.Success();
    }
}
