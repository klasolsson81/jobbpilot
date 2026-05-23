using JobbPilot.Domain.Common;
using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.JobSeekers;

namespace JobbPilot.Domain.SavedJobAds.Events;

public sealed record JobAdUnsavedDomainEvent(
    SavedJobAdId SavedJobAdId,
    JobSeekerId JobSeekerId,
    JobAdId JobAdId,
    DateTimeOffset OccurredAt) : IDomainEvent;
