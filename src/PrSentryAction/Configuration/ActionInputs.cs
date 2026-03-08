namespace PrSentryAction.Configuration;

/// <summary>
/// Reads and validates all GitHub Actions inputs and context variables
/// from environment variables injected by the Actions runner.
/// </summary>
public sealed class ActionInputs
{
    /// <summary>Path to the .sln or root .csproj file to analyze.</summary>
    public string SolutionPath { get; init; }

    /// <summary>When true, the action exits with a non-zero code if violations are found.</summary>
    public bool StrictMode { get; init; }

    /// <summary>GitHub token used to authenticate API requests.</summary>
    public string GitHubToken { get; init; }

    /// <summary>Repository in &quot;owner/repo&quot; format.</summary>
    public string GitHubRepository { get; init; }

    /// <summary>Absolute path to the GitHub event JSON payload file.</summary>
    public string GitHubEventPath { get; init; }

    /// <summary>Name of the GitHub event that triggered the workflow.</summary>
    public string GitHubEventName { get; init; }

    public ActionInputs(
        string solutionPath,
        bool strictMode,
        string gitHubToken,
        string gitHubRepository,
        string gitHubEventPath,
        string gitHubEventName)
    {
        SolutionPath = solutionPath;
        StrictMode = strictMode;
        GitHubToken = gitHubToken;
        GitHubRepository = gitHubRepository;
        GitHubEventPath = gitHubEventPath;
        GitHubEventName = gitHubEventName;
    }

    /// <summary>
    /// Creates an <see cref="ActionInputs"/> instance from the current process
    /// environment variables as set by the GitHub Actions runner.
    /// </summary>
    public static ActionInputs FromEnvironment()
    {
        var solutionPath = Environment.GetEnvironmentVariable("INPUT_SOLUTION_PATH")
            ?? throw new InvalidOperationException("Required input 'solution-path' is missing (INPUT_SOLUTION_PATH).");

        var strictRaw = Environment.GetEnvironmentVariable("INPUT_STRICT") ?? "false";
        var strict = strictRaw.Equals("true", StringComparison.OrdinalIgnoreCase);

        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? Environment.GetEnvironmentVariable("INPUT_GITHUB_TOKEN")
            ?? string.Empty;

        var repo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY") ?? string.Empty;
        var eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH") ?? string.Empty;
        var eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME") ?? string.Empty;

        return new ActionInputs(solutionPath, strict, token, repo, eventPath, eventName);
    }
}
