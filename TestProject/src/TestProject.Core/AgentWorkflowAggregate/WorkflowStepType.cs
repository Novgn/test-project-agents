namespace TestProject.Core.AgentWorkflowAggregate;

public enum WorkflowStepType
{
  AcceptUserInput = 1,
  QueryKusto = 2,
  CreateBranch = 3,
  AnalyzePRHistory = 4,
  GenerateDetectorCode = 5,
  CreatePR = 6,
  WaitForPRApproval = 7,
  MonitorDeployment = 8,
  FetchDetectorResults = 9,
  AnalyzeResults = 10,
  CreateCustomerFacingPR = 11
}
