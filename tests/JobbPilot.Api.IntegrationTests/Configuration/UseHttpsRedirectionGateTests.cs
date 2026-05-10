using System.Net;
using System.Security.Cryptography;
using JobbPilot.Infrastructure.Identity;
using JobbPilot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace JobbPilot.Api.IntegrationTests.Configuration;

/// <summary>
/// Anti-regression för UseHttpsRedirection env-gate per ADR 0026 + ADR 0027 (TD-31).
///
/// Sec-Major-2 STEG 13b: om gaten tas bort skulle redirect → port 443 mot HTTP-only-ALB
/// trigga ALB-health-check fail → ECS deployment_circuit_breaker rollback. Strukturellt
/// regression-skydd, inte docs-disciplin. Tre testfall:
///
/// 1. Production + Alb:HttpsEnabled=false → 200 (UseHttpsRedirection EJ registrerad)
/// 2. Production + Alb:HttpsEnabled=true  → 307 (UseHttpsRedirection registrerad)
/// 3. Development (oavsett Alb-flag)      → 307 (Development gör default-redirect via dev-cert)
///
/// Fall 3 verifierar Program.cs:155 första halvan (<c>builder.Environment.IsDevelopment()</c>).
/// Återanvänder befintlig <see cref="ApiFactory"/> via <c>[Collection("Api")]</c>.
/// </summary>
public abstract class HttpsRedirectionGateFactoryBase : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();
    private readonly RedisContainer _redis = new RedisBuilder("redis:8-alpine").Build();

    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;

    private string _postgresCs = string.Empty;
    private string _redisCs = string.Empty;

    /// <summary>Värdet som sätts på <c>Alb__HttpsEnabled</c> env-var i InitializeAsync.</summary>
    protected abstract bool HttpsEnabled { get; }

    /// <summary>ASP.NET environment-name. Override:s av Development-factory; default Production.</summary>
    protected virtual string EnvironmentName => "Production";

    protected HttpsRedirectionGateFactoryBase()
    {
        var rsa = RSA.Create(2048);
        _privateKeyPath = Path.Combine(Path.GetTempPath(), $"jobbpilot-httpsgate-private-{Guid.NewGuid()}.pem");
        _publicKeyPath = Path.Combine(Path.GetTempPath(), $"jobbpilot-httpsgate-public-{Guid.NewGuid()}.pem");
        File.WriteAllText(_privateKeyPath, rsa.ExportRSAPrivateKeyPem());
        File.WriteAllText(_publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // ASPNETCORE_ENVIRONMENT sätts i InitializeAsync FÖRE host-build (TD-37-
        // läxa: builder.UseEnvironment() är no-op för minimal API där
        // WebApplication.CreateBuilder() läser env-varen direkt vid host-bygge,
        // innan denna ConfigureWebHost-callback körs).
        builder.ConfigureServices(services =>
        {
            // UseHttpsRedirection-middleware behöver veta vilken port att redirecta TILL.
            // Default-resolver kollar ASPNETCORE_URLS / ASPNETCORE_HTTPS_PORTS / HTTPS_PORT —
            // ingen är satt i WebApplicationFactory-test-host → middleware loggar varning
            // "Failed to determine the https port for redirect" och returnerar 200 utan
            // redirect. Sätt explicit 443 så testet kan asserta 307 + Location-scheme.
            services.PostConfigure<HttpsRedirectionOptions>(opts => opts.HttpsPort = 443);

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
                options
                    .UseNpgsql(_postgresCs,
                        npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                    .UseSnakeCaseNamingConvention());

            services.RemoveAll<DbContextOptions<AppIdentityDbContext>>();
            services.RemoveAll<AppIdentityDbContext>();
            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseNpgsql(_postgresCs, npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(AppIdentityDbContext).Assembly.FullName);
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                }));

            services.RemoveAll<IDistributedCache>();
            services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = _redisCs;
                opts.InstanceName = "jobbpilot:";
            });
        });
    }

    public async ValueTask InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _redis.StartAsync());

        _postgresCs = _postgres.GetConnectionString();
        _redisCs = _redis.GetConnectionString();

        // Env-vars sätts FÖRE Services-access (triggar host-build). Production-env
        // kräver populerad ConnectionStrings + KnownNetworks (per ForwardedHeadersConfig.
        // EnsureSafeForEnvironment) + IConnectionMultiplexer-string (TD-37-läxa).
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", EnvironmentName);
        Environment.SetEnvironmentVariable("Jwt__PrivateKeyPath", _privateKeyPath);
        Environment.SetEnvironmentVariable("Jwt__PublicKeyPath", _publicKeyPath);
        Environment.SetEnvironmentVariable("ForwardedHeaders__KnownNetworks__0", "127.0.0.1/32");
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgresCs);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", _redisCs);
        Environment.SetEnvironmentVariable("Alb__HttpsEnabled", HttpsEnabled ? "true" : "false");

        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>().Database.MigrateAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
        Environment.SetEnvironmentVariable("Jwt__PrivateKeyPath", null);
        Environment.SetEnvironmentVariable("Jwt__PublicKeyPath", null);
        Environment.SetEnvironmentVariable("ForwardedHeaders__KnownNetworks__0", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", null);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", null);
        Environment.SetEnvironmentVariable("Alb__HttpsEnabled", null);

        if (File.Exists(_privateKeyPath)) File.Delete(_privateKeyPath);
        if (File.Exists(_publicKeyPath)) File.Delete(_publicKeyPath);

        // SuppressFinalize FÖRE base.DisposeAsync (idempotent, men rätt ordning enligt
        // CA1816 — undviker dubbel-anrop då base själv anropar SuppressFinalize internt).
        GC.SuppressFinalize(this);

        await Task.WhenAll(_postgres.StopAsync(), _redis.StopAsync());
        await base.DisposeAsync();
    }
}

