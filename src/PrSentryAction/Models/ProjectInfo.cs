namespace PrSentryAction.Models;

/// <summary>
/// Represents a .NET project discovered from a solution or project file.
/// </summary>
public sealed class ProjectInfo
{
    /// <summary>Gets the project name (without extension).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the absolute path to the .csproj file.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Gets the names of all directly referenced projects.</summary>
    public IReadOnlyList<string> ProjectReferences { get; init; } = [];

    /// <summary>Gets the detected Clean Architecture layer for this project.</summary>
    public ArchitectureLayer Layer { get; init; } = ArchitectureLayer.Unknown;

    public override string ToString() => $"{Name} ({Layer})";
}
