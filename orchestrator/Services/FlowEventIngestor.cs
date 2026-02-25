using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Orchestrator.Data;
using Orchestrator.Hubs;
using Orchestrator.Models;

namespace Orchestrator.Services;

public class FlowEventIngestor(
    OrchestratorDbContext db,
    IHubContext<FlowHub> hubContext,
    ILogger<FlowEventIngestor> logger)
{
    public async Task<FlowEvent?> IngestAsync(string deliveryId, MappedFlowEvent mapped, CancellationToken cancellationToken)
    {
        var entity = new FlowEvent
        {
            DeliveryId = deliveryId,
            FlowId = mapped.FlowId,
            RepoFullName = mapped.RepoFullName,
            IssueNumber = mapped.IssueNumber,
            PrNumber = mapped.PrNumber,
            WorkflowRunId = mapped.WorkflowRunId,
            EventType = mapped.EventType,
            EventAction = mapped.EventAction,
            State = mapped.State,
            PayloadJson = mapped.PayloadJson,
            OccurredAt = mapped.OccurredAt,
            ReceivedAt = DateTimeOffset.UtcNow
        };

        db.FlowEvents.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            var duplicate = await db.FlowEvents.AsNoTracking()
                .AnyAsync(x => x.DeliveryId == deliveryId, cancellationToken);

            if (duplicate)
            {
                logger.LogInformation("Duplicate delivery detected during save: {DeliveryId}", deliveryId);
                return null;
            }

            throw new InvalidOperationException($"Failed to persist delivery {deliveryId}", ex);
        }

        var payload = new
        {
            entity.FlowId,
            entity.RepoFullName,
            entity.State,
            entity.EventType,
            entity.EventAction,
            entity.OccurredAt,
            entity.ReceivedAt
        };

        await hubContext.Clients.Group(FlowHub.FlowGroup(mapped.FlowId)).SendAsync("FlowUpdated", payload, cancellationToken);
        await hubContext.Clients.Group(FlowHub.RepoGroup(mapped.RepoFullName)).SendAsync("FlowUpdated", payload, cancellationToken);

        return entity;
    }
}
