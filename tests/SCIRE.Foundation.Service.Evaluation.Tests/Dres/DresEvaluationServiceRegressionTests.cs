using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SCIRE.Foundation.Service.Evaluation.Diagnostics;
using SCIRE.Foundation.Service.Evaluation.Dres;
using SCIRE.Foundation.Service.Evaluation.Logging;
using SCIRE.Foundation.Service.Evaluation.Mapping;
using SCIRE.Foundation.Service.Evaluation.Submission;
using SCIRE.Foundation.Service.Evaluation.Tests.Support;

namespace SCIRE.Foundation.Service.Evaluation.Tests.Dres;

public sealed partial class DresEvaluationServiceTests
{
    [Fact]
    public async Task ProtectedOperations_UseTheSameExplicitSession()
    {
        var handler = CreateHandler();
        using var service = await CreateAuthenticatedServiceAsync(handler);
        await service.GetUserAsync();
        await service.GetEvaluationsAsync();
        await service.GetCurrentTaskAsync("evaluation-1");
        await service.GetStateAsync("evaluation-1");
        await service.SubmitAsync(new TextSubmissionScope("answer"));
        var logger = service.CreateLogger();
        await logger.LogEventAsync("text", "jointEmbedding", "red car");
        await logger.LogResultsAsync(new EvaluationResultLog(1000, "list", "all", new[] { new EvaluationRankedResult(new ItemSubmissionScope("item"), 1) }, Array.Empty<EvaluationQueryEvent>()));
        await service.LogoutAsync();
        Assert.All(handler.Requests.Skip(1), request => Assert.Equal("?session=session-123", request.Uri.Query));
        Assert.False(service.IsAuthenticated);
        Assert.Null(service.CurrentUser);
        Assert.Equal(EvaluationAuthenticationState.LoggedOut, service.Status.Authentication);
    }

    [Theory]
    [InlineData("session-456")]
    [InlineData("\"session-456\"")]
    public async Task SessionFallback_AcceptsPlainTextAndJsonStrings(string payload)
    {
        var handler = new StubHttpMessageHandler(request => request.Uri.AbsolutePath == "/api/v2/login"
            ? StubHttpMessageHandler.Json("""{"id":"user-1","username":"test-user","role":"PARTICIPANT"}""")
            : StubHttpMessageHandler.Text(payload));
        using var service = CreateService(handler);
        Assert.Equal("session-456", (await service.LoginAsync("test-user", "test-password")).SessionId);
    }

    [Fact]
    public async Task Login_ResolvesSecureSessionCookieWithoutCookieHandler()
    {
        var handler = new StubHttpMessageHandler(_ =>
        {
            var response = StubHttpMessageHandler.Json("""{"id":"user-1","username":"test-user","role":"PARTICIPANT"}""");
            response.Headers.Add("Set-Cookie", "SESSIONID=cookie-token; Path=/; Secure; HttpOnly");
            return response;
        });
        using var service = CreateService(handler);
        Assert.Equal("cookie-token", (await service.LoginAsync("test-user", "test-password")).SessionId);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(401, false)]
    [InlineData(403, true)]
    [InlineData(404, true)]
    public async Task ApiRejection_DistinguishesSessionFromEvaluationAccess(int code, bool remainsAuthenticated)
    {
        var handler = CreateHandler(request => request.Uri.AbsolutePath.EndsWith("/state") ? StubHttpMessageHandler.Text("Denied", (HttpStatusCode)code) : DefaultResponse(request));
        using var service = await CreateAuthenticatedServiceAsync(handler);
        var error = await Assert.ThrowsAsync<DresApiException>(() => service.GetStateAsync("evaluation-1"));
        Assert.Equal((HttpStatusCode)code, error.StatusCode);
        Assert.Equal(remainsAuthenticated, service.IsAuthenticated);
        Assert.Equal(EvaluationConnectionHealth.Healthy, service.Status.ConnectionHealth);
        Assert.Equal(remainsAuthenticated ? EvaluationAuthenticationState.Authenticated : EvaluationAuthenticationState.Rejected, service.Status.Authentication);
    }

