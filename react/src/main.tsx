import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './styles.css';

class ErrorBoundary extends React.Component<{ children: React.ReactNode }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() { return { failed: true }; }
  render() { return this.state.failed ? <main className="error-page"><h1>Pip cần nghỉ một chút.</h1><p>Tiến trình đã lưu vẫn ở đây. Hãy mở lại trò chơi.</p><button className="primary" onClick={() => location.reload()}>Mở lại Wordy Wings</button></main> : this.props.children; }
}
ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><ErrorBoundary><App/></ErrorBoundary></React.StrictMode>);
