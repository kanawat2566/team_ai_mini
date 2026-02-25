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

export interface FlowEventDto {
  id: number;
  eventType: string;
  eventAction: string;
  state: string;
  occurredAt: string;
  receivedAt: string;
}

export interface FlowSummaryDto {
  flowId: string;
  repoFullName: string;
  state: string;
  updatedAt: string;
  timeline: FlowEventDto[];
}
