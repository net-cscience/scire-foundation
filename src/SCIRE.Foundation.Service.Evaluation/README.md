# SCIRE.Foundation.Service.Evaluation

A handwritten .NET 10 client for DRES v2. The package provides a stateful `IEvaluationService`, application-to-submission mappings, structured evaluation logging, and connection diagnostics. It has no dependency on SPOT, Godot, ASP.NET, or an OpenAPI generator.

## Changes from the supplied implementation

- Every protected request, including evaluation state, carries the stored `session` query parameter.
- Login retains the authenticated user and session in memory. The token is obtained from the login body, the `SESSIONID` response cookie, or the existing session endpoint fallback. Plain-text and JSON-string fallback responses are supported.
- `CurrentUser`, `Status`, `StatusChanged`, `CheckConnectionAsync`, and `MonitorConnectionAsync` expose identity, connection observations, round-trip time, and local request counts.
- `CreateLogger` provides a lightweight asynchronous logger for DRES interactions and result lists.
- `GetCurrentTaskAsync` uses the participant client endpoint. `GetStateAsync` remains available for evaluations that permit detailed viewing.
- Metadata includes the server version, start time, uptime, and clock.
- Explicit answer sets support task IDs or names for asynchronous evaluations.
- Transport, authentication, and public operations are organized into partial files; DTO mapping has one implementation.
- The existing generic submission overloads and constructor remain available. The expanded interface requires updates to other custom implementations of `IEvaluationService`.

## Why the state request failed

The original `LoginAsync` already stored the session ID. The original `GetStateAsync` called `RequireAuthentication()` but sent the request without `CreateAuthenticatedUri(...)`.

The minimal repair in that version is:

```csharp
using var response = await this._httpClient.GetAsync(this.CreateAuthenticatedUri(endpoint), cancellationToken);
```

DRES also refreshes an existing session cookie with `Secure` enabled. With a plain HTTP endpoint, relying on the cookie can therefore stop working after an earlier request. Explicit session parameters avoid that dependency. The revised service centralizes this behavior for all protected operations.

Authentication does not imply viewing permission. DRES can return `403` for detailed evaluation state when `participantCanView` is disabled. The service preserves the session in this case. `GetCurrentTaskAsync` uses the participant endpoint, but its metadata does not include the detailed state's countdown or task run ID. A `404` from that method can also mean that no task is active; it is deliberately exposed as an API error rather than converted to an invented state.

## Direct use

```csharp
using SCIRE.Foundation.Service.Evaluation.Dres;
using SCIRE.Foundation.Service.Evaluation.Submission;

var options = new DresOptions
{
    Endpoint = new Uri("http://your-dres-server:8080"),
    DefaultEvaluationId = evaluationId,
    Timeout = TimeSpan.FromSeconds(10)
};

using var service = DresEvaluationService.Create(options);
var user = await service.LoginAsync(username, password, cancellationToken);
var metadata = await service.GetMetadataAsync(cancellationToken);
var evaluations = await service.GetEvaluationsAsync(cancellationToken);
var health = await service.CheckConnectionAsync(cancellationToken);

var evaluationLogger = service.CreateLogger(evaluationId);
await evaluationLogger.LogEventAsync("TEXT", "jointEmbedding", "a red car", cancellationToken);

var scope = new TemporalSubmissionScope("v_19576", TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(3.25));
var result = await service.SubmitAsync(evaluationId, scope, cancellationToken);

await service.LogoutAsync(cancellationToken);
```

`result.Status` acknowledges the submission request. Use `result.Verdict` to determine whether the answer was judged correct, wrong, indeterminate, or undecidable.

The owned transport created by `Create` disables automatic cookies and redirects and uses a two-minute pooled connection lifetime. If you supply an `HttpClient` through the existing constructor, dedicate it to this service, disable automatic cookies and redirects on its handler, and keep its timeout at least as large as `DresOptions.Timeout`. The service neither changes nor disposes a caller-owned client. Requests use absolute URIs and preserve a configured reverse-proxy prefix.

## Dependency injection

The optional extension uses the ordinary Microsoft dependency-injection abstractions and does not require a web host:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SCIRE.Foundation.Service.Evaluation;
using SCIRE.Foundation.Service.Evaluation.Dres;

services.AddDresEvaluation(options, mappings =>
{
    mappings.Map<MyVideoResult, TemporalSubmissionScope>(result => new TemporalSubmissionScope(result.VideoId, result.Start, result.End));
});

