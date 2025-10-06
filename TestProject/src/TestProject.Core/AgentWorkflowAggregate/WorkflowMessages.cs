using Microsoft.Extensions.AI;
using TestProject.Core.Interfaces;

namespace TestProject.Core.AgentWorkflowAggregate;

// Message types for workflow communication
public record ETWInput(string UserId, string ETWDetails);

// After validation agent
public record ValidationResult(bool IsValid, string Message, KustoQueryResult? KustoData, ETWInput Input);

// After Kusto query executor
public record BranchCreated(string BranchName, string RepositoryUrl, KustoQueryResult KustoData, ETWInput Input);

// After PR analysis agent (returns ChatMessage from agent)
public record PRPatterns(string Patterns, string Conventions, BranchCreated BranchInfo);

// After code generation agent
public record GeneratedCode(string DetectorCode, string FilePath, PRPatterns Patterns);

// After PR creation executor
public record PRCreated(int PullRequestId, string PRUrl, GeneratedCode Code, bool RequiresApproval = true);

// After human approval
public record ApprovalDecision(bool Approved, string? Reason, PRCreated PR);

// After deployment monitoring
public record DeploymentResult(string Status, DateTime? DeployedAt, string BuildId, ApprovalDecision Approval);

// After results fetch
public record PerformanceData(string ResultsJson, DetectorExecutionResults Results, DeploymentResult Deployment);

// After analysis agent
public record AnalysisResult(string Analysis, string Recommendations, PerformanceData Data);

// After customer rollout PR creation
public record CustomerRolloutPR(int PullRequestId, string PRUrl, AnalysisResult Analysis);
