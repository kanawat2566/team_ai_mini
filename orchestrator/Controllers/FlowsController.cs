using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orchestrator.Data;
using Orchestrator.Models;

namespace Orchestrator.Controllers;

[ApiController]
[Route("flows")]
public class FlowsController(OrchestratorDbContext db) : ControllerBase
{
    [HttpGet("{flowId}")]
    public async Task<ActionResult<FlowSummaryDto>> GetFlow(string flowId, CancellationToken cancellationToken)
    {
        var timeline = await db.FlowEvents.AsNoTracking()
            .Where(x => x.FlowId == flowId)
            .OrderBy(x => x.ReceivedAt)
            .Select(x => new FlowEventDto(x.Id, x.EventType, x.EventAction, x.State, x.OccurredAt, x.ReceivedAt))
            .ToListAsync(cancellationToken);

        if (timeline.Count == 0)
        {
            return NotFound();
        }

        var last = timeline[^1];
        var repo = await db.FlowEvents.AsNoTracking()
            .Where(x => x.FlowId == flowId)
            .Select(x => x.RepoFullName)
            .FirstAsync(cancellationToken);

        return Ok(new FlowSummaryDto(flowId, repo, last.State, last.ReceivedAt, timeline));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FlowSummaryDto>>> GetLatestByRepo([FromQuery] string repo, CancellationToken cancellationToken)
    {
        var groups = await db.FlowEvents.AsNoTracking()
            .Where(x => x.RepoFullName == repo)
            .GroupBy(x => x.FlowId)
            .Select(g => g.OrderByDescending(x => x.ReceivedAt).First())
            .OrderByDescending(x => x.ReceivedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        var results = groups
            .Select(x => new FlowSummaryDto(
                x.FlowId,
                x.RepoFullName,
                x.State,
                x.ReceivedAt,
                Array.Empty<FlowEventDto>()))
            .ToList();

        return Ok(results);
    }
}
