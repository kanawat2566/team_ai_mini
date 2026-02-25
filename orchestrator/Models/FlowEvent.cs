using System.ComponentModel.DataAnnotations;

namespace Orchestrator.Models;

public class FlowEvent
{
    public long Id { get; set; }

    [MaxLength(128)]
    public required string DeliveryId { get; set; }

    [MaxLength(512)]
    public required string FlowId { get; set; }

    [MaxLength(256)]
    public required string RepoFullName { get; set; }

    public int? IssueNumber { get; set; }

    public int? PrNumber { get; set; }

    public long? WorkflowRunId { get; set; }

    [MaxLength(64)]
    public required string EventType { get; set; }

    [MaxLength(64)]
    public required string EventAction { get; set; }

    [MaxLength(64)]
    public required string State { get; set; }

    public required string PayloadJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }
}