    [Fact]
    public async Task CurrentTask404_RemainsVisibleAsAnApiError()
    {
        var handler = CreateHandler(request => request.Uri.AbsolutePath.Contains("currentTask") ? StubHttpMessageHandler.Text("No active task", HttpStatusCode.NotFound) : DefaultResponse(request));
        using var service = await CreateAuthenticatedServiceAsync(handler);
        var error = await Assert.ThrowsAsync<DresApiException>(() => service.GetCurrentTaskAsync("evaluation-1"));
        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public async Task HealthProbe_VerifiesAuthenticationAfterPublicTimeRequest()
    {
        var handler = CreateHandler(request => request.Uri.AbsolutePath == "/api/v2/user" ? StubHttpMessageHandler.Text("Expired", HttpStatusCode.Unauthorized) : DefaultResponse(request));
        using var service = await CreateAuthenticatedServiceAsync(handler);
        var status = await service.CheckConnectionAsync();
        Assert.Equal(EvaluationConnectionHealth.Healthy, status.ConnectionHealth);
        Assert.Equal(EvaluationAuthenticationState.Rejected, status.Authentication);
        Assert.False(service.IsAuthenticated);
        Assert.Equal(401, status.LastHttpStatusCode);
        Assert.Equal(2, status.SuccessfulRequests);
        Assert.Equal(1, status.FailedRequests);
        Assert.True(status.RoundTripMilliseconds >= 0);
    }

    [Fact]
    public async Task TransportFailure_ReportsUnreachableWithoutDiscardingSession()
    {
        var handler = CreateHandler(request => request.Uri.AbsolutePath == "/api/v2/status/time" ? throw new HttpRequestException("offline") : DefaultResponse(request));
        using var service = await CreateAuthenticatedServiceAsync(handler);
        var status = await service.CheckConnectionAsync();
        Assert.Equal(EvaluationConnectionHealth.Unreachable, status.ConnectionHealth);
        Assert.True(service.IsAuthenticated);
        Assert.Equal(1, status.FailedRequests);
    }

    [Theory]
    [InlineData("<html>proxy response</html>")]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task InvalidSuccessfulResponse_IsAProtocolFailure(string payload)
    {
        using var service = CreateService(new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(payload)));
        var status = await service.CheckConnectionAsync();
        Assert.Equal(EvaluationConnectionHealth.Degraded, status.ConnectionHealth);
        Assert.Equal(1, status.FailedRequests);
        Assert.Equal(0, status.SuccessfulRequests);
    }

    [Fact]
    public async Task ServerFailure_IsDegradedAndDoesNotThrowFromProbe()
    {
        using var service = CreateService(new StubHttpMessageHandler(_ => StubHttpMessageHandler.Text("Maintenance", HttpStatusCode.ServiceUnavailable)));
        var status = await service.CheckConnectionAsync();
        Assert.Equal(EvaluationConnectionHealth.Degraded, status.ConnectionHealth);
        Assert.Equal(503, status.LastHttpStatusCode);
    }

    [Fact]
    public async Task SubmissionTimeout_IsNotRetried()
    {
        var handler = new StubHttpMessageHandler(async (request, token) =>
        {
            if (request.Uri.AbsolutePath.Contains("/submit/"))
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            return DefaultResponse(request);
        });
        using var client = new HttpClient(handler);
        using var service = new DresEvaluationService(client, new DresOptions { Endpoint = new Uri("https://dres.test/"), DefaultEvaluationId = "evaluation-1", Timeout = TimeSpan.FromMilliseconds(60) }, new EvaluationScopeMappings());
        await service.LoginAsync("test-user", "test-password");
        await Assert.ThrowsAsync<TimeoutException>(() => service.SubmitAsync(new ItemSubmissionScope("item")));
        Assert.Single(handler.Requests, request => request.Uri.AbsolutePath.Contains("/submit/"));
        Assert.True(service.IsAuthenticated);
        Assert.Equal(EvaluationConnectionHealth.Unreachable, service.Status.ConnectionHealth);
    }

