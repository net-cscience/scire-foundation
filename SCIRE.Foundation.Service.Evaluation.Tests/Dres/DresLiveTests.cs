using SCIRE.Foundation.Service.Evaluation.Dres;
using SCIRE.Foundation.Service.Evaluation.Mapping;
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

        using var client = new HttpClient();

        var service = new DresEvaluationService(
            client,
            new DresOptions { Endpoint = endpoint },
            new EvaluationScopeMappings());

        var user = await service.LoginAsync(username, password);

        Assert.True(service.IsAuthenticated);
        Assert.False(string.IsNullOrWhiteSpace(user.SessionId));

        var metadata = await service.GetMetadataAsync();
        Assert.Equal("DRES", metadata.Provider);

        var evaluations = await service.GetEvaluationsAsync();
        Assert.NotNull(evaluations);

        if (!string.IsNullOrWhiteSpace(evaluationId))
        {
            var state = await service.GetStateAsync(evaluationId);
            Assert.Equal(evaluationId, state.EvaluationId);
        }

        await service.LogoutAsync();

        Assert.False(service.IsAuthenticated);
    }
}