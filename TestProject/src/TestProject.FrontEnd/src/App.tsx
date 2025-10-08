import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { ConversationalChat } from './components/ConversationalChat';
import './App.css';

function App() {
  return (
    <FluentProvider theme={webLightTheme}>
      <ConversationalChat />
    </FluentProvider>
  );
}

export default App;
