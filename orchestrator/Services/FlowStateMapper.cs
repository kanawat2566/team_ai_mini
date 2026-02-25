using System.Text.Json;

namespace Orchestrator.Services;

public class FlowStateMapper
{
    public MappedFlowEvent? Map(string eventType, string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var action = root.TryGetProperty("action", out var actionNode) ? actionNode.GetString() ?? "unknown" : "unknown";
        var repoFullName = root.GetProperty("repository").GetProperty("full_name").GetString() ?? "unknown/unknown";

        return eventType switch
        {
            "issues" => MapIssue(root, action, repoFullName, eventType, payload),
            "pull_request" => MapPullRequest(root, action, repoFullName, eventType, payload),
            "workflow_run" => MapWorkflowRun(root, action, repoFullName, eventType, payload),
            _ => null
        };
    }

    private static MappedFlowEvent MapIssue(JsonElement root, string action, string repo, string eventType, string payload)
    {
        var issueNumber = root.GetProperty("issue").GetProperty("number").GetInt32();
        var state = action == "opened" ? "IssueOpened" : $"Issue{action}";
        return new MappedFlowEvent($"{repo}/issue/{issueNumber}", repo, issueNumber, null, null, eventType, action, state, payload, DateTimeOffset.UtcNow);
    }

    private static MappedFlowEvent MapPullRequest(JsonElement root, string action, string repo, string eventType, string payload)
    {
        var pr = root.GetProperty("pull_request");
        var prNumber = root.GetProperty("number").GetInt32();
        var state = action switch
        {
            "opened" => "PROpened",
            "ready_for_review" => "PRReadyForReview",
            "closed" when pr.GetProperty("merged").GetBoolean() => "PRMerged",
            "closed" => "PRClosed",
            _ => $"PR{action}"
        };

        return new MappedFlowEvent($"{repo}/pr/{prNumber}", repo, null, prNumber, null, eventType, action, state, payload, DateTimeOffset.UtcNow);
    }

    private static MappedFlowEvent MapWorkflowRun(JsonElement root, string action, string repo, string eventType, string payload)
    {
        var run = root.GetProperty("workflow_run");
        var runId = run.GetProperty("id").GetInt64();
        var prNumber = run.TryGetProperty("pull_requests", out var prs) && prs.ValueKind == JsonValueKind.Array && prs.GetArrayLength() > 0
            ? prs[0].GetProperty("number").GetInt32()
            : (int?)null;

        var conclusion = run.TryGetProperty("conclusion", out var conclusionNode)
            ? conclusionNode.GetString()
            : null;

        var state = action switch
        {
            "requested" => "WorkflowRequested",
            "in_progress" => "WorkflowInProgress",
            "completed" when conclusion == "success" => "WorkflowSuccess",
            "completed" => "WorkflowFailure",
            _ => $"Workflow{action}"
        };

        var flowId = prNumber.HasValue ? $"{repo}/pr/{prNumber}" : $"{repo}/workflow/{runId}";
        return new MappedFlowEvent(flowId, repo, null, prNumber, runId, eventType, action, state, payload, DateTimeOffset.UtcNow);
    }
}

public record MappedFlowEvent(
    string FlowId,
    string RepoFullName,
    int? IssueNumber,
    int? PrNumber,
    long? WorkflowRunId,
    string EventType,
    string EventAction,
    string State,
    string PayloadJson,
    DateTimeOffset OccurredAt
);
