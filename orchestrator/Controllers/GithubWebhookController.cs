using Microsoft.AspNetCore.Mvc;
using Orchestrator.Services;

namespace Orchestrator.Controllers;

[ApiController]
[Route("webhooks/github")]
public class GithubWebhookController(
    SignatureVerifier signatureVerifier,
    FlowStateMapper mapper,
    FlowEventIngestor ingestor,
    ILogger<GithubWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var eventType = Request.Headers["X-GitHub-Event"].ToString();
        var deliveryId = Request.Headers["X-GitHub-Delivery"].ToString();
        var signature = Request.Headers["X-Hub-Signature-256"].ToString();

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        if (!signatureVerifier.IsValid(signature, payload))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(deliveryId))
        {
            return BadRequest("Missing GitHub headers.");
        }

        var mapped = mapper.Map(eventType, payload);
        if (mapped is null)
        {
            logger.LogInformation("Ignored unsupported event {EventType}", eventType);
            return Accepted(new { ignored = true });
        }

        var ingested = await ingestor.IngestAsync(deliveryId, mapped, cancellationToken);
        if (ingested is null)
        {
            return Ok(new { duplicate = true });
        }

        return Ok(new { ingested = true, mapped.FlowId, mapped.State });
    }
}
