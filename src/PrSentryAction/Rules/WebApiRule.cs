using PrSentryAction.Models;

namespace PrSentryAction.Rules;

/// <summary>
/// Rule 3 – Web/API Layer Boundary:
/// The Web/API project should not have direct references to Infrastructure or Data projects.
/// It should depend on Application (and therefore Domain through transitive references),
/// enforcing interface-based / dependency-inversion programming.
/// </summary>
public sealed class WebApiRule : IArchitectureRule
{
    public string Name => "Web/API Layer Boundary";

    public string Description =>
        "The Web/API project must not directly reference Infrastructure or Data projects. " +
        "Infrastructure concerns should be registered via dependency injection at the composition root, " +
        "not coupled directly to the presentation layer.";

    private static readonly HashSet<ArchitectureLayer> ForbiddenLayers = new()
    {
        ArchitectureLayer.Infrastructure,
        ArchitectureLayer.Data
    };

    public IEnumerable<ArchitecturalViolation> Evaluate(IReadOnlyList<ProjectInfo> projects)
    {
        var layerByName = projects.ToDictionary(
            p => p.Name,
            p => p.Layer,
            StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects.Where(p => p.Layer == ArchitectureLayer.WebApi))
        {
            foreach (var reference in project.ProjectReferences)
            {
                if (!layerByName.TryGetValue(reference, out var refLayer))
                    continue;

                if (ForbiddenLayers.Contains(refLayer))
                {
                    yield return new ArchitecturalViolation
                    {
                        RuleName = Name,
                        ProjectName = project.Name,
                        Description = $"Web/API project '{project.Name}' directly references " +
                                      $"'{reference}' ({refLayer} layer). " +
                                      "Infrastructure and Data dependencies should be registered " +
                                      "via the DI container, not referenced directly from the presentation layer.",
                        Severity = ViolationSeverity.Error
                    };
                }
            }
        }
    }
}
