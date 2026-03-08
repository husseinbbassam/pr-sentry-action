namespace PrSentryAction.Models;

/// <summary>
/// Holds the complete result of an architectural analysis run.
/// </summary>
public sealed class AnalysisResult
{
    /// <summary>Gets all projects discovered during analysis.</summary>
    public IReadOnlyList<ProjectInfo> Projects { get; init; } = [];

    /// <summary>Gets all violations found during analysis.</summary>
    public IReadOnlyList<ArchitecturalViolation> Violations { get; init; } = [];

    /// <summary>Gets whether any violations were found.</summary>
    public bool HasViolations => Violations.Count > 0;

    /// <summary>Gets the solution or project file that was analyzed.</summary>
    public string AnalyzedPath { get; init; } = string.Empty;

    /// <summary>Gets the UTC timestamp when the analysis was performed.</summary>
    public DateTimeOffset AnalyzedAt { get; init; } = DateTimeOffset.UtcNow;
}
