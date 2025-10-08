using System.Collections.Concurrent;

namespace TestProject.Infrastructure.Agents;

/// <summary>
/// Provides workflow execution context across executors
/// </summary>
public class WorkflowContextProvider
{
  private readonly ConcurrentDictionary<string, Guid> _workflowThreadMapping = new();
  private readonly AsyncLocal<Guid?> _currentThreadId = new();
  private const string CURRENT_WORKFLOW_KEY = "__current_workflow__";

  public void SetWorkflowThread(string workflowRunId, Guid threadId)
  {
    _workflowThreadMapping[workflowRunId] = threadId;
  }

  public Guid? GetThreadForWorkflow(string workflowRunId)
  {
    return _workflowThreadMapping.TryGetValue(workflowRunId, out var threadId) ? threadId : null;
  }

  public void SetCurrentWorkflow(Guid workflowId, Guid threadId)
  {
    _currentThreadId.Value = threadId;
    _workflowThreadMapping[CURRENT_WORKFLOW_KEY] = threadId;
    _workflowThreadMapping[workflowId.ToString()] = threadId;
  }

  public Guid GetCurrentThreadId()
  {
    // Try AsyncLocal first
    if (_currentThreadId.Value.HasValue)
      return _currentThreadId.Value.Value;

    // Fall back to the current workflow key
    if (_workflowThreadMapping.TryGetValue(CURRENT_WORKFLOW_KEY, out var threadId))
      return threadId;

    throw new InvalidOperationException("No thread ID set in current context");
  }
}
