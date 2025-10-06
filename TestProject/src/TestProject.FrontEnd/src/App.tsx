import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { WorkflowChat } from './components/WorkflowChat';
import './App.css';

function App() {
  return (
    <FluentProvider theme={webLightTheme}>
      <WorkflowChat />
    </FluentProvider>
  );
}

export default App;
