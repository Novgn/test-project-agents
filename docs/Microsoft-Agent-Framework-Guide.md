# Microsoft Agent Framework Workflow Guide

This guide explains how to integrate executors and AI agents into conversational workflows using the Microsoft Agent Framework.

## Overview

The Microsoft Agent Framework allows you to build **conversational workflows** where executors are chained together based on their input/output message types. The framework automatically routes messages between executors.

## Key Concepts

### 1. **Executors**

Executors are components that:
- Inherit from `ReflectingExecutor<T>`
- Implement `IMessageHandler<TInput, TOutput>`
- Process a message of type `TInput` and return `TOutput`

Example:
```csharp
public class KustoQueryExecutor :
  ReflectingExecutor<KustoQueryExecutor>("KustoQueryExecutor"),
  IMessageHandler<ChatMessage, BranchCreated>
{
  public async ValueTask<BranchCreated> HandleAsync(
    ChatMessage input,
    IWorkflowContext context)
  {
    // Process input and return output
  }
}
```

### 2. **Workflow Chaining**

Executors are chained using `WorkflowBuilder.AddEdge()`. The framework automatically routes messages based on matching input/output types.

```csharp
var builder = new WorkflowBuilder(firstExecutor);
builder.AddEdge(firstExecutor, secondExecutor);
builder.AddEdge(secondExecutor, thirdExecutor);
var workflow = builder.WithOutputFrom(thirdExecutor).Build();
```

### 3. **Message Types**

The framework routes messages based on types:
- `ETWInput` - Initial workflow input
- `ChatMessage` - AI-generated conversational responses
- `BranchCreated` - Domain-specific workflow data
- `PRCreated` - Pull request data
- `string` - Final output

## Your ETW Detector Workflow

Here's your complete workflow chain:

```
┌─────────────────────┐
│  AIValidationAgent  │  ETWInput → ChatMessage
│  (Validates input)  │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  KustoQueryExecutor │  ChatMessage → BranchCreated
│  (Queries database) │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────────┐
│  BranchCreationExecutor │  BranchCreated → BranchCreated
│  (Creates Git branch)   │  (includes approval)
└──────────┬──────────────┘
           │
           ▼
┌─────────────────────────┐
│  CodeGenerationAgent    │  BranchCreated → ChatMessage
│  (AI generates code)    │  (uses IChatClient + LLM)
└──────────┬──────────────┘
           │
           ▼
┌─────────────────────┐
│  PRCreationExecutor │  ChatMessage → PRCreated
│  (Creates PR)       │  (includes approval)
└──────────┬──────────┘
           │
           ▼
┌──────────────────────────┐
│  DeploymentMonitorExecutor│  PRCreated → string
│  (Monitors deployment)    │
└───────────────────────────┘
```

## Adding Executors to the Workflow

### Step 1: Create Your Executor

```csharp
public class MyExecutor(
  IMyService myService,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<MyExecutor> logger)
  : ReflectingExecutor<MyExecutor>("MyExecutor"),
    IMessageHandler<InputType, OutputType>
{
  public async ValueTask<OutputType> HandleAsync(
    InputType input,
    IWorkflowContext context)
  {
    var threadId = contextProvider.GetCurrentThreadId();

    // Send updates to the conversation UI
    await SendMessageAsync(threadId, "Processing...");

    // Do your work
    var result = await myService.DoWork(input);

    // Return typed output for next executor
    return new OutputType(result);
  }

  private async Task SendMessageAsync(Guid threadId, string content)
  {
    var message = new ConversationMessage
    {
      Id = Guid.NewGuid().ToString(),
      Type = ConversationMessageType.AgentMessage,
      Content = content
    };
    await conversationService.AddMessageAsync(threadId, message, CancellationToken.None);
  }
}
```

### Step 2: Register in DI

In `InfrastructureServiceExtensions.cs`:

```csharp
services.AddTransient<MyExecutor>();
```

### Step 3: Add to Workflow

In `ETWDetectorWorkflow.cs`:

```csharp
var myExecutor = serviceProvider.GetRequiredService<MyExecutor>();
builder.AddEdge(previousExecutor, myExecutor);
```

## Creating AI Agents for Type Conversion

When you need to bridge type gaps (e.g., convert `BranchCreated → ChatMessage`), create an AI agent that uses `IChatClient`:

