using JobbPilot.Domain.Common;

namespace JobbPilot.Domain.Resumes.Events;

public sealed record ResumeLanguageChangedDomainEvent(
    ResumeId ResumeId,
    ResumeLanguage NewLanguage,
    DateTimeOffset OccurredAt) : IDomainEvent;
