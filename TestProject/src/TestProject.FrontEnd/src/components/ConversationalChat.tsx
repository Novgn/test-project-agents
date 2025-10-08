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
import { Send24Regular, CheckmarkCircle24Regular, DismissCircle24Regular } from '@fluentui/react-icons';
import {
  workflowService,
  type ConversationMessage,
  type ApprovalRequest,
} from '../services/workflowService';

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
  approvalMessage: {
    alignSelf: 'flex-start',
    backgroundColor: tokens.colorPaletteYellowBackground2,
    borderLeft: `4px solid ${tokens.colorPaletteYellowBorder2}`,
    maxWidth: '90%',
  },
  approvalButtons: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
    marginTop: tokens.spacingVerticalS,
  },
  inputContainer: {
    display: 'flex',
    gap: tokens.spacingHorizontalM,
  },
  contextText: {
    fontSize: '0.85rem',
    color: tokens.colorNeutralForeground3,
    marginTop: tokens.spacingVerticalXS,
  },
});

interface DisplayMessage extends ConversationMessage {
  isWaitingForApproval?: boolean;
}

export const ConversationalChat = () => {
  const styles = useStyles();
  const [messages, setMessages] = useState<DisplayMessage[]>([]);
  const [inputValue, setInputValue] = useState('');
  const [threadId, setThreadId] = useState<string | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [pendingApproval, setPendingApproval] = useState<ApprovalRequest | null>(null);
  const chatEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const handleConversationMessage = (message: ConversationMessage) => {
    console.log('Conversation message:', message);

    // Check if this is an approval request
    if (message.type === 'ApprovalRequest' && message.data) {
      const approval = message.data as ApprovalRequest;
      setPendingApproval(approval);
      setMessages(prev => [...prev, { ...message, isWaitingForApproval: true }]);
    } else {
      setMessages(prev => [...prev, message]);
    }
  };

  const handleSendMessage = async () => {
    if (!inputValue.trim()) return;

    const userMessage = inputValue.trim();
    setInputValue('');

    try {
      if (!threadId) {
        // First message - start new chat
        setIsLoading(true);
        const newThreadId = await workflowService.startChat('user123', '');
        setThreadId(newThreadId);

        // Start streaming conversation messages via SignalR
        setIsConnected(true);
        await workflowService.streamConversationMessages(
          newThreadId,
          handleConversationMessage,
          (error) => {
            console.error('SignalR Error:', error);
            setIsConnected(false);
            setIsLoading(false);
          }
        );

        // Now send the first user message (will be received via SignalR)
        await workflowService.sendMessage(newThreadId, userMessage);
        setIsLoading(false);
      } else {
        // Subsequent messages - send to existing chat (will be received via SignalR)
        await workflowService.sendMessage(threadId, userMessage);
      }
    } catch (error) {
      console.error('Error in chat:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';

      const errorMsg: DisplayMessage = {
        id: Date.now().toString(),
        type: 'Error',
        content: `Error: ${errorMessage}`,
        timestamp: new Date().toISOString(),
      };
      setMessages(prev => [...prev, errorMsg]);
      setIsLoading(false);
      setIsConnected(false);
    }
  };

  const handleApproval = async (approved: boolean, feedback?: string) => {
    if (!pendingApproval || !threadId) return;

    try {
      await workflowService.sendApprovalResponse(
        threadId,
        pendingApproval.id,
        approved,
        feedback
      );

      // Clear pending approval
      setPendingApproval(null);

      // Update the message to show it's no longer waiting for approval
      setMessages(prev =>
        prev.map(msg =>
          msg.data?.id === pendingApproval.id
            ? { ...msg, isWaitingForApproval: false }
            : msg
        )
      );
    } catch (error) {
      console.error('Error sending approval:', error);
    }
  };

  const renderMessage = (message: DisplayMessage) => {
    const isUser = message.type === 'UserResponse';
    const isApproval = message.type === 'ApprovalRequest';
    const isError = message.type === 'Error';

    let className = styles.message;
    if (isUser) className += ' ' + styles.userMessage;
    else if (isApproval) className += ' ' + styles.approvalMessage;
    else className += ' ' + styles.agentMessage;

    return (
      <div key={message.id} className={className}>
        <Text size={400} weight={isApproval ? 'semibold' : 'regular'}>
          {message.content}
        </Text>

        {isApproval && message.data && (
          <>
            <Text className={styles.contextText}>{message.data.context}</Text>
            {message.isWaitingForApproval && (
              <div className={styles.approvalButtons}>
                <Button
                  appearance="primary"
                  icon={<CheckmarkCircle24Regular />}
                  onClick={() => handleApproval(true)}
                >
                  Approve
                </Button>
                <Button
                  appearance="secondary"
                  icon={<DismissCircle24Regular />}
                  onClick={() => handleApproval(false, 'Not approved')}
                >
                  Reject
                </Button>
              </div>
            )}
          </>
        )}

        {isError && (
          <Text size={300} style={{ color: tokens.colorPaletteRedForeground1 }}>
            ⚠️ Error
          </Text>
        )}
      </div>
    );
  };

  return (
    <div className={styles.container}>
      <CardHeader
        header={<Text size={600}>ETW Detector Assistant</Text>}
        description={
          <Text size={300}>
            {isConnected ? '🟢 Connected' : '🔴 Disconnected'}
            {threadId && ` | Chat: ${threadId.substring(0, 8)}...`}
          </Text>
        }
      />

      <Card className={styles.chatArea}>
        <div className={styles.messageContainer}>
          {messages.map(renderMessage)}
          {isLoading && !messages.length && <Spinner label="Starting conversation..." />}
          <div ref={chatEndRef} />
        </div>
      </Card>

      <div className={styles.inputContainer}>
        <Input
          placeholder={threadId ? "Type your message..." : "Start chatting with the AI assistant..."}
          value={inputValue}
          onChange={(_, data) => setInputValue(data.value)}
          onKeyPress={e => e.key === 'Enter' && !isLoading && handleSendMessage()}
          disabled={isLoading}
          style={{ flex: 1 }}
        />
        <Button
          appearance="primary"
          icon={<Send24Regular />}
          onClick={handleSendMessage}
          disabled={isLoading || !inputValue.trim()}
        >
          Send
        </Button>
      </div>
    </div>
  );
};