```csharp
public class CodeGenerationAgent(
  IChatClient chatClient,
  IConversationService conversationService,
  WorkflowContextProvider contextProvider,
  ILogger<CodeGenerationAgent> logger)
  : ReflectingExecutor<CodeGenerationAgent>("CodeGenerationAgent"),
    IMessageHandler<BranchCreated, ChatMessage>
{
  public async ValueTask<ChatMessage> HandleAsync(
    BranchCreated input,
    IWorkflowContext context)
  {
    var messages = new List<ChatMessage>
    {
      new(ChatRole.System, "You are an expert code generator..."),
      new(ChatRole.User, $"Generate code for: {input.BranchName}")
    };

    var response = await chatClient.GetResponseAsync(
      messages,
      new ChatOptions { Temperature = 0.3f });

    return new ChatMessage(ChatRole.Assistant, response?.Text ?? "");
  }
}
```

## Human-in-the-Loop Approvals

Use `IConversationService.RequestApprovalAsync()` to add approval steps:

```csharp
var approval = new ApprovalRequest
{
  Id = Guid.NewGuid().ToString(),
  Question = "Should I create this branch?",
  Context = $"Branch name: {branchName}",
  Step = WorkflowPhase.CreatingBranch,
  Data = new { branchName }
};

await conversationService.RequestApprovalAsync(threadId, approval, CancellationToken.None);

// Wait for approval
var approved = await WaitForApprovalAsync(threadId, approval.Id);
if (!approved) throw new OperationCanceledException("User rejected");
```

## Best Practices

1. **Type Safety**: Let the framework handle routing - don't manually pass data between executors
2. **Conversational Updates**: Use `SendMessageAsync()` to keep users informed
3. **Error Handling**: Catch exceptions and send error messages to the conversation
4. **Logging**: Log all major steps for debugging
5. **Approvals**: Add approval steps for critical operations (branch creation, PR creation, deployment)
6. **AI Agents**: Use `IChatClient` when you need LLM reasoning between executors
7. **Context**: Use `WorkflowContextProvider` to access thread IDs and workflow state

## Example: Full Workflow Implementation

See `ETWDetectorWorkflow.cs` for the complete implementation:

```csharp
public Workflow BuildWorkflow()
{
  // Get all executors from DI
  var aiAgent = serviceProvider.GetRequiredService<AIValidationAgent>();
  var kustoExecutor = serviceProvider.GetRequiredService<KustoQueryExecutor>();
  var branchExecutor = serviceProvider.GetRequiredService<BranchCreationExecutor>();
  var codeGenAgent = serviceProvider.GetRequiredService<CodeGenerationAgent>();
  var prExecutor = serviceProvider.GetRequiredService<PRCreationExecutor>();
  var deploymentExecutor = serviceProvider.GetRequiredService<DeploymentMonitorExecutor>();

  // Build the chain
  var builder = new WorkflowBuilder(aiAgent);
  builder.AddEdge(aiAgent, kustoExecutor);
  builder.AddEdge(kustoExecutor, branchExecutor);
  builder.AddEdge(branchExecutor, codeGenAgent);
  builder.AddEdge(codeGenAgent, prExecutor);
  builder.AddEdge(prExecutor, deploymentExecutor);

  return builder.WithOutputFrom(deploymentExecutor).Build();
}
```

## Running the Workflow

Start the workflow by passing the initial message:

```csharp
var initialMessage = new ETWInput(userId, providerId, ruleId, schemaJson, etwDetails);
var run = await InProcessExecution.StreamAsync(workflow, initialMessage);

await foreach (var evt in run.WatchStreamAsync())
{
  // Handle workflow events
  if (evt is WorkflowOutputEvent output)
  {
    // Workflow completed!
  }
}
```

## Summary

The Microsoft Agent Framework makes it easy to build conversational, multi-step workflows by:
- Automatically routing messages between executors based on types
- Supporting AI agents that use LLMs for reasoning
- Providing human-in-the-loop approvals
- Integrating with your existing conversation service for UI updates

To add a new executor:
1. Create the executor class implementing `IMessageHandler<TIn, TOut>`
2. Register it in DI
3. Add it to the workflow chain with `AddEdge()`

The framework handles the rest!
