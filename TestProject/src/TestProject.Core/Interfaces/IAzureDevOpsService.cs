namespace TestProject.Core.Interfaces;

/// <summary>
/// Service for Azure DevOps operations
/// </summary>
public interface IAzureDevOpsService
{
  /// <summary>
  /// Creates a new branch in the repository
  /// </summary>
  Task<string> CreateBranchAsync(string branchName, string baseBranch = "main", CancellationToken cancellationToken = default);

  /// <summary>
  /// Adds or updates files in a branch
  /// </summary>
  Task CommitFilesAsync(string branchName, Dictionary<string, string> files, string commitMessage, CancellationToken cancellationToken = default);

  /// <summary>
  /// Creates a pull request
  /// </summary>
  Task<PullRequestInfo> CreatePullRequestAsync(
    string sourceBranch,
    string targetBranch,
    string title,
    string description,
    CancellationToken cancellationToken = default);

  /// <summary>
  /// Analyzes historical PRs to extract patterns and conventions
  /// </summary>
  Task<PRPatternAnalysis> AnalyzePRHistoryAsync(int count = 20, CancellationToken cancellationToken = default);

  /// <summary>
  /// Monitors deployment pipeline status
  /// </summary>
  Task<DeploymentStatus> GetDeploymentStatusAsync(int pullRequestId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Waits for a PR to be merged
  /// </summary>
  Task<bool> WaitForPRMergeAsync(int pullRequestId, TimeSpan timeout, CancellationToken cancellationToken = default);
}

public record PullRequestInfo(
  int Id,
  string Url,
  string Status,
  string SourceBranch,
  string TargetBranch
);

public record PRPatternAnalysis(
  Dictionary<string, string> NamingConventions,
  string[] CommonFilePatterns,
  Dictionary<string, string> CodeTemplates,
  string[] RequiredReviewers
);

public record DeploymentStatus(
  string Status,
  DateTime? DeployedAt,
  string BuildId,
  string[] Environments
);
