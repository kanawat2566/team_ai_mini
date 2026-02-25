'use client';

import { useCallback, useMemo, useState } from 'react';
import type { NodeEditor } from 'rete';
import { mapStateToGraph } from '../lib/state-map';
import { FlowSummaryDto, FlowUpdatedEvent, NodeStatus } from '../lib/types';
import { useFlowSubscription } from '../lib/useFlowSubscription';

const statusIcon: Record<NodeStatus, string> = {
  idle: '⬜',
  running: '🔄',
  success: '✅',
  failed: '❌'
};

export function FlowGraph({ flowId }: { flowId: string }) {
  const [_retePlaceholder] = useState<NodeEditor | null>(null);
  const [latestEvent, setLatestEvent] = useState<FlowUpdatedEvent | null>(null);
  const [latestState, setLatestState] = useState('');

  const onFlowBootstrap = useCallback((summary: FlowSummaryDto) => {
    setLatestState(summary.state);
    const lastEvent = summary.timeline.at(-1);
    if (lastEvent) {
      setLatestEvent({
        flowId: summary.flowId,
        repoFullName: summary.repoFullName,
        state: lastEvent.state,
        eventType: lastEvent.eventType,
        eventAction: lastEvent.eventAction,
        occurredAt: lastEvent.occurredAt,
        receivedAt: lastEvent.receivedAt
      });
    }
  }, []);

  const onFlowUpdated = useCallback((event: FlowUpdatedEvent) => {
    setLatestEvent(event);
    setLatestState(event.state);
  }, []);

  useFlowSubscription({
    flowId,
    onFlowBootstrap,
    onFlowUpdated
  });

  const status = useMemo(() => mapStateToGraph(latestState), [latestState]);
  const nodes = [
    { key: 'issue', title: 'Issue', status: status.issue },
    { key: 'pr', title: 'Pull Request', status: status.pr },
    { key: 'workflow', title: 'Workflow Run', status: status.workflow }
  ];

  return (
    <section>
      <p>Rete.js-ready graph (MVP rendering uses lightweight node cards first).</p>
      <div className="graph-row">
        {nodes.map((node, index) => (
          <div className="node" key={node.key}>
            <h3>{node.title}</h3>
            <p className={`status ${node.status}`}>
              {statusIcon[node.status]} {node.status}
            </p>
            {index < nodes.length - 1 && <span className="arrow">→</span>}
          </div>
        ))}
      </div>
      <div className="log-box">
        <strong>Latest Event:</strong>{' '}
        {latestEvent ? `${latestEvent.eventType}.${latestEvent.eventAction} -> ${latestEvent.state}` : 'No events yet'}
      </div>
    </section>
  );
}
