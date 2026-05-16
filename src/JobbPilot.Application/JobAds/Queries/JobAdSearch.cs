using JobbPilot.Domain.JobAds;
using Microsoft.EntityFrameworkCore;

namespace JobbPilot.Application.JobAds.Queries;

/// <summary>
/// Delad sök-komposition för jobbannonser (ADR 0039 Beslut 1). Filter- och
/// sort-logiken är ETT knowledge piece (Hunt/Thomas DRY) — den ägs här och
/// återanvänds av både <c>ListJobAdsQueryHandler</c> och
/// <c>RunSavedSearchQueryHandler</c> så de aldrig kan divergera. Behaviour-
/// preserving extraktion från ListJobAdsQueryHandler (Fowler 2018 — Move
/// Function); befintliga ListJobAds-tester är regressions-grind.
/// </summary>
internal static class JobAdSearch
{
    // F2-P9 (TD-70, CTO-rond 2026-05-13). Filter via Postgres generated columns
    // (Q2-C) — ssyk_concept_id + region_concept_id är STORED-derived från
    // raw_payload, B-tree-indexerade för equality-lookup. q-filter via Like
    // + .ToLower() (Q3-A KISS för Fas 2-volym 5-15k rader). EF.Functions.ILike
    // skulle vara renare men ligger i Npgsql-extension → Application-Clean-Arch-
    // brott. EF.Functions.Like är Microsoft.EntityFrameworkCore (provider-
    // agnostiskt API).
    // Shadow-properties refereras via EF.Property<string?>(...) eftersom de
    // inte är top-level Domain-fält (Evans 2003 §14 ACL — JobTech-taxonomi
    // är inte JobbPilots ubiquitous language).
    public static IQueryable<JobAd> ApplyCriteria(
        IQueryable<JobAd> source, string? ssyk, string? region, string? q)
    {
        if (!string.IsNullOrWhiteSpace(ssyk))
        {
            var ssykValue = ssyk;
            source = source.Where(j => EF.Property<string?>(j, "SsykConceptId") == ssykValue);
        }

        if (!string.IsNullOrWhiteSpace(region))
        {
            var regionValue = region;
            source = source.Where(j => EF.Property<string?>(j, "RegionConceptId") == regionValue);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.ToLowerInvariant()}%";
            // EF/Npgsql translaterar .ToLower() (utan culture-argument) till
            // SQL LOWER(col); .ToLowerInvariant() har ingen mapping i Npgsql
            // (verifierat 2026-05-13 via integration-test). CA1304-suppress:
            // detta är LINQ-translation till SQL, inte runtime-string-op —
            // culture är irrelevant (Postgres LOWER är locale-baserad i sin
            // egen rätt). Pattern lower:as redan invariant-side (C#).
#pragma warning disable CA1304, CA1311
            source = source.Where(j =>
                EF.Functions.Like(j.Title.ToLower(), pattern) ||
                EF.Functions.Like(j.Description.ToLower(), pattern));
#pragma warning restore CA1304, CA1311
        }

        return source;
    }

    // Alla enum-värden explicit listade + throw på unnamed (CS8524-disciplin).
    // Validators (ListJobAdsQueryValidator / SearchCriteria.Create) skyddar mot
    // invalid values innan denna metod, så throw är defense-in-depth — exception
    // når inte runtime. Vid framtida JobAdSortBy-tillägg: case missas →
    // ArgumentOutOfRangeException → integration-test fail (fail-fast).
    public static IQueryable<JobAd> ApplySort(IQueryable<JobAd> source, JobAdSortBy sortBy) =>
        sortBy switch
        {
            JobAdSortBy.PublishedAtDesc => source.OrderByDescending(j => j.PublishedAt).ThenBy(j => j.Id),
            JobAdSortBy.PublishedAtAsc => source.OrderBy(j => j.PublishedAt).ThenBy(j => j.Id),
            // NULL-ExpiresAt sorteras sist (har inget slut-datum = pågående).
            JobAdSortBy.ExpiresAtDesc =>
                source.OrderBy(j => j.ExpiresAt == null)
                      .ThenByDescending(j => j.ExpiresAt)
                      .ThenBy(j => j.Id),
            JobAdSortBy.ExpiresAtAsc =>
                source.OrderBy(j => j.ExpiresAt == null)
                      .ThenBy(j => j.ExpiresAt)
                      .ThenBy(j => j.Id),
            _ => throw new ArgumentOutOfRangeException(
                nameof(sortBy), sortBy, "Unknown JobAdSortBy — validator should reject."),
        };
}
