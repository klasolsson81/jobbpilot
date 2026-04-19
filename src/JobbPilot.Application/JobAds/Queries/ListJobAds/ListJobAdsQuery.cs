using Mediator;

namespace JobbPilot.Application.JobAds.Queries.ListJobAds;

public sealed record ListJobAdsQuery : IQuery<IReadOnlyList<JobAdDto>>;
