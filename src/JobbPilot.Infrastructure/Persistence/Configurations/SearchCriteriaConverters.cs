using System.Text.Json;
using System.Text.Json.Serialization;
using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.SavedSearches;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JobbPilot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Persistens-yta för <see cref="SearchCriteria"/> (CTO Yta A3, ADR 0042
/// Beslut B 2026-05-16). `OwnsOne(...).ToJson()` mappar inte
/// <c>IReadOnlyList&lt;string&gt;</c> stabilt i Npgsql (issue #3129) →
/// property-level <see cref="ValueConverter"/> mot en <c>jsonb</c>-kolumn
/// istället, med en tolerant <see cref="JsonConverter{T}"/> som läser BÅDE
/// gammal skalär-form (<c>"Ssyk":"x"</c>) och ny array-form
/// (<c>"Ssyk":["x"]</c>) — ingen data-migration (lazy on-read). Default-deny:
/// allt som inte är sträng-eller-strängarray avvisas (Saltzer/Schroeder 1975).
/// Bor i Infrastructure — Domain förblir serialiserings-/EF-fritt
/// (CLAUDE.md §2.1).
/// </summary>
internal sealed class SearchCriteriaJsonConverter : JsonConverter<SearchCriteria>
{
    public override SearchCriteria Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("SearchCriteria-jsonb måste vara ett objekt.");

        List<string> ssyk = [];
        List<string> region = [];
        string? q = null;
        JobAdSortBy sortBy = JobAdSortBy.PublishedAtDesc;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;
            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Oväntad token i SearchCriteria-jsonb.");

            var prop = reader.GetString();
            reader.Read();

            switch (prop)
            {
                case "Ssyk":
                    ssyk = ReadStringOrStringArray(ref reader, "Ssyk");
                    break;
                case "Region":
                    region = ReadStringOrStringArray(ref reader, "Region");
                    break;
                case "Q":
                    q = reader.TokenType switch
                    {
                        JsonTokenType.Null => null,
                        JsonTokenType.String => reader.GetString(),
                        _ => throw new JsonException("SearchCriteria.Q måste vara sträng eller null."),
                    };
                    break;
                case "SortBy":
                    if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var sb))
                        throw new JsonException("SearchCriteria.SortBy måste vara ett heltal.");
                    sortBy = (JobAdSortBy)sb;
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        // Re-validera via Domain-faktorn (single source of invariants +
        // sorterad+distinct-normalisering). Lagrad data var giltig vid skrivning;
        // gammal skalär→[x] passerar. Fail-safe default-deny vid korrupt data.
        var result = SearchCriteria.Create(ssyk, region, q, sortBy);
        if (result.IsFailure)
            throw new JsonException(
                $"Lagrad SearchCriteria-jsonb bröt domän-invariant: {result.Error.Code}.");
        return result.Value;
    }

    public override void Write(
        Utf8JsonWriter writer, SearchCriteria value, JsonSerializerOptions options)
    {
        // Skriver alltid ny array-form + PascalCase (matchar befintlig
        // kolumn-nyckelkonvention). SortBy som heltal (oförändrad gammal form).
        writer.WriteStartObject();

        writer.WritePropertyName("Ssyk");
        writer.WriteStartArray();
        foreach (var s in value.Ssyk)
            writer.WriteStringValue(s);
        writer.WriteEndArray();

        writer.WritePropertyName("Region");
        writer.WriteStartArray();
        foreach (var r in value.Region)
            writer.WriteStringValue(r);
        writer.WriteEndArray();

        if (value.Q is null)
            writer.WriteNull("Q");
        else
            writer.WriteString("Q", value.Q);

        writer.WriteNumber("SortBy", (int)value.SortBy);

        writer.WriteEndObject();
    }

    // Tolerant + default-deny: sträng → [s]; array-av-strängar → lista;
    // null → []. Nummer/objekt/bool/array-med-icke-sträng/null-element →
    // hård avvisning (ingen tyst koercering).
    private static List<string> ReadStringOrStringArray(
        ref Utf8JsonReader reader, string field)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return [];
            case JsonTokenType.String:
                return [reader.GetString()!];
            case JsonTokenType.StartArray:
                var list = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return list;
                    if (reader.TokenType != JsonTokenType.String)
                        throw new JsonException(
                            $"SearchCriteria.{field}-arrayen får bara innehålla strängar.");
                    list.Add(reader.GetString()!);
                }
                throw new JsonException($"Oavslutad SearchCriteria.{field}-array.");
            default:
                throw new JsonException(
                    $"SearchCriteria.{field} måste vara sträng, strängarray eller null.");
        }
    }
}

/// <summary>
/// EF Core value-conversion + value-comparison för <see cref="SearchCriteria"/>
/// mappad mot en <c>jsonb</c>-kolumn (CTO Yta A3). Comparern använder VO:ts
/// strukturella record-equality (samma som SavedSearch jsonb-dedupe vilar på).
/// </summary>
internal static class SearchCriteriaConversion
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions();
        o.Converters.Add(new SearchCriteriaJsonConverter());
        return o;
    }

    public static readonly ValueConverter<SearchCriteria, string> Converter =
        new(
            v => JsonSerializer.Serialize(v, Options),
            s => JsonSerializer.Deserialize<SearchCriteria>(s, Options)!);

    // SearchCriteria är en immutabel record-VO → snapshot = samma instans
    // (listorna är normaliserade arrays skapade i Create, muteras aldrig).
    public static readonly ValueComparer<SearchCriteria> Comparer =
        new(
            (a, b) => a == null ? b == null : a.Equals(b),
            v => v.GetHashCode(),
            v => v);
}
