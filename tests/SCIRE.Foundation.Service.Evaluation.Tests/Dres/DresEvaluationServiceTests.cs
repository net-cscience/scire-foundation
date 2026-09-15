using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using SCIRE.Foundation.Service.Evaluation.Dres;
using SCIRE.Foundation.Service.Evaluation.Mapping;
using SCIRE.Foundation.Service.Evaluation.Submission;
using SCIRE.Foundation.Service.Evaluation.Tests.Support;
using Xunit;

namespace SCIRE.Foundation.Service.Evaluation.Tests.Dres;

public sealed partial class DresEvaluationServiceTests
{
    [Fact]
    public async Task LoginAsync_AuthenticatesAndStoresSession()
    {
        var handler = CreateHandler();
        var service = CreateService(handler);

        var user = await service.LoginAsync("test-user", "test-password");

        Assert.True(service.IsAuthenticated);
        Assert.Equal("test-user", user.Username);
        Assert.Equal("session-123", user.SessionId);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/v2/login", request.Uri.AbsolutePath);

        using var json = JsonDocument.Parse(request.Body!);
        Assert.Equal("test-user", json.RootElement.GetProperty("username").GetString());
        Assert.Equal("test-password", json.RootElement.GetProperty("password").GetString());
    }


    [Fact]
    public async Task LoginAsync_UsesSessionEndpointWhenLoginDoesNotContainSession()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            return request.Uri.AbsolutePath switch
            {
                "/api/v2/login" => StubHttpMessageHandler.Json(
                    """{"id":"user-1","username":"test-user","role":"PARTICIPANT"}"""),

                "/api/v2/user/session" => StubHttpMessageHandler.Text("session-456"),

                _ => throw new InvalidOperationException($"Unexpected request '{request.Uri}'.")
            };
        });

        var service = CreateService(handler);

        var user = await service.LoginAsync("test-user", "test-password");

        Assert.Equal("session-456", user.SessionId);
        Assert.True(service.IsAuthenticated);
        Assert.Equal(2, handler.Requests.Count);
    }


    [Fact]
    public async Task GetEvaluationsAsync_MapsDresEvaluation()
    {
        var handler = CreateHandler();
        var service = await CreateAuthenticatedServiceAsync(handler);

        var evaluations = await service.GetEvaluationsAsync();

        var evaluation = Assert.Single(evaluations);

        Assert.Equal("evaluation-1", evaluation.Id);
        Assert.Equal("Test Evaluation", evaluation.Name);
        Assert.Equal("ACTIVE", evaluation.Status);

        var task = Assert.Single(evaluation.TaskTemplates);
        Assert.Equal("KIS", task.Name);

        var request = handler.Requests.Last();
        Assert.Equal("/api/v2/client/evaluation/list", request.Uri.AbsolutePath);
        Assert.Equal("?session=session-123", request.Uri.Query);
    }


    [Fact]
    public async Task GetStateAsync_MapsEvaluationState()
    {
        var handler = CreateHandler();
        var service = await CreateAuthenticatedServiceAsync(handler);

        var state = await service.GetStateAsync("evaluation-1");

        Assert.Equal("evaluation-1", state.EvaluationId);
        Assert.Equal("ACTIVE", state.EvaluationStatus);
        Assert.Equal("task-1", state.TaskId);
        Assert.Equal("RUNNING", state.TaskStatus);
        Assert.Equal(42, state.TimeLeft);
        Assert.Equal(TimeSpan.FromSeconds(42), state.RemainingTime);
        Assert.Equal("?session=session-123", handler.Requests.Last().Uri.Query);
    }


    [Fact]
    public async Task SubmitAsync_MapsApplicationScopeToDresTemporalSubmission()
    {
        var handler = CreateHandler();

        var mappings = new EvaluationScopeMappings()
            .Map<TestResult, TemporalSubmissionScope>(result =>
                new TemporalSubmissionScope(
                    result.VideoId,
                    result.Start,
                    result.End));

        var service = await CreateAuthenticatedServiceAsync(handler, mappings);

        var result = await service.SubmitAsync(
            new TestResult(
                "v_19576",
                TimeSpan.FromSeconds(1.5),
                TimeSpan.FromSeconds(3.25)));

        Assert.True(result.Status);
        Assert.Equal(EvaluationVerdict.Correct, result.Verdict);

        var request = handler.Requests.Single(request =>
            request.Uri.AbsolutePath == "/api/v2/submit/evaluation-1");

        Assert.Equal("?session=session-123", request.Uri.Query);

        using var json = JsonDocument.Parse(request.Body!);

        var answer = json.RootElement
            .GetProperty("answerSets")[0]
            .GetProperty("answers")[0];

        Assert.Equal("v_19576", answer.GetProperty("mediaItemName").GetString());
        Assert.Equal(1500, answer.GetProperty("start").GetInt64());
        Assert.Equal(3250, answer.GetProperty("end").GetInt64());
    }


    [Fact]
    public async Task GetMetadataAsync_MapsServerTime()
    {
        var handler = CreateHandler();
        var service = CreateService(handler);

        var metadata = await service.GetMetadataAsync();

        Assert.Equal("DRES", metadata.Provider);
        Assert.Equal(123456789L, metadata.ServerTimestamp);
        Assert.Equal("2.0.4", metadata.Version);
        Assert.Equal(56789L, metadata.ServerUptimeMilliseconds);
        Assert.Equal(new Uri("https://dres.test/"), metadata.Endpoint);
    }


    [Fact]
    public async Task SubmitAsync_ThrowsDresApiExceptionWhenSubmissionIsRejected()
    {
        var handler = CreateHandler(request =>
        {
            if (request.Uri.AbsolutePath == "/api/v2/submit/evaluation-1")
            {
                return StubHttpMessageHandler.Json(
                    """{"status":false,"description":"No active task accepts submissions."}""",
                    HttpStatusCode.PreconditionFailed);
            }

            return DefaultResponse(request);
        });

        var service = await CreateAuthenticatedServiceAsync(handler);

        var exception = await Assert.ThrowsAsync<DresApiException>(() =>
            service.SubmitAsync(new ItemSubmissionScope("v_19576")));

        Assert.Equal(HttpStatusCode.PreconditionFailed, exception.StatusCode);
        Assert.Equal("No active task accepts submissions.", exception.Message);
    }


    private static DresEvaluationService CreateService(
        StubHttpMessageHandler handler,
        EvaluationScopeMappings? mappings = null)
    {
        var client = new HttpClient(handler);

        var options = new DresOptions
        {
            Endpoint = new Uri("https://dres.test/"),
            DefaultEvaluationId = "evaluation-1"
        };

        return new DresEvaluationService(
            client,
            options,
            mappings ?? new EvaluationScopeMappings());
    }


    private static async Task<DresEvaluationService> CreateAuthenticatedServiceAsync(
        StubHttpMessageHandler handler,
        EvaluationScopeMappings? mappings = null)
    {
        var service = CreateService(handler, mappings);
        await service.LoginAsync("test-user", "test-password");

        return service;
    }


    private static StubHttpMessageHandler CreateHandler(
        Func<RecordedHttpRequest, HttpResponseMessage>? overrideResponse = null)
    {
        return new StubHttpMessageHandler(request =>
            overrideResponse?.Invoke(request) ?? DefaultResponse(request));
    }


    private static HttpResponseMessage DefaultResponse(RecordedHttpRequest request)
    {
        if (request.Uri.AbsolutePath is not ("/api/v2/login" or "/api/v2/status/time" or "/api/v2/status/info") && request.Uri.Query != "?session=session-123")
            return StubHttpMessageHandler.Json("""{"status":false,"description":"Missing session."}""", HttpStatusCode.Unauthorized);
        return request.Uri.AbsolutePath switch
        {
            "/api/v2/login" => StubHttpMessageHandler.Json(
                """
                {
                  "id": "user-1",
                  "username": "test-user",
                  "role": "PARTICIPANT",
                  "sessionId": "session-123"
                }
                """),

            "/api/v2/user" => StubHttpMessageHandler.Json("""{"id":"user-1","username":"test-user","role":"PARTICIPANT","sessionId":"session-123"}"""),

            "/api/v2/client/evaluation/currentTask/evaluation-1" => StubHttpMessageHandler.Json("""{"name":"KIS","taskGroup":"KIS","taskType":"KIS","duration":300}"""),

            "/api/v2/status/info" => StubHttpMessageHandler.Json("""{"version":"2.0.4","startTime":123400000,"uptime":56789}"""),

            "/api/v2/logout" => StubHttpMessageHandler.Json(
                """{"status":true,"description":"Logged out."}"""),

            "/api/v2/client/evaluation/list" => StubHttpMessageHandler.Json(
                """
                [
                  {
                    "id": "evaluation-1",
                    "name": "Test Evaluation",
                    "type": "SYNCHRONOUS",
                    "status": "ACTIVE",
                    "templateId": "template-1",
                    "templateDescription": "Test",
                    "teams": ["team-1"],
                    "taskTemplates": [
                      {
                        "name": "KIS",
                        "taskGroup": "KIS",
                        "taskType": "KIS",
                        "duration": 300
                      }
                    ]
                  }
                ]
                """),

            "/api/v2/evaluation/evaluation-1/state" => StubHttpMessageHandler.Json(
                """
                {
                  "evaluationId": "evaluation-1",
                  "evaluationStatus": "ACTIVE",
                  "taskId": "task-1",
                  "taskStatus": "RUNNING",
                  "taskTemplateId": "template-1",
                  "timeLeft": 42,
                  "timeElapsed": 18
                }
                """),

            "/api/v2/submit/evaluation-1" => StubHttpMessageHandler.Json(
                """
                {
                  "status": true,
                  "submission": "CORRECT",
                  "description": "Correct."
                }
                """),

            "/api/v2/log/query/evaluation-1" => StubHttpMessageHandler.Json(
                """{"status":true,"description":"Logged."}"""),

            "/api/v2/log/result/evaluation-1" => StubHttpMessageHandler.Json(
                """{"status":true,"description":"Logged."}"""),

            "/api/v2/status/time" => StubHttpMessageHandler.Json(
                """{"timeStamp":123456789}"""),

            _ => throw new InvalidOperationException(
                $"Unexpected DRES request '{request.Method} {request.Uri}'.")
        };
    }


    private sealed record TestResult(
        string VideoId,
        TimeSpan Start,
        TimeSpan End
    );
}