    [Fact]
    public async Task CallerCancellation_DoesNotCountAsConnectionFailure()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new StubHttpMessageHandler(async (_, token) =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return StubHttpMessageHandler.Json("{}");
        });
        using var service = CreateService(handler);
        using var cancellation = new CancellationTokenSource();
        var probe = service.CheckConnectionAsync(cancellation.Token);
        await entered.Task;
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => probe);
        Assert.Equal(0, service.Status.FailedRequests);
        Assert.Equal(EvaluationConnectionHealth.Unknown, service.Status.ConnectionHealth);
    }

    [Fact]
    public async Task ExpiredResponseFromPreviousLogin_CannotInvalidateNewSession()
    {
        var oldRequestEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseOldRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loginCount = 0;
        var handler = new StubHttpMessageHandler(async (request, _) =>
        {
            if (request.Uri.AbsolutePath == "/api/v2/login")
            {
                loginCount++;
                return StubHttpMessageHandler.Json(JsonSerializer.Serialize(new { id = "user", username = "test-user", role = "PARTICIPANT", sessionId = "session-" + loginCount }));
            }
            oldRequestEntered.SetResult();
            await releaseOldRequest.Task;
            return StubHttpMessageHandler.Text("Expired old session", HttpStatusCode.Unauthorized);
        });
        using var service = CreateService(handler);
        await service.LoginAsync("test-user", "test-password");
        var oldRequest = service.GetStateAsync("evaluation-1");
        await oldRequestEntered.Task;
        await service.LoginAsync("test-user", "test-password");
        releaseOldRequest.SetResult();
        await Assert.ThrowsAsync<DresApiException>(() => oldRequest);
        Assert.True(service.IsAuthenticated);
        Assert.Equal("session-2", service.CurrentUser!.SessionId);
    }

    [Fact]
    public async Task ClearSession_DuringLoginPreventsLateSessionRestoration()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new StubHttpMessageHandler(async (request, _) => { entered.SetResult(); await release.Task; return DefaultResponse(request); });
        using var service = CreateService(handler);
        var login = service.LoginAsync("test-user", "test-password");
        await entered.Task;
        service.ClearSession();
        release.SetResult();
        await Assert.ThrowsAsync<InvalidOperationException>(() => login);
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task LoggingCallbacks_CannotTurnAcceptedSubmissionIntoFailure()
    {
        using var service = await CreateAuthenticatedServiceAsync(CreateHandler());
        service.StatusChanged += _ => throw new InvalidOperationException("UI observer failed");
        var result = await service.SubmitAsync(new ItemSubmissionScope("item"));
        Assert.True(result.Status);
        Assert.Equal(0, service.Status.FailedRequests);
    }

    [Fact]
    public async Task ErrorsAndDiagnosticLogs_DoNotExposeSession()
    {
        var handler = CreateHandler(request => request.Uri.AbsolutePath.EndsWith("/state") ? StubHttpMessageHandler.Text("Denied " + request.Uri, HttpStatusCode.Forbidden) : DefaultResponse(request));
        var logger = new CapturingLogger();
        using var client = new HttpClient(handler);
        using var service = new DresEvaluationService(client, new DresOptions { Endpoint = new Uri("https://dres.test/") }, new EvaluationScopeMappings(), logger);
        await service.LoginAsync("test-user", "test-password");
        var error = await Assert.ThrowsAsync<DresApiException>(() => service.GetStateAsync("evaluation-1"));
        Assert.DoesNotContain("session-123", error.ToString());
        Assert.DoesNotContain("session-123", service.Status.LastError!);
        Assert.DoesNotContain("session-123", service.CurrentUser!.ToString());
        Assert.DoesNotContain("session-123", string.Join("\n", logger.Messages));
        Assert.DoesNotContain("test-password", string.Join("\n", logger.Messages));
        Assert.Equal("api/v2/evaluation/evaluation-1/state", error.RequestPath);
    }

    [Fact]
    public async Task EscapedSessionAndEndpointPrefix_ArePreserved()
    {
        const string session = "a+/b?c&d=1";
        var handler = new StubHttpMessageHandler(request => request.Uri.AbsolutePath.EndsWith("/login")
            ? StubHttpMessageHandler.Json(JsonSerializer.Serialize(new { id = "user", username = "user", role = "PARTICIPANT", sessionId = session }))
            : StubHttpMessageHandler.Json("[]"));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://unrelated.test/"), Timeout = TimeSpan.FromSeconds(33) };
        await client.GetAsync("warmup");
        using (var service = new DresEvaluationService(client, new DresOptions { Endpoint = new Uri("https://dres.test/proxy") }, new EvaluationScopeMappings()))
        {
            await service.LoginAsync("user", "password");
            await service.GetEvaluationsAsync();
            Assert.Equal("/proxy/api/v2/client/evaluation/list", handler.Requests.Last().Uri.AbsolutePath);
            Assert.Equal("?session=" + Uri.EscapeDataString(session), handler.Requests.Last().Uri.Query);
        }
        Assert.Equal(new Uri("https://unrelated.test/"), client.BaseAddress);
        Assert.Equal(TimeSpan.FromSeconds(33), client.Timeout);
        await client.GetAsync("still-usable-after-service-disposal");
    }

    [Fact]
    public async Task EvaluationLogger_PreservesEventSemanticsAndUsesMilliseconds()
    {
        var handler = CreateHandler();
        using var service = await CreateAuthenticatedServiceAsync(handler);
        var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await service.CreateLogger().LogEventAsync("text", "jointEmbedding", "a red car");
        using var json = JsonDocument.Parse(handler.Requests.Last().Body!);
        Assert.InRange(json.RootElement.GetProperty("timestamp").GetInt64(), before, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var queryEvent = json.RootElement.GetProperty("events")[0];
        Assert.Equal("TEXT", queryEvent.GetProperty("category").GetString());
        Assert.Equal("jointEmbedding", queryEvent.GetProperty("type").GetString());
        Assert.Equal("a red car", queryEvent.GetProperty("value").GetString());
    }

    [Fact]
    public async Task ExplicitSubmission_PreservesTaskTargetAndAnswerSets()
    {
        var handler = CreateHandler();
        using var service = await CreateAuthenticatedServiceAsync(handler);
        var submission = new EvaluationSubmission(new[] { new EvaluationAnswerSet(new EvaluationSubmissionScope[] { new TextSubmissionScope("one"), new TextSubmissionScope("two") }, TaskName: "Question 1") });
        await service.SubmitAsync(submission);
        using var json = JsonDocument.Parse(handler.Requests.Last().Body!);
        var set = json.RootElement.GetProperty("answerSets")[0];
        Assert.Equal("Question 1", set.GetProperty("taskName").GetString());
        Assert.Equal(2, set.GetProperty("answers").GetArrayLength());
        Assert.False(set.TryGetProperty("taskId", out _));
    }

    [Fact]
    public async Task InvalidTemporalScope_IsRejectedBeforeSending()
    {
        var handler = CreateHandler();
        using var service = await CreateAuthenticatedServiceAsync(handler);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitAsync(new TemporalSubmissionScope("video", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1))));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task InvalidLogCategory_IsRejectedBeforeSending()
    {
        var handler = CreateHandler();
        using var service = await CreateAuthenticatedServiceAsync(handler);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateLogger().LogEventAsync("not-a-category", "type", "value"));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task FailedLogout_RetainsSessionForRetry()
    {
        var handler = CreateHandler(request => request.Uri.AbsolutePath.EndsWith("/logout") ? StubHttpMessageHandler.Text("offline", HttpStatusCode.ServiceUnavailable) : DefaultResponse(request));
        using var service = await CreateAuthenticatedServiceAsync(handler);
        await Assert.ThrowsAsync<DresApiException>(() => service.LogoutAsync());
        Assert.True(service.IsAuthenticated);
        service.ClearSession();
        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public async Task Monitor_StopsWhenCancelled()
    {
        using var service = CreateService(CreateHandler());
        using var cancellation = new CancellationTokenSource();
        await using var monitor = service.MonitorConnectionAsync(TimeSpan.FromSeconds(1), cancellation.Token).GetAsyncEnumerator();
        Assert.True(await monitor.MoveNextAsync());
        Assert.Equal(EvaluationConnectionHealth.Healthy, monitor.Current.ConnectionHealth);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await monitor.MoveNextAsync());
    }

    private sealed class CapturingLogger : ILogger<DresEvaluationService>
    {
        public List<string> Messages { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => this.Messages.Add(formatter(state, exception));
    }
}
