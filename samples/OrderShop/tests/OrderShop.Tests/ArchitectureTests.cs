using System.Reflection;
using NetArchTest.Rules;

namespace OrderShop.Tests;

// These tests turn the AGENTS.md rules into failing tests, so a violation cannot be merged.
public class ArchitectureTests
{
    static readonly Assembly Api = typeof(Program).Assembly;
    static readonly Assembly TestAssembly = typeof(ArchitectureTests).Assembly;
    const string FeaturesNamespace = "OrderShop.Api.Features";

    // Top-level classes in Features.*; nested records like Request/Response are part of their feature.
    static List<Type> FeatureClasses() =>
        Types.InAssembly(Api)
            .That().ResideInNamespaceStartingWith(FeaturesNamespace)
            .And().AreNotNested()
            .GetTypes()
            .ToList();

    [Fact]
    public void No_type_is_a_service_or_repository() // R13
    {
        var result = Types.InAssembly(Api)
            .Should().NotHaveNameEndingWith("Service")
            .And().NotHaveNameEndingWith("Repository")
            .GetResult();

        Assert.True(result.IsSuccessful, "Service/repository types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Api_declares_no_interfaces() // R13
    {
        var result = Types.InAssembly(Api)
            .Should().NotBeInterfaces()
            .GetResult();

        Assert.True(result.IsSuccessful, "Interfaces: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Every_feature_class_is_static() // R1, R15
    {
        var result = Types.InAssembly(Api)
            .That().ResideInNamespaceStartingWith(FeaturesNamespace)
            .And().AreNotNested()
            .Should().BeStatic()
            .GetResult();

        Assert.True(result.IsSuccessful, "Not static: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Features_do_not_depend_on_other_features() // R2
    {
        var features = FeatureClasses();
        Assert.NotEmpty(features);

        var offenders = new List<string>();
        foreach (var feature in features)
        {
            var users = Types.InAssembly(Api)
                .That().ResideInNamespaceStartingWith(FeaturesNamespace)
                .And().HaveDependencyOn(feature.FullName!)
                .GetTypes();

            // A feature may use itself and its own nested types; any other feature is a violation.
            offenders.AddRange(users
                .Where(user => user != feature && user.DeclaringType != feature)
                .Select(user => $"{user.FullName} -> {feature.FullName}"));
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void No_hidden_dispatch_libraries() // R14
    {
        var banned = new[] { "MediatR", "AutoMapper", "FluentValidation" };

        var result = Types.InAssembly(Api)
            .Should().NotHaveDependencyOnAny(banned)
            .GetResult();

        var referenced = Api.GetReferencedAssemblies()
            .Select(a => a.Name ?? "")
            .Where(name => banned.Any(b => name.StartsWith(b, StringComparison.OrdinalIgnoreCase)));

        Assert.True(result.IsSuccessful, "Uses banned library: " + string.Join(", ", result.FailingTypeNames ?? []));
        Assert.Empty(referenced);
    }

    [Fact]
    public void Every_feature_has_a_matching_test_class() // R16
    {
        var testClassNames = TestAssembly.GetTypes().Select(t => t.Name).ToHashSet();

        var missing = FeatureClasses()
            .Where(feature => !testClassNames.Contains(feature.Name + "Tests"))
            .Select(feature => feature.FullName);

        Assert.Empty(missing);
    }
}
