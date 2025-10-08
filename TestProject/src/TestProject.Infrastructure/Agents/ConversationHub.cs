using Microsoft.AspNetCore.SignalR;
using TestProject.Core.AgentWorkflowAggregate;
using TestProject.Core.Interfaces;

namespace TestProject.Infrastructure.Agents;

/// <summary>
/// SignalR hub for real-time conversation messages
/// </summary>
public class ConversationHub : Hub
{
  private readonly IConversationService _conversationService;
  private readonly ILogger<ConversationHub> _logger;

  public ConversationHub(
    IConversationService conversationService,
    ILogger<ConversationHub> logger)
  {
    _conversationService = conversationService;
    _logger = logger;
  }

  public async Task JoinWorkflow(string workflowId)
  {
    await Groups.AddToGroupAsync(Context.ConnectionId, workflowId);
    _logger.LogInformation("Client {ConnectionId} joined workflow {WorkflowId}",
      Context.ConnectionId, workflowId);
  }

  public async Task LeaveWorkflow(string workflowId)
  {
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, workflowId);
    _logger.LogInformation("Client {ConnectionId} left workflow {WorkflowId}",
      Context.ConnectionId, workflowId);
  }

  public Task SendApproval(string workflowId, string approvalId, bool approved, string? feedback)
  {
    _logger.LogInformation("Received approval {ApprovalId} for workflow {WorkflowId}: {Approved}",
      approvalId, workflowId, approved);

    // The approval will be processed by the endpoint
    // This is just for logging/notification purposes
    return Task.CompletedTask;
  }
}
