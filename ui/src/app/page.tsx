'use client';

import { useState } from 'react';
import { FlowGraph } from '../components/FlowGraph';

export default function HomePage() {
  const [flowId, setFlowId] = useState('owner/repo/pr/1');

  return (
    <main className="container">
      <h1>DevOps Loop Tracker</h1>
      <p>Track GitHub Issue, Pull Request, and Workflow status in realtime via SignalR.</p>

      <label htmlFor="flowId">Flow ID</label>
      <input
        id="flowId"
        value={flowId}
        onChange={(event) => setFlowId(event.target.value)}
        placeholder="owner/repo/pr/123"
      />

      <FlowGraph flowId={flowId} />
    </main>
  );
}
