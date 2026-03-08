using PrSentryAction.Models;

namespace PrSentryAction.Rules;

/// <summary>
/// Rule 1 – Domain Isolation:
/// The Domain project must have zero dependencies on other internal projects.
/// The Domain layer represents pure business entities and logic; it must remain
/// free of any outward-facing or infrastructure concerns.
/// </summary>
public sealed class DomainRule : IArchitectureRule
{
    public string Name => "Domain Isolation";

    public string Description =>
        "The Domain project must not reference any other internal projects. " +
        "It should contain only pure business entities and domain logic.";

    public IEnumerable<ArchitecturalViolation> Evaluate(IReadOnlyList<ProjectInfo> projects)
    {
        var internalProjectNames = projects
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects.Where(p => p.Layer == ArchitectureLayer.Domain))
        {
            var forbiddenRefs = project.ProjectReferences
                .Where(r => internalProjectNames.Contains(r))
                .ToList();

            foreach (var forbidden in forbiddenRefs)
            {
                yield return new ArchitecturalViolation
                {
                    RuleName = Name,
                    ProjectName = project.Name,
                    Description = $"Domain project '{project.Name}' has a forbidden dependency on '{forbidden}'. " +
                                  "Domain must have zero internal project references.",
                    Severity = ViolationSeverity.Error
                };
            }
        }
    }
}
