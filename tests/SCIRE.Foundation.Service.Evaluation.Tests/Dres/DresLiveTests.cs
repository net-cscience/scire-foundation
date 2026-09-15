using System.Net;
using SCIRE.Foundation.Service.Evaluation.Diagnostics;
using SCIRE.Foundation.Service.Evaluation.Dres;
using SCIRE.Foundation.Service.Evaluation.Tests.Support;

namespace SCIRE.Foundation.Service.Evaluation.Tests.Dres;

public sealed class DresLiveTests
{
    [DresLiveFact]
    [Trait("Category", "DresLive")]
    public async Task LiveDres_SmokeTest()
    {
        var endpoint = new Uri(Environment.GetEnvironmentVariable("DRES_ENDPOINT")!);
        var username = Environment.GetEnvironmentVariable("DRES_USERNAME")!;
        var password = Environment.GetEnvironmentVariable("DRES_PASSWORD")!;
        var evaluationId = Environment.GetEnvironmentVariable("DRES_EVALUATION_ID");
        using var service = DresEvaluationService.Create(new DresOptions { Endpoint = endpoint });
        var user = await service.LoginAsync(username, password);
        try
        {
            Assert.True(service.IsAuthenticated);
            Assert.False(string.IsNullOrWhiteSpace(user.SessionId));
            Assert.Equal(user.Id, (await service.GetUserAsync()).Id);
            var metadata = await service.GetMetadataAsync();
            Assert.Equal("DRES", metadata.Provider);
            Assert.False(string.IsNullOrWhiteSpace(metadata.Version));
            var evaluations = await service.GetEvaluationsAsync();
            if (!string.IsNullOrWhiteSpace(evaluationId))
            {
                Assert.Contains(evaluations, evaluation => evaluation.Id == evaluationId);
                try
                {
                    var task = await service.GetCurrentTaskAsync(evaluationId);
                    Assert.False(string.IsNullOrWhiteSpace(task.Name));
                }
                catch (DresApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
                {
                    // A visible evaluation can exist before a task is active.
                }
            }
            var status = await service.CheckConnectionAsync();
            Assert.Equal(EvaluationConnectionHealth.Healthy, status.ConnectionHealth);
            Assert.Equal(EvaluationAuthenticationState.Authenticated, status.Authentication);
            Assert.True(status.RoundTripMilliseconds >= 0);
        }
        finally
        {
            await service.LogoutAsync();
        }
        Assert.False(service.IsAuthenticated);
    }

    [DresLiveFact(requireViewerState: true)]
    [Trait("Category", "DresLive")]
    public async Task LiveDres_ViewerState()
    {
        using var service = DresEvaluationService.Create(new DresOptions { Endpoint = new Uri(Environment.GetEnvironmentVariable("DRES_ENDPOINT")!) });
        await service.LoginAsync(Environment.GetEnvironmentVariable("DRES_USERNAME")!, Environment.GetEnvironmentVariable("DRES_PASSWORD")!);
        try
        {
            var evaluationId = Environment.GetEnvironmentVariable("DRES_EVALUATION_ID")!;
            var state = await service.GetStateAsync(evaluationId);
            Assert.Equal(evaluationId, state.EvaluationId);
        }
        finally
        {
            await service.LogoutAsync();
        }
    }
}