var evaluation = serviceProvider.GetRequiredService<IEvaluationService>();
await evaluation.LoginAsync(username, password, cancellationToken);
```

Registration shares one instance between `IEvaluationService` and `DresEvaluationService`. Reuse that instance for the configured endpoint and user so login, logging, health checks, and submission share a session. For multiple users or endpoints, create separate service instances; do not share the singleton across users.

The endpoint and default evaluation ID are fixed at construction. Applications can persist those settings in their own configuration and construct a replacement service when they change. To select another evaluation without reconnecting, pass its ID to submission methods and create a logger bound to that ID. The logger captures its evaluation ID, not the session token, and therefore uses the current session on each call.

## Mapping application results

```csharp
var mappings = new EvaluationScopeMappings()
    .Map<MyVideoResult, TemporalSubmissionScope>(result => new TemporalSubmissionScope(result.VideoId, result.Start, result.End));

using var service = DresEvaluationService.Create(options, mappings);
await service.LoginAsync(username, password, cancellationToken);
await service.SubmitAsync(myVideoResult, cancellationToken);
```

Configure mappings before concurrent use. An exact runtime type mapping takes precedence. Multiple assignable mappings are rejected as ambiguous. Built-in `ItemSubmissionScope`, `TemporalSubmissionScope`, and `TextSubmissionScope` instances can be submitted directly. Collection names are optional and can disambiguate media items.

For explicit task targeting:

```csharp
var answers = new EvaluationSubmissionScope[] { new TextSubmissionScope("answer") };
var answerSet = new EvaluationAnswerSet(answers, TaskName: "Task 1");
await service.SubmitAsync(evaluationId, new EvaluationSubmission(new[] { answerSet }), cancellationToken);
```

The client accepts one task selector per answer set. A task ID or name must come from the relevant evaluation; do not substitute an evaluation ID or an unrelated template ID.

## Two logging purposes

`IEvaluationLogger` sends evaluation data to DRES. Its calls return `Task` and should be awaited. It does not introduce a queue, retry loop, or fire-and-forget delivery. `LogEventAsync` supplies the current UTC Unix timestamp; `LogQueryAsync` and `LogResultsAsync` preserve explicitly supplied timestamps.

```csharp
using SCIRE.Foundation.Service.Evaluation.Logging;