public sealed class HttpsRedirectionDisabledProductionFactory : HttpsRedirectionGateFactoryBase
{
    protected override bool HttpsEnabled => false;
}

public sealed class HttpsRedirectionEnabledProductionFactory : HttpsRedirectionGateFactoryBase
{
    protected override bool HttpsEnabled => true;
}

public sealed class HttpsRedirectionDevelopmentFactory : HttpsRedirectionGateFactoryBase
{
    // Development triggar UseHttpsRedirection oavsett Alb-flag (Program.cs:155
    // första halvan: builder.Environment.IsDevelopment()).
    protected override bool HttpsEnabled => false;
    protected override string EnvironmentName => "Development";
}

[CollectionDefinition("HttpsRedirectionDisabled")]
public sealed class HttpsRedirectionDisabledFixtureGroup
    : ICollectionFixture<HttpsRedirectionDisabledProductionFactory>;

[CollectionDefinition("HttpsRedirectionEnabled")]
public sealed class HttpsRedirectionEnabledFixtureGroup
    : ICollectionFixture<HttpsRedirectionEnabledProductionFactory>;

[CollectionDefinition("HttpsRedirectionDevelopment")]
public sealed class HttpsRedirectionDevelopmentFixtureGroup
    : ICollectionFixture<HttpsRedirectionDevelopmentFactory>;

[Collection("HttpsRedirectionDisabled")]
public class HttpsRedirectionGateDisabledTests(HttpsRedirectionDisabledProductionFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    [Fact]
    public async Task GET_api_ready_returns_200_when_HttpsEnabled_false_in_Production()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync("/api/ready", ct);

        // Med Alb:HttpsEnabled=false (default fram till ADR 0026-trigger) registreras
        // INTE UseHttpsRedirection — HTTP-request returnerar direkt utan redirect.
        // Skydd mot regression där gaten tas bort och redirect → port 443 mot HTTP-only-
        // ALB → ALB-health-check fail → ECS deployment_circuit_breaker rollback.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

[Collection("HttpsRedirectionEnabled")]
public class HttpsRedirectionGateEnabledTests(HttpsRedirectionEnabledProductionFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    [Fact]
    public async Task GET_api_ready_returns_307_when_HttpsEnabled_true_in_Production()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync("/api/ready", ct);

        // Med Alb:HttpsEnabled=true (post-STEG 13c HTTPS-flip) registreras
        // UseHttpsRedirection — HTTP-request returnerar 307 mot https://.
        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.Scheme.ShouldBe("https");
    }
}

[Collection("HttpsRedirectionDevelopment")]
public class HttpsRedirectionGateDevelopmentTests(HttpsRedirectionDevelopmentFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    [Fact]
    public async Task GET_api_ready_returns_307_in_Development_regardless_of_Alb_flag()
    {
        var ct = TestContext.Current.CancellationToken;

        var response = await _client.GetAsync("/api/ready", ct);

        // Program.cs:155 — Development triggar UseHttpsRedirection oavsett Alb-flag
        // (dev-cert via Kestrel). Skydd mot regression där env-gate omdesignas så
        // Development-läge tappar redirect.
        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location!.Scheme.ShouldBe("https");
    }
}
