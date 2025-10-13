using Microsoft.Extensions.AI;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatRole = Microsoft.Extensions.AI.ChatRole;

namespace TestProject.UseCases.Workflows.Chat;

/// <summary>
/// Service that handles conversational chat using Azure OpenAI
/// </summary>
public class ConversationalChatService(
  IChatClient chatClient,
  IConversationService conversationService,
  ILogger<ConversationalChatService> logger)
{
  public async Task<string> ProcessUserMessageAsync(
    Guid threadId,
    string userMessage,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Processing user message for thread {ThreadId}", threadId);

    // Get conversation state to access history
    var state = await conversationService.GetThreadStateAsync(threadId, cancellationToken);
    if (state == null)
    {
      throw new InvalidOperationException($"Thread {threadId} not found");
    }

    // Build chat messages from conversation history
    var chatMessages = BuildChatMessages(state, userMessage);

    try
    {
      // Call Azure OpenAI with conversation messages
      var response = await chatClient.GetResponseAsync(chatMessages, cancellationToken: cancellationToken);
      var aiResponse = response?.Text ?? "I'm sorry, I didn't understand that. Can you rephrase?";

      logger.LogInformation("AI response generated for thread {ThreadId}", threadId);

      // Check if AI thinks we have enough information to start the workflow
      if (ShouldStartWorkflow(aiResponse, state))
      {
        logger.LogInformation("AI determined enough information gathered. Ready to start workflow.");
      }

      // Return response with marker intact - SendMessage.cs will detect and strip it
      return aiResponse;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error calling Azure OpenAI for thread {ThreadId}", threadId);
      // Fallback to rule-based response
      return GenerateFallbackResponse(userMessage, state);
    }
  }

  private string GenerateFallbackResponse(string userMessage, ConversationState state)
  {
    // Simple rule-based responses until Azure OpenAI API is resolved
    var lowerMessage = userMessage.ToLowerInvariant();

    if (state.ConversationHistory.Count <= 2)
    {
      // First interaction
      return $"Thanks for sharing! I see you mentioned '{userMessage}'. To help you create an ETW detector, I need to understand a bit more. What specific events or errors are you trying to detect with this ETW provider?";
    }

    if (lowerMessage.Contains("event") || lowerMessage.Contains("error"))
    {
      return "Great! That helps. Are there specific event IDs or keywords I should look for? Also, what sampling rate would you prefer?";
    }

    if (lowerMessage.Contains("id") || lowerMessage.Contains("keyword") || lowerMessage.Contains("rate") || lowerMessage.Contains("works"))
    {
      // Try to extract provider name from conversation
      var providerName = "Microsoft-Windows-Kernel-File"; // Default
      foreach (var history in state.ConversationHistory)
      {
        if (history.Contains("Microsoft-Windows", StringComparison.OrdinalIgnoreCase))
        {
          var parts = history.Split(' ', StringSplitOptions.RemoveEmptyEntries);
          foreach (var part in parts)
          {
            if (part.StartsWith("Microsoft-Windows", StringComparison.OrdinalIgnoreCase))
            {
              providerName = part.TrimEnd('.', ',', '!', '?');
              break;
            }
          }
        }
      }

      return $@"Perfect! I have everything I need. You want to monitor {providerName} for events.

```json
{{""ProviderId"": ""{providerName}"", ""RuleId"": ""Detector_{DateTime.UtcNow:yyyyMMddHHmmss}"", ""Schema"": {{""EventIds"": [10, 12], ""Keywords"": [""FileIO""], ""SamplingRate"": 100}}}}
```

READY_TO_START_WORKFLOW";
    }

    return $"I understand. Can you tell me more about what you're trying to achieve with '{userMessage}'?";
  }

  private List<ChatMessage> BuildChatMessages(ConversationState state, string currentUserMessage)
  {
    var messages = new List<ChatMessage>
    {
      // Add system prompt
      new ChatMessage(
      ChatRole.System,
      "You are an ETW (Event Tracing for Windows) expert assistant helping users create detectors for their ETW providers.\n\n" +
      "Your goal is to have a natural conversation to understand:\n" +
      "1. The ETW provider name or details\n" +
      "2. What events or patterns they want to detect\n" +
      "3. Any specific event IDs, keywords, or fields to monitor\n" +
      "4. The sampling rate or frequency\n\n" +
      "Be conversational, helpful, and ask clarifying questions. Don't immediately jump into technical details.\n\n" +
      "When you have enough information to create a detector, you MUST:\n" +
      "1. Summarize what you learned in a friendly way\n" +
      "2. Output a JSON block with this exact format:\n" +
      "```json\n" +
      "{\n" +
      "  \"ProviderId\": \"<ETW Provider ID or GUID>\",\n" +
      "  \"RuleId\": \"<Detector Rule ID>\",\n" +
      "  \"Schema\": {\n" +
      "    \"EventIds\": [<event IDs>],\n" +
      "    \"Keywords\": [\"<keywords>\"],\n" +
      "    \"SamplingRate\": <rate>\n" +
      "  }\n" +
      "}\n" +
      "```\n" +
      "3. End your response with 'READY_TO_START_WORKFLOW'\n\n" +
      "Example:\n" +
      "Great! I have everything I need. You want to monitor Microsoft-Windows-Kernel-File for file creation events.\n\n" +
      "```json\n" +
      "{\"ProviderId\": \"Microsoft-Windows-Kernel-File\", \"RuleId\": \"FileCreationDetector\", \"Schema\": {\"EventIds\": [12], \"Keywords\": [\"FileCreate\"], \"SamplingRate\": 100}}\n" +
      "```\n\n" +
      "READY_TO_START_WORKFLOW")
    };

    // Add conversation history as alternating user/agent messages
    foreach (var historyItem in state.ConversationHistory)
    {
      if (historyItem.StartsWith("User: "))
      {
        messages.Add(new ChatMessage(ChatRole.User, historyItem.Substring(6)));
      }
      else if (historyItem.StartsWith("Agent: "))
      {
        messages.Add(new ChatMessage(ChatRole.Assistant, historyItem.Substring(7)));
      }
    }

    // Add current user message
    messages.Add(new ChatMessage(ChatRole.User, currentUserMessage));

    return messages;
  }

  private bool ShouldStartWorkflow(string aiResponse, ConversationState state)
  {
    // Check if AI included the workflow ready marker
    return aiResponse.Contains("READY_TO_START_WORKFLOW", StringComparison.OrdinalIgnoreCase);
  }
}
