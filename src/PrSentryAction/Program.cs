using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrSentryAction.Configuration;
using PrSentryAction.Parsers;
using PrSentryAction.Rules;
using PrSentryAction.Services;

// ──────────────────────────────────────────────────────────────────────────────
// Build the DI container
// ──────────────────────────────────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddLogging(b => b
    .AddConsole()
    .SetMinimumLevel(LogLevel.Information));

services.AddSingleton<SolutionParser>();
services.AddSingleton<IArchitectureRule, DomainRule>();
services.AddSingleton<IArchitectureRule, ApplicationRule>();
services.AddSingleton<IArchitectureRule, WebApiRule>();
services.AddSingleton<ArchitectureAnalyzer>();
services.AddSingleton<MarkdownFormatter>();
services.AddSingleton<GitHubService>();

var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILogger<Program>>();

// ──────────────────────────────────────────────────────────────────────────────
// Read inputs
// ──────────────────────────────────────────────────────────────────────────────
ActionInputs inputs;
try
{
    inputs = ActionInputs.FromEnvironment();
}
catch (InvalidOperationException ex)
{
    logger.LogError("Configuration error: {Message}", ex.Message);
    return 1;
}

logger.LogInformation("PR Sentry starting");
logger.LogInformation("Solution path : {Path}", inputs.SolutionPath);
logger.LogInformation("Strict mode   : {Strict}", inputs.StrictMode);

// ──────────────────────────────────────────────────────────────────────────────
// Run the analysis
// ──────────────────────────────────────────────────────────────────────────────
var analyzer = provider.GetRequiredService<ArchitectureAnalyzer>();
var formatter = provider.GetRequiredService<MarkdownFormatter>();
var github = provider.GetRequiredService<GitHubService>();

try
{
    var result = analyzer.Analyze(inputs.SolutionPath);
    var markdown = formatter.Format(result);

    // Write to stdout so the workflow log always shows the summary
    Console.WriteLine();
    Console.WriteLine(markdown);

    // Set GitHub Actions output variable
    SetOutput("summary", markdown);

    // Post the markdown as a PR comment
    await github.PostCommentAsync(
        inputs.GitHubToken,
        inputs.GitHubRepository,
        inputs.GitHubEventPath,
        markdown);

    if (result.HasViolations)
    {
        logger.LogError("{Count} architectural violation(s) were detected.", result.Violations.Count);

        if (inputs.StrictMode)
        {
            logger.LogError("Strict mode is enabled – failing the build.");
            return 1;
        }
        else
        {
            logger.LogWarning("Strict mode is disabled – violations are reported but the build continues.");
        }
    }
    else
    {
        logger.LogInformation("No architectural violations found. All rules passed.");
    }

    return 0;
}
catch (FileNotFoundException ex)
{
    logger.LogError("File not found: {Message}", ex.Message);
    return 1;
}
catch (NotSupportedException ex)
{
    logger.LogError("Unsupported file type: {Message}", ex.Message);
    return 1;
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error during analysis.");
    return 1;
}

// ──────────────────────────────────────────────────────────────────────────────
// Helpers
// ──────────────────────────────────────────────────────────────────────────────
static void SetOutput(string name, string value)
{
    var outputFile = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
    if (string.IsNullOrWhiteSpace(outputFile))
        return;

    // GitHub Actions multiline output format
    var delimiter = $"ghadelimiter_{Guid.NewGuid():N}";
    File.AppendAllText(outputFile, $"{name}<<{delimiter}{Environment.NewLine}{value}{Environment.NewLine}{delimiter}{Environment.NewLine}");
}
