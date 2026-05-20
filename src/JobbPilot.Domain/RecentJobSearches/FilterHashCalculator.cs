using System.Security.Cryptography;
using System.Text.Json;
using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.SavedSearches;

namespace JobbPilot.Domain.RecentJobSearches;

/// <summary>
/// Deterministic FilterHash-beräkning (CTO 2026-05-20 Variant A). Hashen är
/// uniqueness-identitet för <see cref="RecentJobSearch"/> per JobSeeker:
/// <c>UNIQUE(job_seeker_id, filter_hash)</c>. Domain-placerad eftersom
/// canonical-formatet är ett domän-kontrakt — om Infrastructure ändrar
/// serialisering tyst förlorar vi unique-index-integritet (Clean Arch
/// dependency rule, Martin 2017 kap. 22).
///
/// <para>
/// Canonical-JSON: <c>{"q":string?|null,"ssyk":[...],"region":[...],"sortBy":int}</c>.
/// Listorna är redan sorted+distinct ordinal från <see cref="SearchCriteria"/>:s
/// invarianter → deterministisk. SHA-256 ger fix 64-tecken hex-output, ingen
/// känd preimage-attack relevant för denna icke-säkerhets-användning.
/// </para>
/// </summary>
public static class FilterHashCalculator
{
    public static string Compute(SearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        return Compute(criteria.Q, criteria.Ssyk, criteria.Region, criteria.SortBy);
    }

    public static string Compute(
        string? q,
        IReadOnlyList<string> ssyk,
        IReadOnlyList<string> region,
        JobAdSortBy sortBy)
    {
        ArgumentNullException.ThrowIfNull(ssyk);
        ArgumentNullException.ThrowIfNull(region);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            if (q is null)
                writer.WriteNull("q");
            else
                writer.WriteString("q", q);

            writer.WriteStartArray("ssyk");
            foreach (var s in ssyk)
                writer.WriteStringValue(s);
            writer.WriteEndArray();

            writer.WriteStartArray("region");
            foreach (var r in region)
                writer.WriteStringValue(r);
            writer.WriteEndArray();

            writer.WriteNumber("sortBy", (int)sortBy);
            writer.WriteEndObject();
        }

        var hash = SHA256.HashData(stream.ToArray());
        return Convert.ToHexStringLower(hash);
    }
}
