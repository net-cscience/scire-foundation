using System.Net;
using Microsoft.Extensions.DependencyInjection;
using SCIRE.Foundation.Service.Evaluation.Diagnostics;
using SCIRE.Foundation.Service.Evaluation.Dres;
using SCIRE.Foundation.Service.Evaluation.Mapping;
using SCIRE.Foundation.Service.Evaluation.Tests.Support;

namespace SCIRE.Foundation.Service.Evaluation.Tests.Dres;

public sealed class DresPackageTests
{
    [Fact]
    public void DependencyInjection_ReusesOneServiceForTheSession()
    {
        var services = new ServiceCollection();
        services.AddDresEvaluation(new DresOptions { Endpoint = new Uri("https://dres.test/"), DefaultEvaluationId = "evaluation-1" });
        using var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IEvaluationService>();
        Assert.Same(first, provider.GetRequiredService<IEvaluationService>());
        Assert.Same(first, provider.GetRequiredService<DresEvaluationService>());
        Assert.Equal("evaluation-1", first.CreateLogger().EvaluationId);
    }

    [Fact]
    public async Task RequestDeadline_AlsoCoversReadingTheResponseBody()
    {
        using var client = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new DelayedStream()) }));
        using var service = new DresEvaluationService(client, new DresOptions { Endpoint = new Uri("https://dres.test/"), Timeout = TimeSpan.FromMilliseconds(60) }, new EvaluationScopeMappings());
        var status = await service.CheckConnectionAsync();
        Assert.Equal(EvaluationConnectionHealth.Unreachable, status.ConnectionHealth);
        Assert.Equal(200, status.LastHttpStatusCode);
        Assert.Equal(1, status.FailedRequests);
        Assert.Contains("timed out", status.LastError!);
    }

    [Fact]
    public async Task StateWithoutLogin_DoesNotSendARequest()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Unexpected request"));
        using var client = new HttpClient(handler);
        using var service = new DresEvaluationService(client, new DresOptions { Endpoint = new Uri("https://dres.test/") }, new EvaluationScopeMappings());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetStateAsync("evaluation-1"));
        Assert.Empty(handler.Requests);
    }

    private sealed class DelayedStream : MemoryStream
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
        }
    }
}
