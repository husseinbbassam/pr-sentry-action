using Microsoft.Extensions.Logging;
using PrSentryAction.Models;
using PrSentryAction.Parsers;
using PrSentryAction.Rules;

namespace PrSentryAction.Services;

/// <summary>
/// Orchestrates the full architectural analysis: parses projects, runs all rules,
/// and returns a consolidated <see cref="AnalysisResult"/>.
/// </summary>
public sealed class ArchitectureAnalyzer(
    SolutionParser parser,
    IEnumerable<IArchitectureRule> rules,
    ILogger<ArchitectureAnalyzer> logger)
{
    /// <summary>
    /// Analyzes the given solution or project file for Clean Architecture violations.
    /// </summary>
    /// <param name="solutionPath">Absolute path to the .sln, .slnx, or .csproj file.</param>
    public AnalysisResult Analyze(string solutionPath)
    {
        logger.LogInformation("Starting analysis of '{Path}'", solutionPath);

        var projects = parser.Parse(solutionPath);
        logger.LogInformation("Discovered {Count} project(s)", projects.Count);

        foreach (var p in projects)
            logger.LogDebug("  {Project}", p);

        var violations = new List<ArchitecturalViolation>();

        foreach (var rule in rules)
        {
            logger.LogDebug("Running rule: {Rule}", rule.Name);
            var ruleViolations = rule.Evaluate(projects).ToList();

            if (ruleViolations.Count > 0)
                logger.LogWarning("Rule '{Rule}' found {Count} violation(s)", rule.Name, ruleViolations.Count);
            else
                logger.LogDebug("Rule '{Rule}' passed", rule.Name);

            violations.AddRange(ruleViolations);
        }

        logger.LogInformation(
            "Analysis complete. {ViolationCount} violation(s) found across {ProjectCount} project(s).",
            violations.Count, projects.Count);

        return new AnalysisResult
        {
            AnalyzedPath = solutionPath,
            Projects = projects,
            Violations = violations.AsReadOnly(),
            AnalyzedAt = DateTimeOffset.UtcNow
        };
    }
}
