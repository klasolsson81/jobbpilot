using JobbPilot.Application.Common.Abstractions;
using JobbPilot.Application.Common.Auditing;
using JobbPilot.Domain.Common;
using Mediator;

namespace JobbPilot.Application.Resumes.Commands.SetResumeLanguage;

public sealed record SetResumeLanguageCommand(
    Guid ResumeId,
    string Language)
    : ICommand<Result>, IAuthenticatedRequest, IAuditableCommand<Result>
{
    public string EventType => "Resume.LanguageChanged";
    public string AggregateType => "Resume";
    public Guid ExtractAggregateId(Result response) => ResumeId;
}
