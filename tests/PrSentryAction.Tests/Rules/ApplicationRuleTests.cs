using FluentAssertions;
using PrSentryAction.Models;
using PrSentryAction.Rules;

namespace PrSentryAction.Tests.Rules;

public sealed class ApplicationRuleTests
{
    private readonly ApplicationRule _rule = new();

    [Fact]
    public void Evaluate_ApplicationReferencingOnlyDomain_ReturnsNoViolations()
    {
        var projects = new List<ProjectInfo>
        {
            new() { Name = "MyApp.Application", Layer = ArchitectureLayer.Application, ProjectReferences = ["MyApp.Domain"] },
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ApplicationReferencingInfrastructure_ReturnsViolation()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "MyApp.Application",
                Layer = ArchitectureLayer.Application,
                ProjectReferences = ["MyApp.Domain", "MyApp.Infrastructure"]
            },
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] },
            new() { Name = "MyApp.Infrastructure", Layer = ArchitectureLayer.Infrastructure, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().HaveCount(1);
        violations[0].ProjectName.Should().Be("MyApp.Application");
        violations[0].Description.Should().Contain("MyApp.Infrastructure");
        violations[0].Severity.Should().Be(ViolationSeverity.Error);
    }

    [Fact]
    public void Evaluate_ApplicationReferencingDataProject_ReturnsViolation()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "MyApp.Application",
                Layer = ArchitectureLayer.Application,
                ProjectReferences = ["MyApp.Data"]
            },
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] },
            new() { Name = "MyApp.Data", Layer = ArchitectureLayer.Data, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().HaveCount(1);
        violations[0].Description.Should().Contain("MyApp.Data");
    }

    [Fact]
    public void Evaluate_ApplicationReferencingWebApi_ReturnsViolation()
    {
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "MyApp.Application",
                Layer = ArchitectureLayer.Application,
                ProjectReferences = ["MyApp.Web"]
            },
            new() { Name = "MyApp.Web", Layer = ArchitectureLayer.WebApi, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().HaveCount(1);
    }

    [Fact]
    public void Evaluate_NoApplicationProjects_ReturnsNoViolations()
    {
        var projects = new List<ProjectInfo>
        {
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ApplicationReferencingUnknownProject_ReturnsNoViolation()
    {
        // Unknown (not in projects list) references should be ignored
        var projects = new List<ProjectInfo>
        {
            new()
            {
                Name = "MyApp.Application",
                Layer = ArchitectureLayer.Application,
                ProjectReferences = ["MyApp.Domain", "SomeThirdPartyLib"]
            },
            new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] }
        };

        var violations = _rule.Evaluate(projects).ToList();

        violations.Should().BeEmpty();
    }
}
