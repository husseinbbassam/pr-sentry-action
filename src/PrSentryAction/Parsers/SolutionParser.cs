using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using PrSentryAction.Models;

namespace PrSentryAction.Parsers;

/// <summary>
/// Parses a .NET solution (.sln / .slnx) or individual .csproj file and builds
/// the full dependency graph as a list of <see cref="ProjectInfo"/> objects.
/// </summary>
public sealed class SolutionParser(ILogger<SolutionParser> logger)
{
    private static readonly HashSet<string> DomainKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "Domain" };

    private static readonly HashSet<string> ApplicationKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "Application", "App" };

    private static readonly HashSet<string> InfrastructureKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "Infrastructure", "Infra" };

    private static readonly HashSet<string> DataKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "Data", "Persistence", "Repository", "Repositories", "Dal", "Db", "Database" };

    private static readonly HashSet<string> WebApiKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "Web", "API", "Api", "Host", "Presentation", "Server", "Mvc", "Grpc" };

    /// <summary>
    /// Parses the given solution or project file path and returns the discovered projects.
    /// </summary>
    /// <param name="path">Absolute path to a .sln, .slnx, or .csproj file.</param>
    public IReadOnlyList<ProjectInfo> Parse(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Solution/project file not found: {path}", path);

        var ext = Path.GetExtension(path).ToLowerInvariant();

        var csprojPaths = ext switch
        {
            ".sln" => ParseSlnFile(path),
            ".slnx" => ParseSlnxFile(path),
            ".csproj" => [path],
            _ => throw new NotSupportedException($"Unsupported file type: {ext}. Expected .sln, .slnx, or .csproj.")
        };

        var projects = new List<ProjectInfo>();
        foreach (var csprojPath in csprojPaths)
        {
            if (!File.Exists(csprojPath))
            {
                logger.LogWarning("Project file not found, skipping: {Path}", csprojPath);
                continue;
            }

            var projectInfo = ParseCsproj(csprojPath);
            projects.Add(projectInfo);
            logger.LogDebug("Parsed project: {Name} ({Layer}) with {RefCount} references",
                projectInfo.Name, projectInfo.Layer, projectInfo.ProjectReferences.Count);
        }

        return projects.AsReadOnly();
    }

    private static IEnumerable<string> ParseSlnFile(string slnPath)
    {
        var slnDir = Path.GetDirectoryName(slnPath)!;
        var lines = File.ReadAllLines(slnPath);

        foreach (var line in lines)
        {
            // Lines look like: Project("{FAE04EC0-...}") = "MyProject", "src\MyProject\MyProject.csproj", "{GUID}"
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith("Project(", StringComparison.Ordinal))
                continue;

            var parts = trimmed.Split(',');
            if (parts.Length < 2)
                continue;

            var relativePath = parts[1].Trim().Trim('"');
            if (!relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return Path.GetFullPath(Path.Combine(slnDir, relativePath));
        }
    }

    private static IEnumerable<string> ParseSlnxFile(string slnxPath)
    {
        var slnDir = Path.GetDirectoryName(slnxPath)!;
        var doc = XDocument.Load(slnxPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        // <Project Path="src/PrSentryAction/PrSentryAction.csproj" />
        foreach (var projectEl in doc.Descendants(ns + "Project"))
        {
            var relativePath = projectEl.Attribute("Path")?.Value;
            if (string.IsNullOrWhiteSpace(relativePath))
                continue;
            if (!relativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return Path.GetFullPath(Path.Combine(slnDir, relativePath));
        }
    }

    private static ProjectInfo ParseCsproj(string csprojPath)
    {
        var name = Path.GetFileNameWithoutExtension(csprojPath);
        var doc = XDocument.Load(csprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        var references = doc
            .Descendants(ns + "ProjectReference")
            .Select(el => el.Attribute("Include")?.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => Path.GetFileNameWithoutExtension(v!.Replace('\\', '/')))
            .ToList();

        var layer = DetectLayer(name);

        return new ProjectInfo
        {
            Name = name,
            FilePath = Path.GetFullPath(csprojPath),
            ProjectReferences = references.AsReadOnly(),
            Layer = layer
        };
    }

    /// <summary>
    /// Determines the Clean Architecture layer of a project based on its name segments.
    /// </summary>
    public static ArchitectureLayer DetectLayer(string projectName)
    {
        // Split on dots and other common separators to check each segment
        var segments = projectName.Split('.', '_', '-');

        foreach (var segment in segments)
        {
            if (WebApiKeywords.Contains(segment)) return ArchitectureLayer.WebApi;
            if (DataKeywords.Contains(segment)) return ArchitectureLayer.Data;
            if (InfrastructureKeywords.Contains(segment)) return ArchitectureLayer.Infrastructure;
            if (ApplicationKeywords.Contains(segment)) return ArchitectureLayer.Application;
            if (DomainKeywords.Contains(segment)) return ArchitectureLayer.Domain;
        }

        return ArchitectureLayer.Unknown;
    }
}
