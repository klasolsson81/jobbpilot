using JobbPilot.Application.Common.Abstractions;
using Mediator;

namespace JobbPilot.Application.JobSeekers.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IQuery<JobSeekerProfileDto?>, IAuthenticatedRequest;
