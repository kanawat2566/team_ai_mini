# DevOps Loop Tracker MVP

MVP for tracking GitHub events (`issues`, `pull_request`, `workflow_run`) through a .NET 8 orchestrator and pushing realtime state to a Next.js UI.

## Structure

- `orchestrator/` — .NET 8 Web API + SignalR + SQLite single-table event store.
- `ui/` — Next.js dashboard with node status view (Issue -> PR -> Workflow).

## Orchestrator (Backend)

### Implemented

- `POST /webhooks/github`
  - Verifies `X-Hub-Signature-256` when `GithubWebhook:Secret` is configured.
  - Handles 3 events: `issues`, `pull_request`, `workflow_run`.
  - Maps incoming payload to normalized state.
  - Stores raw payload and metadata in a single table (`FlowEvents`).
  - Enforces idempotency via unique `DeliveryId` (`X-GitHub-Delivery`).

- `GET /flows/{flowId}`
  - Returns current state and full timeline for one flow.

- `GET /flows?repo=owner/repo`
  - Returns latest state snapshot per flow for the repo.

- SignalR Hub `/hubs/flow`
  - `SubscribeFlow(flowId)`
  - `SubscribeRepo(repoFullName)`
  - Broadcast event: `FlowUpdated`

### State mapping

- `issues.opened` -> `IssueOpened`
- `pull_request.opened` -> `PROpened`
- `pull_request.ready_for_review` -> `PRReadyForReview`
- `pull_request.closed (merged=true)` -> `PRMerged`
- `pull_request.closed (merged=false)` -> `PRClosed`
- `workflow_run.requested` -> `WorkflowRequested`
- `workflow_run.in_progress` -> `WorkflowInProgress`
- `workflow_run.completed (success)` -> `WorkflowSuccess`
- `workflow_run.completed (!success)` -> `WorkflowFailure`

## UI

- Next.js single-page dashboard.
- Input for `flowId`.
- 3 visual nodes: Issue, Pull Request, Workflow Run.
- Status badges show idle/running/success/failure with icons including ✅.
- SignalR subscription listens for `FlowUpdated` and updates graph status in realtime.
- Includes `rete` dependency and a Rete-ready component placeholder for phase-2 canvas rendering.

## Quick start

### Backend

```bash
cd orchestrator
# requires .NET SDK 8+
dotnet restore
dotnet run
```

### UI

```bash
cd ui
npm install
npm run dev
```

Set `NEXT_PUBLIC_HUB_URL` if backend is not at `http://localhost:5000/hubs/flow`.

## GitHub webhook setup

Set webhook URL to:

```text
https://<your-host>/webhooks/github
```

Select events:

- Issues
- Pull requests
- Workflow runs

If using secret, set the same value in orchestrator `appsettings.json`:

```json
{
  "GithubWebhook": {
    "Secret": "<same-secret>"
  }
}
```
