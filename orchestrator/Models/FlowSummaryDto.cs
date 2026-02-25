namespace Orchestrator.Models;

public record FlowSummaryDto(
    string FlowId,
    string RepoFullName,
    string State,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<FlowEventDto> Timeline
);

public record FlowEventDto(
    long Id,
    string EventType,
    string EventAction,
    string State,
    DateTimeOffset OccurredAt,
    DateTimeOffset ReceivedAt
);
