using NetArchTest.Rules;
using Shouldly;

namespace JobbPilot.Architecture.Tests;

public class DomainLayerTests
{
    [Fact]
    public void Domain_should_not_depend_on_any_other_project()
    {
        var result = Types.InAssembly(typeof(JobbPilot.Domain.Common.Entity<>).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Mediator",
                "FluentValidation",
                "JobbPilot.Application",
                "JobbPilot.Infrastructure",
                "JobbPilot.Api",
                "JobbPilot.Worker")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Domain läcker: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_should_not_depend_on_Infrastructure_or_EFCore()
    {
        var result = Types.InAssembly(typeof(JobbPilot.Application.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "JobbPilot.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            $"Application läcker: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_should_not_depend_on_Api_or_Worker()
    {
        var result = Types.InAssembly(typeof(JobbPilot.Infrastructure.AssemblyMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "JobbPilot.Api",
                "JobbPilot.Worker")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Domain_aggregates_should_only_have_private_setters()
    {
        var aggregateTypes = Types.InAssembly(typeof(JobbPilot.Domain.Common.AggregateRoot<>).Assembly)
            .That()
            .Inherit(typeof(JobbPilot.Domain.Common.AggregateRoot<>))
            .GetTypes();

        foreach (var type in aggregateTypes)
        {
            var publicSetters = type.GetProperties()
                .Where(p => p.SetMethod is { IsPublic: true })
                .Select(p => $"{type.Name}.{p.Name}")
                .ToList();

            publicSetters.ShouldBeEmpty(
                $"Aggregate {type.Name} har public setters: {string.Join(", ", publicSetters)}");
        }
    }
}
