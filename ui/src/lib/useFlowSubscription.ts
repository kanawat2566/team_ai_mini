'use client';

import { useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { FlowUpdatedEvent } from './types';

interface UseFlowSubscriptionArgs {
  flowId: string;
  onFlowUpdated: (event: FlowUpdatedEvent) => void;
  hubUrl?: string;
}

export function useFlowSubscription({ flowId, onFlowUpdated, hubUrl }: UseFlowSubscriptionArgs) {
  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl ?? process.env.NEXT_PUBLIC_HUB_URL ?? 'http://localhost:5000/hubs/flow')
      .withAutomaticReconnect()
      .build();

    connection.on('FlowUpdated', (event: FlowUpdatedEvent) => {
      if (event.flowId === flowId) {
        onFlowUpdated(event);
      }
    });

    connection
      .start()
      .then(() => connection.invoke('SubscribeFlow', flowId))
      .catch((error: unknown) => {
        console.error('Unable to connect to SignalR hub', error);
      });

    return () => {
      connection.stop().catch(() => undefined);
    };
  }, [flowId, hubUrl, onFlowUpdated]);
}
