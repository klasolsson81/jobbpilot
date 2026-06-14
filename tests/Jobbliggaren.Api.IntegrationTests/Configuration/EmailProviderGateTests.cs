using Jobbliggaren.Application.Common.Abstractions;
using Jobbliggaren.Infrastructure;
using Jobbliggaren.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;

namespace Jobbliggaren.Api.IntegrationTests.Configuration;

/// <summary>
/// TD-104 / STEG 6 (security-auditor Major #1): <see cref="ConsoleEmailSender"/> logs the
/// recipient email + plaintext invitation token via ILogger. Once the persistent Seq sink is
/// attached (this STEG), that becomes durable PII + a live credential — so it must register
/// ONLY in Development/Test. Other environments fall back to the no-op
/// <see cref="NullEmailSender"/> until a real transactional provider lands (TD-101).
/// Pure DI-registration inspection — no host boot / Testcontainers needed.
/// </summary>
public class EmailProviderGateTests
{
    private static Type ResolveEmailSenderImpl(string environmentName, string? provider = null)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(environmentName);

        var values = new Dictionary<string, string?>();
        if (provider is not null)
            values[$"{EmailOptions.SectionName}:Provider"] = provider;
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var services = new ServiceCollection();
        services.AddInvitationsAndEmail(config, env);

        return services.Single(d => d.ServiceType == typeof(IEmailSender)).ImplementationType!;
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Test")]
    public void ConsoleProvider_InDevelopmentOrTest_RegistersConsoleEmailSender(string env) =>
        ResolveEmailSenderImpl(env).ShouldBe(typeof(ConsoleEmailSender));

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void ConsoleProvider_OutsideDevelopmentOrTest_FallsBackToNullEmailSender(string env) =>
        ResolveEmailSenderImpl(env).ShouldBe(typeof(NullEmailSender));

    [Fact]
    public void DefaultProvider_InProduction_DoesNotRegisterConsoleEmailSender() =>
        ResolveEmailSenderImpl("Production", provider: null).ShouldBe(typeof(NullEmailSender));

    [Fact]
    public void UnknownProvider_FailsLoud() =>
        Should.Throw<InvalidOperationException>(() => ResolveEmailSenderImpl("Development", "Smtp"));
}
