using JobbPilot.Api.IntegrationTests.Infrastructure;
using JobbPilot.Domain.Common;
using JobbPilot.Domain.JobAds;
using JobbPilot.Domain.JobSeekers;
using JobbPilot.Domain.SavedSearches;
using JobbPilot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;

namespace JobbPilot.Api.IntegrationTests.SavedSearches;

// Batch 3 — Yta A3 (senior-cto-advisor agentId a3f867af2b57df564, ADR 0042
// Beslut B.4). Bakåtkompat-yta för saved_searches.criteria jsonb:
// gammal skalär-form ({"Ssyk":"x"}) måste fortfarande deserialiseras korrekt
// mot den nya list-formen, med TOLERANT DEFAULT-DENY (felaktig form koerceras
// EJ tyst — den kastar).
//
// DISCOVERY-FYND (rapporterat): det finns INGEN custom SearchCriteriaJsonConverter
// on-disk. Persistens sker via EF Core OwnsOne(s => s.Criteria).ToJson() i
// SavedSearchConfiguration.cs. Dessa tester verifierar därför BETEENDET på den
// faktiska persistensvägen (riktig Postgres jsonb roundtrip via AppDbContext),
// inte en konverter-klass vid namn — så de överlever oavsett om impl väljer
// custom JsonConverter<SearchCriteria>, EF value converter eller datamigrering.
//
// RÖD tills list-formen + bakåtkompat-läsningen implementerats.
[Collection("Api")]
public sealed class SearchCriteriaJsonbBackcompatTests(ApiFactory factory)
{
    private readonly ApiFactory _factory = factory;

    private static async Task<JobSeeker> SeedSeekerAsync(AppDbContext db, IDateTimeProvider clock, CancellationToken ct)
    {
        var seeker = JobSeeker.Register(Guid.NewGuid(), "Backcompat User", clock).Value;
        db.JobSeekers.Add(seeker);
        await db.SaveChangesAsync(ct);
        return seeker;
    }

    // Skriver en saved_searches-rad direkt med RÅ jsonb (kringgår EF-mappningen)
    // för att simulera en redan-persistent gammal skalär-form-rad.
    private async Task<Guid> InsertRawSavedSearchAsync(
        Guid jobSeekerId, string criteriaJson, CancellationToken ct)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        await conn.OpenAsync(ct);
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO saved_searches
              (id, job_seeker_id, name, criteria, notification_enabled,
               created_at, updated_at)
            VALUES
              (@id, @sid, @name, @criteria::jsonb, false, @now, @now)
            """;
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("sid", jobSeekerId);
        cmd.Parameters.AddWithValue("name", "Gammal skalär-rad");
        cmd.Parameters.AddWithValue("criteria", criteriaJson);
        cmd.Parameters.AddWithValue("now", now);
        await cmd.ExecuteNonQueryAsync(ct);
        await conn.CloseAsync();
        return id;
    }

    // (a) Gammal skalär-form läses som ett-element-lista.
    [Fact]
    public async Task OldScalarForm_ReadsAsSingleElementList()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var seeker = await SeedSeekerAsync(db, clock, ct);

        // Gammal on-disk-form: Ssyk/Region som skalär sträng.
        var oldJson = """{"Ssyk":"x","Region":"y","Q":null,"SortBy":0}""";
        var id = await InsertRawSavedSearchAsync(seeker.Id.Value, oldJson, ct);

        using var readScope = _factory.Services.CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await readDb.SavedSearches
            .SingleAsync(s => s.Id == new SavedSearchId(id), ct);

        saved.Criteria.Ssyk.ShouldBe(["x"]);
        saved.Criteria.Region.ShouldBe(["y"]);
        saved.Criteria.Q.ShouldBeNull();
        saved.Criteria.SortBy.ShouldBe(JobAdSortBy.PublishedAtDesc);
    }

    // (b) Ny array-form roundtrip (skriv via EF → läs via EF).
    [Fact]
    public async Task NewArrayForm_RoundTripsThroughEf()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var seeker = await SeedSeekerAsync(db, clock, ct);

        var criteria = SearchCriteria.Create(
            ["sysdev", "frontend"], ["stockholm", "uppsala"], "backend",
            JobAdSortBy.PublishedAtDesc).Value;
        var saved = SavedSearch.Create(seeker.Id, "Ny array-rad", criteria, false, clock).Value;
        db.SavedSearches.Add(saved);
        await db.SaveChangesAsync(ct);

        using var readScope = _factory.Services.CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloaded = await readDb.SavedSearches
            .SingleAsync(s => s.Id == saved.Id, ct);

        reloaded.Criteria.Ssyk.ShouldBe(["frontend", "sysdev"]); // sorterad ordinal
        reloaded.Criteria.Region.ShouldBe(["stockholm", "uppsala"]);
        reloaded.Criteria.Q.ShouldBe("backend");
        reloaded.Criteria.ShouldBe(criteria); // strukturell equality bevarad
    }

    // (c) Default-deny: felaktiga former koerceras EJ tyst — de kastar.
    [Theory]
    [InlineData("""{"Ssyk":42,"Region":[],"Q":null,"SortBy":0}""")]            // nummer
    [InlineData("""{"Ssyk":{"k":"v"},"Region":[],"Q":null,"SortBy":0}""")]     // objekt
    [InlineData("""{"Ssyk":["ok",123],"Region":[],"Q":null,"SortBy":0}""")]    // array m. icke-sträng
    [InlineData("""{"Ssyk":["ok",null],"Region":[],"Q":null,"SortBy":0}""")]   // null-element
    public async Task MalformedForm_DefaultDeny_Throws(string malformedJson)
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        var seeker = await SeedSeekerAsync(db, clock, ct);
        var id = await InsertRawSavedSearchAsync(seeker.Id.Value, malformedJson, ct);

        using var readScope = _factory.Services.CreateScope();
        var readDb = readScope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Tolerant default-deny: tyst koercion förbjuden. Materialisering ska
        // kasta (JsonException/DomainException/InvalidOperationException-wrap).
        await Should.ThrowAsync<Exception>(async () =>
            await readDb.SavedSearches.SingleAsync(s => s.Id == new SavedSearchId(id), ct));
    }
}
