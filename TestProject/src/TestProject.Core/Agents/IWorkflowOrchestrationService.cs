using Microsoft.Agents.AI.Workflows;

namespace TestProject.Core.Agents;

/// <summary>
/// Service for orchestrating ETW detector workflows using Microsoft Agent Framework
/// </summary>
public interface IWorkflowOrchestrationService
{
  /// <summary>
  /// Starts a new ETW detector workflow
  /// </summary>
  Task<Guid> StartWorkflowAsync(string userId, string etwDetails, CancellationToken cancellationToken = default);

  /// <summary>
  /// Gets the result of a completed workflow
  /// </summary>
  Task<string?> GetWorkflowResultAsync(Guid workflowId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Streams workflow events in real-time
  /// </summary>
  IAsyncEnumerable<WorkflowEvent> StreamWorkflowEventsAsync(Guid workflowId, CancellationToken cancellationToken = default);
}
