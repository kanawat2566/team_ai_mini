export type NodeStatus = 'idle' | 'running' | 'success' | 'failed';

export interface FlowUpdatedEvent {
  flowId: string;
  repoFullName: string;
  state: string;
  eventType: string;
  eventAction: string;
  occurredAt: string;
  receivedAt: string;
}
