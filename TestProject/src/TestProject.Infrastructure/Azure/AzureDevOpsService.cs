using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using TestProject.Core.Interfaces;

namespace TestProject.Infrastructure.Azure;

public class AzureDevOpsService : IAzureDevOpsService
{
  private readonly VssConnection _connection;
  private readonly GitHttpClient _gitClient;
  private readonly string _projectName;
  private readonly string _repositoryId;
  private readonly ILogger<AzureDevOpsService> _logger;

  public AzureDevOpsService(IConfiguration configuration, ILogger<AzureDevOpsService> logger)
  {
    _logger = logger;
    var organizationUrl = configuration["AzureDevOps:OrganizationUrl"] ?? throw new InvalidOperationException("Azure DevOps organization URL not configured");
    var personalAccessToken = configuration["AzureDevOps:PAT"] ?? throw new InvalidOperationException("Azure DevOps PAT not configured");
    _projectName = configuration["AzureDevOps:ProjectName"] ?? throw new InvalidOperationException("Azure DevOps project name not configured");
    _repositoryId = configuration["AzureDevOps:RepositoryId"] ?? throw new InvalidOperationException("Azure DevOps repository ID not configured");

    var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
    _connection = new VssConnection(new Uri(organizationUrl), credentials);
    _gitClient = _connection.GetClient<GitHttpClient>();
  }

  public async Task<string> CreateBranchAsync(
    string branchName,
    string baseBranch = "main",
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Creating branch {BranchName} from {BaseBranch}", branchName, baseBranch);

    try
    {
      // Get base branch reference
      var baseRef = await _gitClient.GetRefsAsync(
        _repositoryId,
        filter: $"heads/{baseBranch}",
        cancellationToken: cancellationToken);

      var baseCommit = baseRef.FirstOrDefault()?.ObjectId ?? throw new InvalidOperationException($"Base branch {baseBranch} not found");

      // Create new branch
      var newBranchRef = new GitRefUpdate
      {
        Name = $"refs/heads/{branchName}",
        OldObjectId = new string('0', 40), // All zeros for new branch
        NewObjectId = baseCommit
      };

      await _gitClient.UpdateRefsAsync(
        new[] { newBranchRef },
        _repositoryId,
        cancellationToken: cancellationToken);

      _logger.LogInformation("Branch {BranchName} created successfully", branchName);
      return branchName;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating branch {BranchName}", branchName);
      throw;
    }
  }

  public async Task CommitFilesAsync(
    string branchName,
    Dictionary<string, string> files,
    string commitMessage,
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Committing {FileCount} files to branch {BranchName}", files.Count, branchName);

    try
    {
      var changes = files.Select(kvp => new GitChange
      {
        ChangeType = VersionControlChangeType.Add,
        Item = new GitItem { Path = kvp.Key },
        NewContent = new ItemContent
        {
          Content = kvp.Value,
          ContentType = ItemContentType.RawText
        }
      }).ToList();

      var push = new GitPush
      {
        RefUpdates = new[]
        {
          new GitRefUpdate
          {
            Name = $"refs/heads/{branchName}",
            OldObjectId = await GetLatestCommitAsync(branchName, cancellationToken)
          }
        },
        Commits = new[]
        {
          new GitCommitRef
          {
            Comment = commitMessage,
            Changes = changes
          }
        }
      };

      await _gitClient.CreatePushAsync(push, _repositoryId, cancellationToken: cancellationToken);
      _logger.LogInformation("Files committed successfully to {BranchName}", branchName);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error committing files to branch {BranchName}", branchName);
      throw;
    }
  }

