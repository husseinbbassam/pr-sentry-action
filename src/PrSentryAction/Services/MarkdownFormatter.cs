using System.Text;
using PrSentryAction.Models;

namespace PrSentryAction.Services;

/// <summary>
/// Converts an <see cref="AnalysisResult"/> into a GitHub-flavoured Markdown comment body.
/// </summary>
public sealed class MarkdownFormatter
{
    private const string CheckMark = "✅";
    private const string CrossMark = "❌";
    private const string WarningMark = "⚠️";

    /// <summary>
    /// Formats the analysis result as a Markdown string suitable for posting as a PR comment.
    /// </summary>
    public string Format(AnalysisResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## 🏛️ PR Sentry – Clean Architecture Analysis");
        sb.AppendLine();
        sb.AppendLine($"> **Analyzed:** `{result.AnalyzedPath}`");
        sb.AppendLine($"> **Projects found:** {result.Projects.Count}");
        sb.AppendLine($"> **Run at:** {result.AnalyzedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        if (!result.HasViolations)
        {
            sb.AppendLine($"{CheckMark} **No architectural violations found.** Your solution complies with Clean Architecture rules.");
            sb.AppendLine();
            AppendProjectTable(sb, result.Projects);
            return sb.ToString();
        }

        sb.AppendLine($"{CrossMark} **{result.Violations.Count} architectural violation(s) detected.**");
        sb.AppendLine();

        // Group violations by rule
        var byRule = result.Violations
            .GroupBy(v => v.RuleName)
            .OrderBy(g => g.Key);

        foreach (var group in byRule)
        {
            sb.AppendLine($"### {CrossMark} Rule: {group.Key}");
            sb.AppendLine();
            sb.AppendLine("| Severity | Project | Description |");
            sb.AppendLine("|----------|---------|-------------|");

            foreach (var violation in group)
            {
                var severityIcon = violation.Severity == ViolationSeverity.Error ? CrossMark : WarningMark;
                var escapedDesc = violation.Description.Replace("|", "\\|");
                sb.AppendLine($"| {severityIcon} {violation.Severity} | `{violation.ProjectName}` | {escapedDesc} |");
            }

            sb.AppendLine();
        }

        AppendProjectTable(sb, result.Projects);

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("_Powered by [PR Sentry](https://github.com/husseinbbassam/pr-sentry-action)_ 🛡️");

        return sb.ToString();
    }

    private static void AppendProjectTable(StringBuilder sb, IReadOnlyList<ProjectInfo> projects)
    {
        sb.AppendLine("### 📦 Discovered Projects");
        sb.AppendLine();
        sb.AppendLine("| Project | Layer |");
        sb.AppendLine("|---------|-------|");

        foreach (var project in projects.OrderBy(p => p.Name))
        {
            var layerIcon = project.Layer switch
            {
                ArchitectureLayer.Domain => "🧬",
                ArchitectureLayer.Application => "⚙️",
                ArchitectureLayer.Infrastructure => "🔧",
                ArchitectureLayer.Data => "🗄️",
                ArchitectureLayer.WebApi => "🌐",
                _ => "❓"
            };
            sb.AppendLine($"| `{project.Name}` | {layerIcon} {project.Layer} |");
        }

        sb.AppendLine();
    }
}
