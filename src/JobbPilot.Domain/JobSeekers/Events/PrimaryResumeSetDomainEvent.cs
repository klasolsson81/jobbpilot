using JobbPilot.Domain.Common;
using JobbPilot.Domain.Resumes;

namespace JobbPilot.Domain.JobSeekers.Events;

public sealed record PrimaryResumeSetDomainEvent(
    JobSeekerId JobSeekerId,
    ResumeId? NewPrimaryResumeId,
    DateTimeOffset OccurredAt) : IDomainEvent;
