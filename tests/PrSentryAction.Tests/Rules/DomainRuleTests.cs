using FluentAssertions;
using PrSentryAction.Models;
using PrSentryAction.Rules;

namespace PrSentryAction.Tests.Rules;

public sealed class DomainRuleTests
{
    private readonly DomainRule _rule = new();

    [Fact]
    public void Evaluate_DomainWithNoReferences_ReturnsNoViolations()
    {
        var projects = new List<ProjectInfo>
        {
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_DomainReferencingInternalProject_ReturnsViolation()
    {
        var projects = new List<ProjectInfo>
        {
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = ["MyApp.Application"] },
            new() { Name = "MyApp.Application", Layer = ArchitectureLayer.Application, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().HaveCount(1);
        violations[0].ProjectName.Should().Be("MyApp.Domain");
        violations[0].Description.Should().Contain("MyApp.Application");
        violations[0].Severity.Should().Be(ViolationSeverity.Error);
    }

    [Fact]
    public void Evaluate_DomainReferencingMultipleInternalProjects_ReturnsOneViolationPerReference()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "MyApp.Domain",
                Layer = ArchitectureLayer.Domain,
                ProjectReferences = ["MyApp.Application", "MyApp.Infrastructure"]
            },
            new() { Name = "MyApp.Application", Layer = ArchitectureLayer.Application, ProjectReferences = [] },
            new() { Name = "MyApp.Infrastructure", Layer = ArchitectureLayer.Infrastructure, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().HaveCount(2);
        violations.Should().AllSatisfy(v => v.RuleName.Should().Be("Domain Isolation"));
    }

    [Fact]
    public void Evaluate_NoDomainProjects_ReturnsNoViolations()
    {
        var projects = new List<ProjectInfo>
        {
            new() { Name = "MyApp.Application", Layer = ArchitectureLayer.Application, ProjectReferences = ["MyApp.Domain"] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_DomainReferencingExternalNuGetPackage_ReturnsNoViolation()
    {
        // External NuGet packages are not in the project list, so they should be ignored
        var projects = new List<ProjectInfo>
        {
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = ["SomeExternalLib"] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().BeEmpty();
    }
}
