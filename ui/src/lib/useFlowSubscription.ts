'use client';

import { useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { FlowSummaryDto, FlowUpdatedEvent } from './types';

interface UseFlowSubscriptionArgs {
  flowId: string;
  onFlowBootstrap: (summary: FlowSummaryDto) => void;
  onFlowUpdated: (event: FlowUpdatedEvent) => void;
  hubUrl?: string;
  apiBaseUrl?: string;
}

export function useFlowSubscription({
  flowId,
  onFlowBootstrap,
  onFlowUpdated,
  hubUrl,
  apiBaseUrl
}: UseFlowSubscriptionArgs) {
  useEffect(() => {
    const hubEndpoint = hubUrl ?? process.env.NEXT_PUBLIC_HUB_URL ?? 'http://localhost:5000/hubs/flow';
    const apiEndpoint = apiBaseUrl ?? process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5000';

    let isCancelled = false;
    let latestRealtimeUpdateMs = Number.NEGATIVE_INFINITY;

    const parseTimestamp = (value: string | undefined) => {
      const timestamp = Date.parse(value ?? '');
      return Number.isNaN(timestamp) ? Number.NEGATIVE_INFINITY : timestamp;
    };

    const connection = new signalR.HubConnectionBuilder().withUrl(hubEndpoint).withAutomaticReconnect().build();

    async function bootstrap() {
      try {
        const response = await fetch(`${apiEndpoint}/flows/${encodeURIComponent(flowId)}`);
        if (response.ok) {
          const summary = (await response.json()) as FlowSummaryDto;
          const summaryUpdatedAtMs = parseTimestamp(summary.updatedAt);

          if (!isCancelled && summaryUpdatedAtMs >= latestRealtimeUpdateMs) {
            onFlowBootstrap(summary);
          }
        }
      } catch (error: unknown) {
        console.warn('Unable to bootstrap flow state from API', error);
      }
    }

    connection.on('FlowUpdated', (event: FlowUpdatedEvent) => {
      latestRealtimeUpdateMs = Math.max(
        latestRealtimeUpdateMs,
        parseTimestamp(event.receivedAt),
        parseTimestamp(event.occurredAt)
      );

      if (event.flowId === flowId) {
        onFlowUpdated(event);
      }
    });

    connection
      .start()
      .then(() => connection.invoke('SubscribeFlow', flowId))
      .then(() => bootstrap())
      .catch((error: unknown) => {
        console.error('Unable to connect to SignalR hub', error);
      });

    return () => {
      isCancelled = true;
      connection.stop().catch(() => undefined);
    };
  }, [apiBaseUrl, flowId, hubUrl, onFlowBootstrap, onFlowUpdated]);
}
