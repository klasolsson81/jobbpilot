using Mediator;

namespace JobbPilot.Application.JobAds.Queries.GetJobAd;

public sealed record GetJobAdQuery(Guid Id) : IQuery<JobAdDto?>;
