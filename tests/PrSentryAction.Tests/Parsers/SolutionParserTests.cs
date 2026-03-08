using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PrSentryAction.Models;
using PrSentryAction.Parsers;

namespace PrSentryAction.Tests.Parsers;

public sealed class SolutionParserLayerDetectionTests
{
    [Theory]
    [InlineData("MyApp.Domain", ArchitectureLayer.Domain)]
    [InlineData("Company.Product.Domain", ArchitectureLayer.Domain)]
    [InlineData("MyApp.Application", ArchitectureLayer.Application)]
    [InlineData("MyApp.App", ArchitectureLayer.Application)]
    [InlineData("Company.Product.Application", ArchitectureLayer.Application)]
    [InlineData("MyApp.Infrastructure", ArchitectureLayer.Infrastructure)]
    [InlineData("MyApp.Infra", ArchitectureLayer.Infrastructure)]
    [InlineData("MyApp.Data", ArchitectureLayer.Data)]
    [InlineData("MyApp.Persistence", ArchitectureLayer.Data)]
    [InlineData("MyApp.Repository", ArchitectureLayer.Data)]
    [InlineData("MyApp.Dal", ArchitectureLayer.Data)]
    [InlineData("MyApp.Web", ArchitectureLayer.WebApi)]
    [InlineData("MyApp.API", ArchitectureLayer.WebApi)]
    [InlineData("MyApp.Api", ArchitectureLayer.WebApi)]
    [InlineData("MyApp.Host", ArchitectureLayer.WebApi)]
    [InlineData("MyApp.Presentation", ArchitectureLayer.WebApi)]
    [InlineData("MyApp.Server", ArchitectureLayer.WebApi)]
    [InlineData("UnknownProject", ArchitectureLayer.Unknown)]
    [InlineData("SomeLibrary", ArchitectureLayer.Unknown)]
    public void DetectLayer_ReturnsExpectedLayer(string projectName, ArchitectureLayer expected)
    {
        var result = SolutionParser.DetectLayer(projectName);
        result.Should().Be(expected);
    }
}

public sealed class SolutionParserCsprojTests
{
    private string _tempDir = string.Empty;

    private string SetupTempDir()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"pr-sentry-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        return _tempDir;
    }

    private void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Parse_CsprojWithNoReferences_ReturnsProjectWithEmptyDependencies()
    {
        var dir = SetupTempDir();
        try
        {
            var csprojContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                  </PropertyGroup>
                </Project>
                """;
            var csprojPath = Path.Combine(dir, "MyApp.Domain.csproj");
            File.WriteAllText(csprojPath, csprojContent);

            var logger = new NullLogger<SolutionParser>();
            var parser = new SolutionParser(logger);

            var projects = parser.Parse(csprojPath);

            projects.Should().HaveCount(1);
            projects[0].Name.Should().Be("MyApp.Domain");
            projects[0].Layer.Should().Be(ArchitectureLayer.Domain);
            projects[0].ProjectReferences.Should().BeEmpty();
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Parse_CsprojWithProjectReferences_ReturnsProjectWithDependencies()
    {
        var dir = SetupTempDir();
        try
        {
            var csprojContent = """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net9.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\MyApp.Domain\MyApp.Domain.csproj" />
                  </ItemGroup>
                </Project>
                """;
            var csprojPath = Path.Combine(dir, "MyApp.Application.csproj");
            File.WriteAllText(csprojPath, csprojContent);

            var logger = new NullLogger<SolutionParser>();
            var parser = new SolutionParser(logger);

            var projects = parser.Parse(csprojPath);

            projects.Should().HaveCount(1);
            projects[0].Name.Should().Be("MyApp.Application");
            projects[0].Layer.Should().Be(ArchitectureLayer.Application);
            projects[0].ProjectReferences.Should().ContainSingle().Which.Should().Be("MyApp.Domain");
        }
        finally { Cleanup(); }
    }

    [Fact]
    public void Parse_NonExistentFile_ThrowsFileNotFoundException()
    {
        var logger = new NullLogger<SolutionParser>();
        var parser = new SolutionParser(logger);

        var act = () => parser.Parse("/nonexistent/path/MyApp.Domain.csproj");

        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Parse_UnsupportedFileType_ThrowsNotSupportedException()
    {
        var dir = SetupTempDir();
        try
        {
            var filePath = Path.Combine(dir, "MyApp.vbproj");
            File.WriteAllText(filePath, "<Project/>");

            var logger = new NullLogger<SolutionParser>();
            var parser = new SolutionParser(logger);

            var act = () => parser.Parse(filePath);

            act.Should().Throw<NotSupportedException>();
        }
        finally { Cleanup(); }
    }
}
