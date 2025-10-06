import * as signalR from '@microsoft/signalr';

export interface WorkflowUpdate {
  workflowRunId: string;
  stepName: string;
  status: string;
  message: string;
  timestamp: string;
  metadata?: Record<string, any>;
}

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private listeners: Map<string, ((update: WorkflowUpdate) => void)[]> = new Map();

  async connect(): Promise<void> {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:5000/hubs/workflow', {
        withCredentials: true
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.connection.on('WorkflowUpdate', (update: WorkflowUpdate) => {
      const eventListeners = this.listeners.get('WorkflowUpdate') || [];
      eventListeners.forEach(listener => listener(update));
    });

    this.connection.on('Connected', (connectionId: string) => {
      console.log('Connected to SignalR hub:', connectionId);
    });

    this.connection.on('Error', (message: string) => {
      console.error('SignalR error:', message);
    });

    try {
      await this.connection.start();
      console.log('SignalR connection established');
    } catch (error) {
      console.error('Error connecting to SignalR:', error);
      throw error;
    }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  async subscribeToWorkflow(workflowId: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }
    await this.connection.invoke('SubscribeToWorkflow', workflowId);
  }

  async unsubscribeFromWorkflow(workflowId: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }
    await this.connection.invoke('UnsubscribeFromWorkflow', workflowId);
  }

  async approveStep(workflowId: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }
    await this.connection.invoke('ApproveStep', workflowId);
  }

  async rejectStep(workflowId: string, reason: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }
    await this.connection.invoke('RejectStep', workflowId, reason);
  }

  async sendMessage(message: string): Promise<void> {
    if (!this.connection) {
      throw new Error('SignalR connection not established');
    }
    await this.connection.invoke('SendMessage', message);
  }

  onWorkflowUpdate(callback: (update: WorkflowUpdate) => void): () => void {
    if (!this.listeners.has('WorkflowUpdate')) {
      this.listeners.set('WorkflowUpdate', []);
    }
    this.listeners.get('WorkflowUpdate')!.push(callback);

    // Return unsubscribe function
    return () => {
      const listeners = this.listeners.get('WorkflowUpdate') || [];
      const index = listeners.indexOf(callback);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    };
  }

  getConnectionState(): signalR.HubConnectionState {
    return this.connection?.state || signalR.HubConnectionState.Disconnected;
  }
}

export const signalRService = new SignalRService();
