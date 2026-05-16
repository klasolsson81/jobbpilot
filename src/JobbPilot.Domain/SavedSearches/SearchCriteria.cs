using System.Text.RegularExpressions;
using JobbPilot.Domain.Common;
using JobbPilot.Domain.JobAds;

namespace JobbPilot.Domain.SavedSearches;

/// <summary>
/// Value object — en reproducerbar jobbsökning. Speglar ListJobAds-sökytan
/// (ADR 0039 Beslut 3). Page/PageSize exkluderas medvetet: de är
/// runtime-pagination, inte del av sökningens identitet. SortBy ingår
/// (determinerar paginerat resultat → del av användarens avsikt).
///
/// Record → värde-likhet (Evans 2003 kap. 5). Invarianter speglar
/// ListJobAdsQueryValidator så en sparad sökning aldrig kan vara mer
/// tillåtande än motsvarande live-sökning.
/// </summary>
public sealed record SearchCriteria
{
    // JobTech v2 concept-id-format — identiskt med ListJobAdsQueryValidator
    // (CTO-rond 2026-05-13 Q7a/Q7b). Defense-in-depth default-deny.
    private static readonly Regex ConceptIdPattern =
        new(@"^[A-Za-z0-9_-]{1,32}$", RegexOptions.Compiled);

    private const int QMinLength = 2;
    private const int QMaxLength = 100;

    public string? Ssyk { get; private init; }
    public string? Region { get; private init; }
    public string? Q { get; private init; }
    public JobAdSortBy SortBy { get; private init; }

    // EF + record copy-semantik
    private SearchCriteria() { }

    public static Result<SearchCriteria> Create(
        string? ssyk, string? region, string? q, JobAdSortBy sortBy)
    {
        if (!Enum.IsDefined(sortBy))
            return Result.Failure<SearchCriteria>(DomainError.Validation(
                "SearchCriteria.InvalidSortBy", "Ogiltig sortering."));

        var normSsyk = Normalize(ssyk);
        var normRegion = Normalize(region);
        var normQ = Normalize(q);

        if (normSsyk is null && normRegion is null && normQ is null)
            return Result.Failure<SearchCriteria>(DomainError.Validation(
                "SearchCriteria.Empty",
                "Minst ett sökkriterium (yrkesområde, region eller fritext) krävs."));

        if (normSsyk is not null && !ConceptIdPattern.IsMatch(normSsyk))
            return Result.Failure<SearchCriteria>(DomainError.Validation(
                "SearchCriteria.InvalidSsyk",
                "Yrkesområde måste vara en giltig JobTech concept-id (1-32 tecken, alfanumeriskt + _-)."));

        if (normRegion is not null && !ConceptIdPattern.IsMatch(normRegion))
            return Result.Failure<SearchCriteria>(DomainError.Validation(
                "SearchCriteria.InvalidRegion",
                "Region måste vara en giltig JobTech location-concept-id (1-32 tecken, alfanumeriskt + _-)."));

        if (normQ is not null && (normQ.Length < QMinLength || normQ.Length > QMaxLength))
            return Result.Failure<SearchCriteria>(DomainError.Validation(
                "SearchCriteria.InvalidQ", "Söktext måste vara 2-100 tecken."));

        return Result.Success(new SearchCriteria
        {
            Ssyk = normSsyk,
            Region = normRegion,
            Q = normQ,
            SortBy = sortBy,
        });
    }

    private static string? Normalize(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
