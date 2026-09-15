using System;
using System.Net.Http;
using System.Threading.Tasks;
using SCIRE.Foundation.Service.Evaluation.Dres;
using SCIRE.Foundation.Service.Evaluation.Mapping;
using SCIRE.Foundation.Service.Evaluation.Tests.Support;
using Xunit;

namespace SCIRE.Foundation.Service.Evaluation.Tests.Dres;

public sealed class DresLiveTests
{
    [DresLiveFact(force: true)]
    [Trait("Category", "DresLive")]
    public async Task LiveDres_SmokeTest()
    {
        var endpoint = new Uri(Environment.GetEnvironmentVariable("DRES_ENDPOINT")??"http://10.34.64.205:8080");
        var username = Environment.GetEnvironmentVariable("DRES_USERNAME")??"user1";
        var password = Environment.GetEnvironmentVariable("DRES_PASSWORD")??"password1";
        var evaluationId = Environment.GetEnvironmentVariable("DRES_EVALUATION_ID")??"5415a4b5";

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