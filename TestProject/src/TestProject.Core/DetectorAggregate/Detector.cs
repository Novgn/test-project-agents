namespace TestProject.Core.DetectorAggregate;

public class Detector : EntityBase<Guid>, IAggregateRoot
{
  public string Name { get; private set; } = string.Empty;
  public string Description { get; private set; } = string.Empty;
  public string ETWProvider { get; private set; } = string.Empty;
  public string ConverterName { get; private set; } = string.Empty;
  public string BranchName { get; private set; } = string.Empty;
  public int? PullRequestId { get; private set; }
  public string? PullRequestUrl { get; private set; }
  public bool IsCustomerFacing { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public DateTime? DeployedAt { get; private set; }
  public string Status { get; private set; } = "Draft";

  private Detector() { } // EF Core

  public Detector(
    string name,
    string description,
    string etwProvider,
    string converterName)
  {
    Id = Guid.NewGuid();
    Name = Guard.Against.NullOrEmpty(name, nameof(name));
    Description = Guard.Against.NullOrEmpty(description, nameof(description));
    ETWProvider = Guard.Against.NullOrEmpty(etwProvider, nameof(etwProvider));
    ConverterName = Guard.Against.NullOrEmpty(converterName, nameof(converterName));
    CreatedAt = DateTime.UtcNow;
    IsCustomerFacing = false;
  }

  public void SetBranch(string branchName)
  {
    BranchName = Guard.Against.NullOrEmpty(branchName, nameof(branchName));
    Status = "BranchCreated";
  }

  public void SetPullRequest(int pullRequestId, string pullRequestUrl)
  {
    PullRequestId = Guard.Against.NegativeOrZero(pullRequestId, nameof(pullRequestId));
    PullRequestUrl = Guard.Against.NullOrEmpty(pullRequestUrl, nameof(pullRequestUrl));
    Status = "PRCreated";
  }

  public void MarkAsDeployed()
  {
    DeployedAt = DateTime.UtcNow;
    Status = "Deployed";
  }

  public void MakeCustomerFacing()
  {
    if (!DeployedAt.HasValue)
    {
      throw new InvalidOperationException("Cannot make detector customer-facing before deployment");
    }
    IsCustomerFacing = true;
    Status = "CustomerFacing";
  }
}
