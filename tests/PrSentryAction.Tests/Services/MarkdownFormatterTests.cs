using FluentAssertions;
using PrSentryAction.Models;
using PrSentryAction.Services;

namespace PrSentryAction.Tests.Services;

public sealed class MarkdownFormatterTests
{
    private readonly MarkdownFormatter _formatter = new();

    [Fact]
    public void Format_NoViolations_ContainsSuccessMessage()
    {
        var result = new AnalysisResult
        {
            AnalyzedPath = "/repo/MyApp.sln",
            Projects =
            [
                new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] },
                new() { Name = "MyApp.Application", Layer = ArchitectureLayer.Application, ProjectReferences = ["MyApp.Domain"] }
            ],
            Violations = []
        };

        var markdown = _formatter.Format(result);

        markdown.Should().Contain("No architectural violations found");
        markdown.Should().Contain("PR Sentry");
        markdown.Should().Contain("MyApp.Domain");
        markdown.Should().Contain("MyApp.Application");
    }

    [Fact]
    public void Format_WithViolations_ContainsViolationDetails()
    {
        var result = new AnalysisResult
        {
            AnalyzedPath = "/repo/MyApp.sln",
            Projects =
            [
                new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = ["MyApp.Application"] }
            ],
            Violations =
            [
                new()
                {
                    RuleName = "Domain Isolation",
                    ProjectName = "MyApp.Domain",
                    Description = "Domain project has forbidden dependency.",
                    Severity = ViolationSeverity.Error
                }
            ]
        };

        var markdown = _formatter.Format(result);

        markdown.Should().Contain("violation(s) detected");
        markdown.Should().Contain("Domain Isolation");
        markdown.Should().Contain("MyApp.Domain");
        markdown.Should().Contain("Domain project has forbidden dependency.");
    }

    [Fact]
    public void Format_OutputContainsHeader()
    {
        var result = new AnalysisResult
        {
            AnalyzedPath = "/repo/MyApp.sln",
            Projects = [],
            Violations = []
        };

        var markdown = _formatter.Format(result);

        markdown.Should().Contain("PR Sentry");
        markdown.Should().Contain("Clean Architecture Analysis");
    }

    [Fact]
    public void Format_ProjectTable_ContainsAllProjects()
    {
        var result = new AnalysisResult
        {
            AnalyzedPath = "/repo/MyApp.sln",
            Projects =
            [
                new() { Name = "MyApp.Domain", Layer = ArchitectureLayer.Domain, ProjectReferences = [] },
                new() { Name = "MyApp.Infrastructure", Layer = ArchitectureLayer.Infrastructure, ProjectReferences = [] },
                new() { Name = "MyApp.Web", Layer = ArchitectureLayer.WebApi, ProjectReferences = [] }
            ],
            Violations = []
        };

        var markdown = _formatter.Format(result);

        markdown.Should().Contain("MyApp.Domain");
        markdown.Should().Contain("MyApp.Infrastructure");
        markdown.Should().Contain("MyApp.Web");
    }
}
