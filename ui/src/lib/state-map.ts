import { NodeStatus } from './types';

export interface GraphStatus {
  issue: NodeStatus;
  pr: NodeStatus;
  workflow: NodeStatus;
}

export const initialGraphStatus: GraphStatus = {
  issue: 'idle',
  pr: 'idle',
  workflow: 'idle'
};

export function mapStateToGraph(state: string): GraphStatus {
  if (state.startsWith('Issue')) {
    return { issue: 'success', pr: 'idle', workflow: 'idle' };
  }

  if (state.startsWith('PR')) {
    const done = state === 'PRMerged';
    const failed = state === 'PRClosed';
    return {
      issue: 'success',
      pr: failed ? 'failed' : done ? 'success' : 'running',
      workflow: 'idle'
    };
  }

  if (state.startsWith('Workflow')) {
    return {
      issue: 'success',
      pr: 'success',
      workflow:
        state === 'WorkflowSuccess'
          ? 'success'
          : state === 'WorkflowFailure'
            ? 'failed'
            : 'running'
    };
  }

  return initialGraphStatus;
}
