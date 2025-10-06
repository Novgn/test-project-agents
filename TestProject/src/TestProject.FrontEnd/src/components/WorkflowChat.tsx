import { useState, useEffect, useRef } from 'react';
import {
  Card,
  CardHeader,
  Input,
  Button,
  makeStyles,
  tokens,
  Text,
  Spinner,
} from '@fluentui/react-components';
import { Send24Regular } from '@fluentui/react-icons';
import { workflowService, type WorkflowEvent } from '../services/workflowService';

const useStyles = makeStyles({
  container: {
    display: 'flex',
    flexDirection: 'column',
    height: '100vh',
    width: '100%',
    padding: tokens.spacingVerticalM,
    boxSizing: 'border-box',
  },
  chatArea: {
    flex: 1,
    overflowY: 'auto',
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    borderRadius: tokens.borderRadiusMedium,
    marginBottom: tokens.spacingVerticalM,
  },
  messageContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalM,
  },
  message: {
    padding: tokens.spacingVerticalM,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground3,
    maxWidth: '80%',
  },
  userMessage: {
    alignSelf: 'flex-end',
    backgroundColor: tokens.colorBrandBackground,
    color: tokens.colorNeutralForegroundOnBrand,
  },
  agentMessage: {
    alignSelf: 'flex-start',
  },
  inputContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  eventBadge: {
    fontSize: '0.75rem',
    padding: '2px 8px',
    borderRadius: tokens.borderRadiusSmall,
    backgroundColor: tokens.colorNeutralBackground5,
    marginTop: tokens.spacingVerticalXS,
  },
});

interface Message {
  id: string;
  text: string;
  sender: 'user' | 'agent';
  timestamp: Date;
  eventType?: string;
}

export const WorkflowChat = () => {
  const styles = useStyles();
  const [messages, setMessages] = useState<Message[]>([]);
  const [inputValue, setInputValue] = useState('');
  const [workflowId, setWorkflowId] = useState<string | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const chatEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const addMessage = (text: string, sender: 'user' | 'agent', eventType?: string) => {
    const newMessage: Message = {
      id: Date.now().toString(),
      text,
      sender,
      timestamp: new Date(),
      eventType,
    };
    setMessages(prev => [...prev, newMessage]);
  };

  const handleWorkflowEvent = (event: WorkflowEvent) => {
    console.log('Workflow event:', event);

    let message = '';
    switch (event.type) {
      case 'WorkflowStartedEvent':
        message = '🚀 Workflow started...';
        break;
      case 'ExecutorInvokeEvent':
        message = `⚙️ Executing: ${event.data.executor}`;
        break;
      case 'ExecutorCompleteEvent':
        message = `✅ Completed: ${event.data.executor}`;
        break;
      case 'WorkflowOutputEvent':
        message = event.data.output || 'Workflow completed';
        setIsLoading(false);
        setIsConnected(false);
        break;
      case 'WorkflowErrorEvent':
        message = `❌ Error: ${event.data.message}`;
        setIsLoading(false);
        setIsConnected(false);
        break;
      default:
        message = event.data.message || `Event: ${event.type}`;
    }

    addMessage(message, 'agent', event.type);
  };

  const handleSendMessage = async () => {
    if (!inputValue.trim()) return;

    const userMessage = inputValue.trim();
    addMessage(userMessage, 'user');
    setInputValue('');
    setIsLoading(true);

    try {
      // Start new workflow
      const newWorkflowId = await workflowService.startWorkflow('user123', userMessage);
      setWorkflowId(newWorkflowId);

      addMessage('Workflow started. Streaming events...', 'agent');

      // Start streaming events
      setIsConnected(true);
      workflowService.streamWorkflowEvents(
        newWorkflowId,
        handleWorkflowEvent,
        (error) => {
          console.error('SSE Error:', error);
          addMessage('Connection lost. Workflow may still be running.', 'agent');
          setIsConnected(false);
          setIsLoading(false);
        }
      );
    } catch (error) {
      console.error('Error starting workflow:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      addMessage(`Error: ${errorMessage}`, 'agent');
      setIsLoading(false);
    }
  };

  return (
    <div className={styles.container}>
      <CardHeader
        header={<Text size={600}>AI Agent Workflow (SSE)</Text>}
        description={
          <Text size={300}>
            {isConnected ? '🟢 Streaming' : '🔴 Disconnected'}
            {workflowId && ` | Workflow ID: ${workflowId.substring(0, 8)}...`}
          </Text>
        }
      />

      <Card className={styles.chatArea}>
        <div className={styles.messageContainer}>
          {messages.map(message => (
            <div
              key={message.id}
              className={`${styles.message} ${
                message.sender === 'user' ? styles.userMessage : styles.agentMessage
              }`}
            >
              <Text size={400}>{message.text}</Text>
              {message.eventType && (
                <div className={styles.eventBadge}>
                  {message.eventType}
                </div>
              )}
            </div>
          ))}
          {isLoading && <Spinner label="Processing workflow..." />}
          <div ref={chatEndRef} />
        </div>
      </Card>

      <div className={styles.inputContainer}>
        <Input
          placeholder="Enter ETW provider details..."
          value={inputValue}
          onChange={(_, data) => setInputValue(data.value)}
          onKeyPress={e => e.key === 'Enter' && handleSendMessage()}
          disabled={isLoading}
          style={{ flex: 1 }}
        />
        <Button
          appearance="primary"
          icon={<Send24Regular />}
          onClick={handleSendMessage}
          disabled={isLoading || !inputValue.trim()}
        >
          Start Workflow
        </Button>
      </div>
    </div>
  );
};
