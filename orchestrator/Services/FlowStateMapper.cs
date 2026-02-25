using System.Text.Json;

namespace Orchestrator.Services;

public class FlowStateMapper
{
    public MappedFlowEvent? Map(string eventType, string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        if (!TryGetRepoFullName(root, out var repoFullName))
        {
            return null;
        }

        var action = TryGetString(root, "action") ?? "unknown";

        return eventType switch
        {
            "issues" => MapIssue(root, action, repoFullName, eventType, payload),
            "pull_request" => MapPullRequest(root, action, repoFullName, eventType, payload),
            "workflow_run" => MapWorkflowRun(root, action, repoFullName, eventType, payload),
            _ => null
        };
    }

    private static MappedFlowEvent? MapIssue(JsonElement root, string action, string repo, string eventType, string payload)
    {
        if (!TryGetNestedInt(root, out var issueNumber, "issue", "number"))
        {
            return null;
        }

        var state = action == "opened" ? "IssueOpened" : $"Issue{action}";
        var occurredAt = TryGetNestedDateTimeOffset(root, "issue", "updated_at")
            ?? TryGetNestedDateTimeOffset(root, "issue", "created_at")
            ?? DateTimeOffset.UtcNow;

        return new MappedFlowEvent($"{repo}/issue/{issueNumber}", repo, issueNumber, null, null, eventType, action, state, payload, occurredAt);
    }

    private static MappedFlowEvent? MapPullRequest(JsonElement root, string action, string repo, string eventType, string payload)
    {
        if (!TryGetInt(root, out var prNumber, "number") ||
            !TryGetNestedElement(root, out var pr, "pull_request"))
        {
            return null;
        }

        var merged = TryGetBool(pr, "merged") ?? false;
        var state = action switch
        {
            "opened" => "PROpened",
            "ready_for_review" => "PRReadyForReview",
            "closed" when merged => "PRMerged",
            "closed" => "PRClosed",
            _ => $"PR{action}"
        };

        var occurredAt = TryGetDateTimeOffset(pr, "updated_at")
            ?? TryGetDateTimeOffset(pr, "created_at")
            ?? DateTimeOffset.UtcNow;

        return new MappedFlowEvent($"{repo}/pr/{prNumber}", repo, null, prNumber, null, eventType, action, state, payload, occurredAt);
    }

    private static MappedFlowEvent? MapWorkflowRun(JsonElement root, string action, string repo, string eventType, string payload)
    {
        if (!TryGetNestedElement(root, out var run, "workflow_run") ||
            !TryGetLong(run, out var runId, "id"))
        {
            return null;
        }

        var prNumber = TryGetFirstPullRequestNumber(run);
        var conclusion = TryGetString(run, "conclusion");

        var state = action switch
        {
            "requested" => "WorkflowRequested",
            "in_progress" => "WorkflowInProgress",
            "completed" when conclusion == "success" => "WorkflowSuccess",
            "completed" => "WorkflowFailure",
            _ => $"Workflow{action}"
        };

        var flowId = prNumber.HasValue ? $"{repo}/pr/{prNumber}" : $"{repo}/workflow/{runId}";
        var occurredAt = TryGetDateTimeOffset(run, "updated_at")
            ?? TryGetDateTimeOffset(run, "run_started_at")
            ?? TryGetDateTimeOffset(run, "created_at")
            ?? DateTimeOffset.UtcNow;

        return new MappedFlowEvent(flowId, repo, null, prNumber, runId, eventType, action, state, payload, occurredAt);
    }

    private static int? TryGetFirstPullRequestNumber(JsonElement run)
    {
        if (!run.TryGetProperty("pull_requests", out var prs) ||
            prs.ValueKind != JsonValueKind.Array ||
            prs.GetArrayLength() == 0)
        {
            return null;
        }

        var first = prs[0];
        return TryGetInt(first, out var number, "number") ? number : null;
    }

    private static bool TryGetRepoFullName(JsonElement root, out string repoFullName)
    {
        repoFullName = string.Empty;
        if (!TryGetNestedElement(root, out var repository, "repository"))
        {
            return false;
        }

        var value = TryGetString(repository, "full_name");
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        repoFullName = value;
        return true;
    }

    private static bool TryGetNestedElement(JsonElement root, out JsonElement value, params string[] path)
    {
        value = root;
        foreach (var segment in path)
        {
            if (!value.TryGetProperty(segment, out value))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryGetNestedInt(JsonElement root, out int value, params string[] path)
    {
        value = default;
        return TryGetNestedElement(root, out var nested, path) && nested.TryGetInt32(out value);
    }

    private static bool TryGetInt(JsonElement root, out int value, string property)
    {
        value = default;
        return root.TryGetProperty(property, out var node) && node.TryGetInt32(out value);
    }

    private static bool TryGetLong(JsonElement root, out long value, string property)
    {
        value = default;
        return root.TryGetProperty(property, out var node) && node.TryGetInt64(out value);
    }

    private static bool? TryGetBool(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var node) && node.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? node.GetBoolean()
            : null;
    }

    private static string? TryGetString(JsonElement root, string property)
    {
        return root.TryGetProperty(property, out var node) ? node.GetString() : null;
    }

    private static DateTimeOffset? TryGetDateTimeOffset(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var node) || node.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(node.GetString(), out var parsed) ? parsed : null;
    }

    private static DateTimeOffset? TryGetNestedDateTimeOffset(JsonElement root, params string[] path)
    {
        if (!TryGetNestedElement(root, out var node, path) || node.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(node.GetString(), out var parsed) ? parsed : null;
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
