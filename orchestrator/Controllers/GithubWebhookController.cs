using System.Text.Json;
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
            logger.LogWarning("Invalid webhook signature for delivery {DeliveryId}", deliveryId);
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(deliveryId))
        {
            return BadRequest("Missing GitHub headers.");
        }

        MappedFlowEvent? mapped;
        try
        {
            mapped = mapper.Map(eventType, payload);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid JSON payload for delivery {DeliveryId}", deliveryId);
            return BadRequest("Invalid JSON payload.");
        }

        if (mapped is null)
        {
            logger.LogInformation("Ignored unsupported/invalid event {EventType} for delivery {DeliveryId}", eventType, deliveryId);
            return Accepted(new { ignored = true });
        }

        var ingested = await ingestor.IngestAsync(deliveryId, mapped, cancellationToken);
        if (ingested is null)
        {
            return Ok(new { duplicate = true });
        }

        logger.LogInformation("Ingested {EventType}.{Action} for {FlowId}", mapped.EventType, mapped.EventAction, mapped.FlowId);
        return Ok(new { ingested = true, mapped.FlowId, mapped.State });
    }
}