var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
var queryEvent = new EvaluationQueryEvent(timestamp, "TEXT", "jointEmbedding", queryText);
var ranked = results.Select((item, index) => new EvaluationRankedResult(mappings.Resolve(item), index + 1)).ToArray();
var log = new EvaluationResultLog(timestamp, "list", availabilityDescriptor, ranked, new[] { queryEvent });
await evaluationLogger.LogResultsAsync(log, cancellationToken);
```

Valid categories are `TEXT`, `IMAGE`, `SKETCH`, `FILTER`, `BROWSING`, `COOPERATION`, and `OTHER`; category input is case-insensitive. Use event types and availability descriptors agreed with the evaluation organizers. Preserve the original event timestamp when logging after a retrieval operation.

The optional `ILogger<DresEvaluationService>` records application diagnostics. An application can supply its existing Microsoft logging pipeline, including an NLog provider. It is not used to infer DRES query events from arbitrary application log messages. The built-in diagnostic calls omit request bodies and session parameters and redact the current credential if it appears in a server error.

## Connection status

```csharp
await foreach (var status in service.MonitorConnectionAsync(TimeSpan.FromSeconds(5), cancellationToken))
{
    // Marshal to the application's UI thread before updating controls.
    UpdateConnectionDisplay(status.ConnectionHealth, status.Authentication, status.RoundTripMilliseconds);
}
```

Alternatively, subscribe to `StatusChanged` and call `CheckConnectionAsync` from an existing timer. No background work starts until the application requests it. `StatusChanged` callbacks can run on request continuation threads and should remain short. A callback exception is isolated from the completed HTTP operation. With concurrent requests, notifications are advisory; read `Status` when applying a UI update to obtain the latest snapshot.

Each probe calls the public clock endpoint and, if a session is held, `/api/v2/user` to validate it. The latency value is the duration of the last completed HTTP operation, including its response body. It is not a one-way network delay or the sum of both probe requests. Local request counters include login, probes, metadata requests, and evaluation operations; they are not server-wide evaluation statistics. Snapshots carry `LastRequestAt` so an application can identify stale measurements.

| Observation | Connection health | Session behavior |
| --- | --- | --- |
| Successful request | Healthy | Retained |
| HTTP 401 for the retained session | Healthy | Marked Rejected; local token discarded |
| HTTP 403 or 404 | Healthy | Retained; API error exposed |
| HTTP 5xx or invalid successful JSON | Degraded | Retained |
| Network error or request timeout | Unreachable | Retained |
| Caller cancellation | Unchanged | Retained |

`CheckConnectionAsync` returns status for expected connection errors. Other service methods throw `DresApiException`, `HttpRequestException`, or `TimeoutException`; caller cancellation propagates as `OperationCanceledException`. A rejected session can indicate an expired session or insufficient role for that operation; inspect the error and configured user role before logging in again. The package does not retain passwords or perform automatic login.

The retained session is invalidated only if a rejection belongs to the current login generation. A delayed response from an earlier login cannot discard a newer session. Remote logout failures retain the local session so logout can be retried. `ClearSession()` explicitly discards it locally. `Dispose()` does not make a remote logout request. Perform login/account changes in the application's connection workflow; let active submissions finish before switching users.

Submissions and logs are sent once. A timeout after a POST does not establish whether the server accepted it; automatic replay could create duplicate evaluation records.

## Time and rank units

| Value | Unit |
| --- | --- |
| Query/event/result timestamps | UTC Unix milliseconds |
| Server timestamp/start time | UTC Unix milliseconds |
| Server uptime | Milliseconds |
| Temporal submission start/end | Milliseconds on the wire; `TimeSpan` in the public API |
| State `TimeLeft` / `TimeElapsed` | Seconds |
| Task template `Duration` | Seconds |
| `RoundTripMilliseconds` | Milliseconds |
| Result `Rank` | One-based integer |

`EvaluationState.RemainingTime` and `ElapsedTime` expose typed `TimeSpan` values. Negative temporal starts and inverted ranges are rejected before sending. Temporal offsets are truncated to whole milliseconds.

## Build and test

Keep the original repository layout:

- `src/SCIRE.Foundation.Service.Evaluation/`
- `tests/SCIRE.Foundation.Service.Evaluation.Tests/`

From the repository root:

```text
dotnet test tests/SCIRE.Foundation.Service.Evaluation.Tests/SCIRE.Foundation.Service.Evaluation.Tests.csproj -c Release
dotnet pack src/SCIRE.Foundation.Service.Evaluation/SCIRE.Foundation.Service.Evaluation.csproj -c Release -o artifacts
```

The package version is an editable prerelease default, `0.1.0-alpha.2`; the pack command can override it with `-p:PackageVersion=...`. The package includes XML documentation and this README. Nothing is published by these commands.

Live tests are opt-in. Set `DRES_ENDPOINT`, `DRES_USERNAME`, and `DRES_PASSWORD` locally, optionally `DRES_EVALUATION_ID`, then run `RunDresLiveTest.ps1`. Enable `DRES_TEST_VIEWER_STATE=1` only for a run that permits detailed participant viewing. The live tests exercise login, metadata, evaluation discovery, current-task information, health, and logout. They do not submit answers or write evaluation logs.

## API evidence and validation limits

The attachment identifies DRES 2.0.4. The implementation was checked against the DRES v2 contract and upstream handlers, including the tagged 2.0.4 state and session behavior. The supplied client examples are useful for session reuse but their C# example still uses the legacy v1 API; this package uses v2 POST submission and logging endpoints.

- [DRES v2.0.4 client API definition](https://github.com/dres-dev/DRES/blob/v2.0.4/doc/oas-client.json)
- [DRES v2.0.4 session handling](https://github.com/dres-dev/DRES/blob/v2.0.4/backend/src/main/kotlin/dev/dres/api/rest/RestApi.kt)
- [Detailed evaluation state and viewing restrictions](https://github.com/dres-dev/DRES/blob/v2.0.4/backend/src/main/kotlin/dev/dres/api/rest/handler/evaluation/viewer/GetEvaluationStateHandler.kt)
- [Evaluation state time conversion](https://github.com/dres-dev/DRES/blob/v2.0.4/backend/src/main/kotlin/dev/dres/api/rest/types/evaluation/ApiEvaluationState.kt)
- [Client examples](https://github.com/dres-dev/Client-Examples)

Offline tests use controlled HTTP responses and cover authentication, schema handling, submission and log payloads, health, timeouts, cancellation, session races, and DI lifetime. The private DRES instance was not contacted from this workspace; live compatibility must be checked from its network using the opt-in tests.
