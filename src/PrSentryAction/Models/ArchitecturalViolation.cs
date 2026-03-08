namespace PrSentryAction.Models;

/// <summary>
/// Represents a single Clean Architecture rule violation.
/// </summary>
public sealed class ArchitecturalViolation
{
    /// <summary>Gets the name of the rule that was violated.</summary>
    public string RuleName { get; init; } = string.Empty;

    /// <summary>Gets the project that violated the rule.</summary>
    public string ProjectName { get; init; } = string.Empty;

    /// <summary>Gets a human-readable description of the violation.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the severity of the violation.</summary>
    public ViolationSeverity Severity { get; init; } = ViolationSeverity.Error;

    public override string ToString() => $"[{Severity}] {RuleName}: {ProjectName} – {Description}";
}
