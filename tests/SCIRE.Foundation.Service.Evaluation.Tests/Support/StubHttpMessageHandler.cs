using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SCIRE.Foundation.Service.Evaluation.Tests.Support;

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<RecordedHttpRequest, HttpResponseMessage> _responder;


    public StubHttpMessageHandler(Func<RecordedHttpRequest, HttpResponseMessage> responder)
    {
        this._responder = responder;
    }


    public List<RecordedHttpRequest> Requests { get; } = [];


    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        var recorded = new RecordedHttpRequest(
            request.Method,
            request.RequestUri!,
            body);

        this.Requests.Add(recorded);

        return this._responder(recorded);
    }


    public static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }


    public static HttpResponseMessage Text(string text, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(text, Encoding.UTF8, "text/plain")
        };
    }
}


internal sealed record RecordedHttpRequest(
    HttpMethod Method,
    Uri Uri,
    string? Body
);