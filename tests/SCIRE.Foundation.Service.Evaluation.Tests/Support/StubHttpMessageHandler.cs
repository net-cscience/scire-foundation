using System.Net;
using System.Text;

namespace SCIRE.Foundation.Service.Evaluation.Tests.Support;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<RecordedHttpRequest, CancellationToken, Task<HttpResponseMessage>> _responder;

    public StubHttpMessageHandler(Func<RecordedHttpRequest, HttpResponseMessage> responder) : this((request, _) => Task.FromResult(responder(request))) { }

    public StubHttpMessageHandler(Func<RecordedHttpRequest, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        this._responder = responder;
    }

    public List<RecordedHttpRequest> Requests { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new RecordedHttpRequest(request.Method, request.RequestUri!, body);
        lock (this.Requests) { this.Requests.Add(recorded); }
        return await this._responder(recorded, cancellationToken);
    }

    public static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    public static HttpResponseMessage Text(string text, HttpStatusCode statusCode = HttpStatusCode.OK) => new(statusCode) { Content = new StringContent(text, Encoding.UTF8, "text/plain") };
}

internal sealed record RecordedHttpRequest(
    HttpMethod Method,
    Uri Uri,
    string? Body
    );
