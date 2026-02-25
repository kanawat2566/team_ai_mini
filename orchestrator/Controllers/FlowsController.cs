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
        var records = await db.FlowEvents.AsNoTracking()
            .Where(x => x.FlowId == flowId)
            .OrderBy(x => x.ReceivedAt)
            .Select(x => new
            {
                x.Id,
                x.EventType,
                x.EventAction,
                x.State,
                x.OccurredAt,
                x.ReceivedAt,
                x.RepoFullName
            })
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
        {
            return NotFound();
        }

        var timeline = records
            .Select(x => new FlowEventDto(x.Id, x.EventType, x.EventAction, x.State, x.OccurredAt, x.ReceivedAt))
            .ToList();

        var last = records[^1];
        return Ok(new FlowSummaryDto(flowId, last.RepoFullName, last.State, last.ReceivedAt, timeline));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FlowSummaryDto>>> GetLatestByRepo([FromQuery] string repo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(repo))
        {
            return BadRequest("Query string 'repo' is required.");
        }

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
