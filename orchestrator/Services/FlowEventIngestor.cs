using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Orchestrator.Data;
using Orchestrator.Hubs;
using Orchestrator.Models;

namespace Orchestrator.Services;

public class FlowEventIngestor(
    OrchestratorDbContext db,
    IHubContext<FlowHub> hubContext)
{
    public async Task<FlowEvent?> IngestAsync(string deliveryId, MappedFlowEvent mapped, CancellationToken cancellationToken)
    {
        var existing = await db.FlowEvents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.DeliveryId == deliveryId, cancellationToken);

        if (existing is not null)
        {
            return null;
        }

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
        await db.SaveChangesAsync(cancellationToken);

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
