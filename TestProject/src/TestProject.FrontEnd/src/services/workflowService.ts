export interface WorkflowEvent {
  type: string;
  timestamp: string;
  data: {
    message?: string;
    output?: string;
    executor?: string;
  };
}

export class WorkflowService {
  private eventSource: EventSource | null = null;

  /**
   * Start a new workflow
   */
  async startWorkflow(userId: string, etwDetails: string): Promise<string> {
    try {
      const response = await fetch('http://localhost:5000/api/workflows/start', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          userId,
          etwDetails,
        }),
      });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('Server error:', errorText);
        throw new Error(`Failed to start workflow: ${response.status} ${response.statusText}`);
      }

      const data = await response.json();
      return data.workflowId; // The workflow ID
    } catch (error) {
      console.error('Error starting workflow:', error);
      throw error;
    }
  }

  /**
   * Stream workflow events using Server-Sent Events
   */
  streamWorkflowEvents(
    workflowId: string,
    onEvent: (event: WorkflowEvent) => void,
    onError?: (error: Event) => void
  ): () => void {
    // Close existing connection if any
    this.disconnect();

    // Create new EventSource connection
    this.eventSource = new EventSource(
      `http://localhost:5000/api/workflows/${workflowId}/stream`
    );

    this.eventSource.onmessage = (event) => {
      try {
        const workflowEvent: WorkflowEvent = JSON.parse(event.data);
        onEvent(workflowEvent);
      } catch (error) {
        console.error('Failed to parse event:', error);
      }
    };

    this.eventSource.onerror = (error) => {
      console.error('EventSource error:', error);
      if (onError) {
        onError(error);
      }
      this.disconnect();
    };

    // Return cleanup function
    return () => this.disconnect();
  }

  /**
   * Disconnect from SSE stream
   */
  disconnect(): void {
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }
  }
}

export const workflowService = new WorkflowService();
