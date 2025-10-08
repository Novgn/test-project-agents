import * as signalR from '@microsoft/signalr';

export interface WorkflowEvent {
  type: string;
  timestamp: string;
  data: {
    message?: string;
    output?: string;
    executor?: string;
  };
}

export interface ConversationMessage {
  id: string;
  type: string;
  content: string;
  data?: any;
  timestamp: string;
}

export interface ApprovalRequest {
  id: string;
  question: string;
  context: string;
  data?: any;
  step: string;
}

export class WorkflowService {
  private connection: signalR.HubConnection | null = null;

  /**
   * Start a new conversational chat session
   */
  async startChat(userId: string, initialMessage: string): Promise<string> {
    try {
      const response = await fetch('/api/chat/start', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          userId,
          initialMessage,
        }),
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Server error:', errorText);
        throw new Error(`Failed to start chat: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      return data.threadId; // The thread ID
    } catch (error) {
      console.error('Error starting chat:', error);
      throw error;
    }
  }

  /**
   * Send a message in an existing chat conversation
   */
  async sendMessage(threadId: string, message: string): Promise<string> {
    try {
      const response = await fetch('/api/chat/send', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          threadId,
          message,
        }),
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Server error:', errorText);
        throw new Error(`Failed to send message: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      return data.response; // The AI response
    } catch (error) {
      console.error('Error sending message:', error);
      throw error;
    }
  }

  /**
   * Stream conversation messages using SignalR
   */
  async streamConversationMessages(
    threadId: string,
    onMessage: (message: ConversationMessage) => void,
    onError?: (error: any) => void
  ): Promise<() => void> {
    // Close existing connection if any
    await this.disconnect();

    // Create new SignalR connection
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/conversation')
      .withAutomaticReconnect()
      .build();

    // Set up message handler
    this.connection.on('ReceiveMessage', (message: ConversationMessage) => {
      console.log('Received message from SignalR:', message);
      onMessage(message);
    });

    // Handle connection errors
    this.connection.onclose((error) => {
      console.error('SignalR connection closed:', error);
      if (onError && error) {
        onError(error);
      }
    });

    try {
      // Start the connection
      await this.connection.start();
      console.log('SignalR connected successfully');

      // Join the thread group to receive messages (using threadId as the group)
      await this.connection.invoke('JoinWorkflow', threadId);
      console.log(`Joined thread group: ${threadId}`);
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
      if (onError) {
        onError(error);
      }
    }

    // Return cleanup function
    return () => this.disconnect();
  }

  /**
   * Disconnect from SignalR
   */
  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('SignalR disconnected');
      } catch (error) {
        console.error('Error disconnecting SignalR:', error);
      }
      this.connection = null;
    }
  }

  /**
   * Send approval response
   */
  async sendApprovalResponse(
    workflowId: string,
    approvalId: string,
    approved: boolean,
    feedback?: string
  ): Promise<void> {
    try {
      const response = await fetch(
        `/api/workflows/${workflowId}/approve`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify({
            workflowId,
            approvalId,
            approved,
            feedback,
          }),
        }
      );

      if (!response.ok) {
        throw new Error(`Failed to send approval: ${response.status} ${response.statusText}`);
      }
    } catch (error) {
      console.error('Error sending approval:', error);
      throw error;
    }
  }
}

export const workflowService = new WorkflowService();
