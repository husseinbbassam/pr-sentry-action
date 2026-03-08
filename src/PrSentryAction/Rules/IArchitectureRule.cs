using PrSentryAction.Models;

namespace PrSentryAction.Rules;

/// <summary>
/// Defines a Clean Architecture dependency rule that can be evaluated against a project graph.
/// </summary>
public interface IArchitectureRule
{
    /// <summary>Gets the unique name of this rule.</summary>
    string Name { get; }

    /// <summary>Gets a human-readable description of what this rule enforces.</summary>
    string Description { get; }

    /// <summary>
    /// Evaluates the rule against the full set of discovered projects.
    /// </summary>
    /// <param name="projects">All projects in the solution.</param>
    /// <returns>Zero or more violations found by this rule.</returns>
    IEnumerable<ArchitecturalViolation> Evaluate(IReadOnlyList<ProjectInfo> projects);
}
