namespace PrSentryAction.Models;

/// <summary>
/// Identifies the Clean Architecture layer a project belongs to based on its name.
/// </summary>
public enum ArchitectureLayer
{
    Unknown,
    Domain,
    Application,
    Infrastructure,
    Data,
    WebApi
}
