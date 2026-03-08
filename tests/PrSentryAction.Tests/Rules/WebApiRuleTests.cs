using FluentAssertions;
using PrSentryAction.Models;
using PrSentryAction.Rules;

namespace PrSentryAction.Tests.Rules;

public sealed class WebApiRuleTests
{
    private readonly WebApiRule _rule = new();

    [Fact]
    public void Evaluate_WebApiReferencingApplicationAndDomain_ReturnsNoViolations()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "MyApp.Web",
                Layer = ArchitectureLayer.WebApi,
                ProjectReferences = ["MyApp.Application", "MyApp.Domain"]
            },
            new() { Name = "MyApp.Application", Layer = ArchitectureLayer.Application, ProjectReferences = [] },
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_WebApiReferencingInfrastructure_ReturnsViolation()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "MyApp.Web",
                Layer = ArchitectureLayer.WebApi,
                ProjectReferences = ["MyApp.Application", "MyApp.Infrastructure"]
            },
            new() { Name = "MyApp.Application", Layer = ArchitectureLayer.Application, ProjectReferences = [] },
            new() { Name = "MyApp.Infrastructure", Layer = ArchitectureLayer.Infrastructure, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().HaveCount(1);
        violations[0].ProjectName.Should().Be("MyApp.Web");
        violations[0].Description.Should().Contain("MyApp.Infrastructure");
        violations[0].Severity.Should().Be(ViolationSeverity.Error);
    }

    [Fact]
    public void Evaluate_WebApiReferencingDataProject_ReturnsViolation()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "MyApp.API",
                Layer = ArchitectureLayer.WebApi,
                ProjectReferences = ["MyApp.Data"]
            },
            new() { Name = "MyApp.Data", Layer = ArchitectureLayer.Data, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().HaveCount(1);
        violations[0].ProjectName.Should().Be("MyApp.API");
        violations[0].Description.Should().Contain("MyApp.Data");
    }

    [Fact]
    public void Evaluate_WebApiReferencingBothInfrastructureAndData_ReturnsTwoViolations()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "MyApp.Web",
                Layer = ArchitectureLayer.WebApi,
                ProjectReferences = ["MyApp.Infrastructure", "MyApp.Data"]
            },
            new() { Name = "MyApp.Infrastructure", Layer = ArchitectureLayer.Infrastructure, ProjectReferences = [] },
            new() { Name = "MyApp.Data", Layer = ArchitectureLayer.Data, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().HaveCount(2);
        violations.Should().AllSatisfy(v => v.RuleName.Should().Be("Web/API Layer Boundary"));
    }

    [Fact]
    public void Evaluate_NoWebApiProjects_ReturnsNoViolations()
    {
        var projects = new List<ProjectInfo>
        {
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] },
            new() { Name = "MyApp.Application", Layer = ArchitectureLayer.Application, ProjectReferences = ["MyApp.Domain"] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().BeEmpty();
    }
}
