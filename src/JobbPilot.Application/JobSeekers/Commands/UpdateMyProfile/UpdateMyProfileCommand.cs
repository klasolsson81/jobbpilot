using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Domain.Common;
using Mediator;

namespace JobbPilot.Application.JobSeekers.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    string? DisplayName,
    string? Language,
    bool? EmailNotifications,
    bool? WeeklySummary) : ICommand<Result>, IAuthenticatedRequest;
