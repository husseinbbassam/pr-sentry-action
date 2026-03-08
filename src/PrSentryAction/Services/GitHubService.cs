using System.Text.Json;
using Microsoft.Extensions.Logging;
using Octokit;

namespace PrSentryAction.Services;

/// <summary>
/// Interacts with the GitHub REST API to post Pull Request comments.
/// </summary>
public sealed class GitHubService(ILogger<GitHubService> logger)
{
    /// <summary>
    /// Posts (or updates) a PR comment with the given Markdown body.
    /// If a previous PR Sentry comment exists it will be replaced to avoid
    /// flooding the PR thread.
    /// </summary>
    /// <param name="token">GitHub personal access token or GITHUB_TOKEN secret.</param>
    /// <param name="repository">Repository in &quot;owner/repo&quot; format.</param>
    /// <param name="eventPath">Path to the GitHub event JSON payload file.</param>
    /// <param name="markdownBody">The formatted Markdown comment to post.</param>
    public async Task PostCommentAsync(
        string token,
        string repository,
        string eventPath,
        string markdownBody)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("No GitHub token provided – skipping PR comment.");
            return;
        }

        int? pullRequestNumber = TryGetPullRequestNumber(eventPath);
        if (pullRequestNumber is null)
        {
            logger.LogInformation("Not a pull_request event – skipping PR comment.");
            return;
        }

        var repoParts = repository.Split('/', 2);
        if (repoParts.Length != 2)
        {
            logger.LogWarning("GITHUB_REPOSITORY '{Repo}' is not in owner/repo format – skipping PR comment.", repository);
            return;
        }

        var owner = repoParts[0];
        var repo = repoParts[1];

        var client = new GitHubClient(new ProductHeaderValue("pr-sentry-action"))
        {
            Credentials = new Credentials(token)
        };

        try
        {
            // Look for an existing comment by PR Sentry to replace it
            var existingComments = await client.Issue.Comment.GetAllForIssue(owner, repo, pullRequestNumber.Value);
            var existingComment = existingComments.FirstOrDefault(
                c => c.Body.Contains("🏛️ PR Sentry", StringComparison.Ordinal));

            if (existingComment is not null)
            {
                logger.LogInformation("Updating existing PR Sentry comment #{CommentId}", existingComment.Id);
                await client.Issue.Comment.Update(owner, repo, existingComment.Id, markdownBody);
            }
            else
            {
                logger.LogInformation("Creating new PR Sentry comment on PR #{Number}", pullRequestNumber.Value);
                await client.Issue.Comment.Create(owner, repo, pullRequestNumber.Value, markdownBody);
            }

            logger.LogInformation("PR comment posted successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to post PR comment to {Owner}/{Repo}#{Pr}", owner, repo, pullRequestNumber.Value);
            throw;
        }
    }

    private int? TryGetPullRequestNumber(string eventPath)
    {
        if (string.IsNullOrWhiteSpace(eventPath) || !File.Exists(eventPath))
            return null;

        try
        {
            var json = File.ReadAllText(eventPath);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("pull_request", out var pr) &&
                pr.TryGetProperty("number", out var numberEl) &&
                numberEl.TryGetInt32(out var number))
            {
                return number;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not parse pull request number from event payload at '{Path}'", eventPath);
        }

        return null;
    }
}
