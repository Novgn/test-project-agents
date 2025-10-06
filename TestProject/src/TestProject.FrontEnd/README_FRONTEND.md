# TestProject Frontend

A React-based frontend application for the AI Agent Workflow system, built with Vite, TypeScript, and Fluent UI.

## Features

- **Real-time Communication**: SignalR integration for live workflow updates
- **Chat Interface**: Fluent UI-based chat interface for agent interaction
- **Workflow Management**: Start, monitor, and approve AI agent workflows
- **ETW Detector Creation**: Guide users through automated detector creation process

## Technologies

- **React** - UI library
- **TypeScript** - Type-safe development
- **Vite** - Fast build tool and dev server
- **Fluent UI** - Microsoft's design system
- **SignalR** - Real-time communication
- **Axios** - HTTP client

## Getting Started

### Prerequisites

- Node.js 18+ and npm

### Installation

```bash
npm install
```

### Development

```bash
npm run dev
```

The application will be available at `http://localhost:5173`

### Build

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

## Project Structure

```
src/
├── components/
│   ├── WorkflowChat.tsx    # Main chat interface component
│   └── ...
├── services/
│   └── signalRService.ts   # SignalR connection service
├── App.tsx                  # Main app component
└── main.tsx                 # Entry point
```

## Usage

1. Start the backend API server (TestProject.Web)
2. Run the frontend dev server
3. Enter ETW details in the chat interface
4. The AI agents will automatically process the workflow
5. Approve or reject changes when prompted

## Configuration

Update the API URL in `signalRService.ts` if your backend is running on a different port:

```typescript
.withUrl('http://localhost:5000/hubs/workflow', {
  withCredentials: true
})
```