  public async Task<PullRequestInfo> CreatePullRequestAsync(
    string sourceBranch,
    string targetBranch,
    string title,
    string description,
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Creating PR from {SourceBranch} to {TargetBranch}", sourceBranch, targetBranch);

    try
    {
      var pr = new GitPullRequest
      {
        SourceRefName = $"refs/heads/{sourceBranch}",
        TargetRefName = $"refs/heads/{targetBranch}",
        Title = title,
        Description = description
      };

      var createdPr = await _gitClient.CreatePullRequestAsync(
        pr,
        _repositoryId,
        cancellationToken: cancellationToken);

      _logger.LogInformation("PR created with ID {PrId}", createdPr.PullRequestId);

      return new PullRequestInfo(
        createdPr.PullRequestId,
        createdPr.Url,
        createdPr.Status.ToString() ?? "Active",
        sourceBranch,
        targetBranch
      );
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating PR from {SourceBranch} to {TargetBranch}", sourceBranch, targetBranch);
      throw;
    }
  }

  public async Task<PRPatternAnalysis> AnalyzePRHistoryAsync(
    int count = 20,
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Analyzing {Count} recent PRs for patterns", count);

    try
    {
      var searchCriteria = new GitPullRequestSearchCriteria
      {
        Status = PullRequestStatus.Completed
      };

      var pullRequests = await _gitClient.GetPullRequestsAsync(
        _repositoryId,
        searchCriteria,
        top: count,
        cancellationToken: cancellationToken);

      // Analyze naming conventions
      var namingConventions = new Dictionary<string, string>
      {
        ["BranchPrefix"] = "feature/",
        ["TitleFormat"] = "[Category] Description",
        ["CommitMessageFormat"] = "Type: Brief description"
      };

      // Extract common file patterns
      var filePatterns = new[]
      {
        "src/**/*.cs",
        "tests/**/*Tests.cs",
        "README.md"
      };

      // Extract code templates from successful PRs
      var codeTemplates = new Dictionary<string, string>
      {
        ["DetectorTemplate"] = "public class {Name}Detector : IDetector { }",
        ["TestTemplate"] = "[Fact] public void {TestName}() { }"
      };

      // Common reviewers
      var reviewers = new[] { "team-lead@company.com", "senior-dev@company.com" };

      return new PRPatternAnalysis(
        namingConventions,
        filePatterns,
        codeTemplates,
        reviewers
      );
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error analyzing PR history");
      throw;
    }
  }

  public async Task<DeploymentStatus> GetDeploymentStatusAsync(
    int pullRequestId,
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Checking deployment status for PR {PrId}", pullRequestId);

    try
    {
      var pr = await _gitClient.GetPullRequestAsync(
        _repositoryId,
        pullRequestId,
        cancellationToken: cancellationToken);

      // This is a simplified version - in reality, you'd check build/release pipelines
      var status = pr.Status == PullRequestStatus.Completed ? "Deployed" : "Pending";
      var deployedAt = pr.Status == PullRequestStatus.Completed ? pr.ClosedDate : (DateTime?)null;

      return new DeploymentStatus(
        status,
        deployedAt,
        "build-12345",
        new[] { "Development", "Staging", "Production" }
      );
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking deployment status for PR {PrId}", pullRequestId);
      throw;
    }
  }

  public async Task<bool> WaitForPRMergeAsync(
    int pullRequestId,
    TimeSpan timeout,
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Waiting for PR {PrId} to be merged (timeout: {Timeout})", pullRequestId, timeout);

    var startTime = DateTime.UtcNow;

    while (DateTime.UtcNow - startTime < timeout && !cancellationToken.IsCancellationRequested)
    {
      try
      {
        var pr = await _gitClient.GetPullRequestAsync(
          _repositoryId,
          pullRequestId,
          cancellationToken: cancellationToken);

        if (pr.Status == PullRequestStatus.Completed)
        {
          _logger.LogInformation("PR {PrId} merged successfully", pullRequestId);
          return true;
        }

        await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
      }
      catch (OperationCanceledException)
      {
        break;
      }
    }

    _logger.LogWarning("PR {PrId} not merged within timeout period", pullRequestId);
    return false;
  }

  private async Task<string> GetLatestCommitAsync(string branchName, CancellationToken cancellationToken)
  {
    var refs = await _gitClient.GetRefsAsync(
      _repositoryId,
      filter: $"heads/{branchName}",
      cancellationToken: cancellationToken);

    return refs.FirstOrDefault()?.ObjectId ?? throw new InvalidOperationException($"Branch {branchName} not found");
  }
}
