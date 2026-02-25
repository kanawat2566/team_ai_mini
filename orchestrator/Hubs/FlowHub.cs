using Microsoft.AspNetCore.SignalR;

namespace Orchestrator.Hubs;

public class FlowHub : Hub
{
    public const string HubPath = "/hubs/flow";

    public Task SubscribeFlow(string flowId) => Groups.AddToGroupAsync(Context.ConnectionId, FlowGroup(flowId));

    public Task SubscribeRepo(string repoFullName) => Groups.AddToGroupAsync(Context.ConnectionId, RepoGroup(repoFullName));

    public static string FlowGroup(string flowId) => $"flow:{flowId}";

    public static string RepoGroup(string repoFullName) => $"repo:{repoFullName}";
}
