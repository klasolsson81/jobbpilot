namespace JobbPilot.Application.JobAds.Queries;

public sealed record JobAdDto(
    Guid Id,
    string Title,
    string CompanyName,
    string Description,
    string Url,
    string Source,
    string Status,
    DateTimeOffset PublishedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt
);
