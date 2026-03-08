using PrSentryAction.Models;

namespace PrSentryAction.Rules;

/// <summary>
/// Rule 2 – Application Layer Boundary:
/// The Application project may only depend on the Domain project.
/// It must not reference Infrastructure, Data, or Web/API projects.
/// </summary>
public sealed class ApplicationRule : IArchitectureRule
{
    public string Name => "Application Layer Boundary";

    public string Description =>
        "The Application project may only depend on Domain. " +
        "It must not reference Infrastructure, Data, or Web/API projects.";

    private static readonly HashSet<ArchitectureLayer> ForbiddenLayers = new()
    {
        ArchitectureLayer.Infrastructure,
        ArchitectureLayer.Data,
        ArchitectureLayer.WebApi
    };

    public IEnumerable<ArchitecturalViolation> Evaluate(IReadOnlyList<ProjectInfo> projects)
    {
        // Build a name → layer lookup for fast access
        var layerByName = projects.ToDictionary(
            p => p.Name,
            p => p.Layer,
            StringComparer.OrdinalIgnoreCase);

        foreach (var project in projects.Where(p => p.Layer == ArchitectureLayer.Application))
        {
            foreach (var reference in project.ProjectReferences)
            {
                if (!layerByName.TryGetValue(reference, out var refLayer))
                    continue; // external / unknown project – skip

                if (ForbiddenLayers.Contains(refLayer))
                {
                    yield return new ArchitecturalViolation
                    {
                        RuleName = Name,
                        ProjectName = project.Name,
                        Description = $"Application project '{project.Name}' illegally depends on " +
                                      $"'{reference}' which belongs to the {refLayer} layer. " +
                                      "Application may only reference Domain projects.",
                        Severity = ViolationSeverity.Error
                    };
                }
            }
        }
    }
}
