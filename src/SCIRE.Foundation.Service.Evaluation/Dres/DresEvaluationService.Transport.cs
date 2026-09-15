using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SCIRE.Foundation.Service.Evaluation.Diagnostics;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto;

namespace SCIRE.Foundation.Service.Evaluation.Dres;

public sealed partial class DresEvaluationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    private Task<T> SendJsonAsync<T>(HttpMethod method, string path, object? body, DresSession? session, CancellationToken cancellationToken) => this.SendAsync(method, path, body, session, ReadJsonAsync<T>, cancellationToken);

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
        return result ?? throw new JsonException("DRES returned an empty JSON response.");
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, DresSession? session, Func<HttpResponseMessage, CancellationToken, Task<T>> readResponse, CancellationToken cancellationToken, string? sensitiveValue = null)
    {
        ObjectDisposedException.ThrowIf(this._disposed != 0, this);
        cancellationToken.ThrowIfCancellationRequested();
        var sessionId = session?.User.SessionId;
        var relativeUri = sessionId is null ? path : $"{path}?session={Uri.EscapeDataString(sessionId)}";
        using var request = new HttpRequestMessage(method, new Uri(this._baseUri, relativeUri));
        request.Headers.Accept.ParseAdd("application/json");
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: JsonOptions);
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(this._options.Timeout);
        var started = Stopwatch.GetTimestamp();
        HttpStatusCode? statusCode = null;
        try
        {
            using var response = await this._httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
            statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.Unauthorized && session is not null)
            {
                this.RejectSession(session);
            }
            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorAsync(response, deadline.Token).ConfigureAwait(false);
                throw new DresApiException(statusCode.Value, Redact(message, sessionId, sensitiveValue), path);
            }
            var result = await readResponse(response, deadline.Token).ConfigureAwait(false);
            if (result is DresSuccessStatus { Status: false } rejected)
                throw new DresApiException(statusCode.Value, Redact(rejected.Description, sessionId, sensitiveValue), path);
            this.RecordRequest(method, path, statusCode, started, null, EvaluationConnectionHealth.Healthy);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            var message = $"DRES request timed out: {method} {path}.";
            this.RecordRequest(method, path, statusCode, started, message, EvaluationConnectionHealth.Unreachable);
            throw new TimeoutException(message);
        }
        catch (HttpRequestException)
        {
            var message = $"DRES transport failed: {method} {path}.";
            this.RecordRequest(method, path, statusCode, started, message, EvaluationConnectionHealth.Unreachable);
            throw new HttpRequestException(message, null, statusCode);
        }
        catch (IOException)
        {
            var message = $"DRES response body could not be read: {method} {path}.";
            this.RecordRequest(method, path, statusCode, started, message, EvaluationConnectionHealth.Unreachable);
            throw new HttpRequestException(message, null, statusCode);
        }
        catch (JsonException)
        {
            var message = $"DRES returned an invalid or incomplete JSON response: {method} {path}.";
            this.RecordRequest(method, path, statusCode, started, message, EvaluationConnectionHealth.Degraded);
            throw new DresApiException(statusCode ?? HttpStatusCode.OK, message, path);
        }
        catch (DresApiException exception)
        {
            var health = (int)exception.StatusCode >= 500 || (int)exception.StatusCode < 400 ? EvaluationConnectionHealth.Degraded : EvaluationConnectionHealth.Healthy;
            this.RecordRequest(method, path, statusCode, started, exception.Message, health);
            throw;
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var document = JsonDocument.Parse(text);
                if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String)
                    return description.GetString()!;
                if (document.RootElement.ValueKind == JsonValueKind.String)
                    return document.RootElement.GetString()!;
            }
            catch (JsonException)
            {
                // Proxy errors may be plain text or HTML rather than DRES ErrorStatus objects.
            }
            return text;
        }
        return $"DRES request failed with status {(int)response.StatusCode} ({response.StatusCode}).";
    }

    private static string Redact(string message, params string?[] secrets)
    {
        foreach (var secret in secrets)
        {
            if (string.IsNullOrEmpty(secret))
                continue;
            message = message.Replace(secret, "[redacted]", StringComparison.Ordinal).Replace(Uri.EscapeDataString(secret), "[redacted]", StringComparison.Ordinal);
        }
        return message.Length <= 1024 ? message : message[..1024] + "…";
    }

    private void RecordRequest(HttpMethod method, string path, HttpStatusCode? statusCode, long started, string? error, EvaluationConnectionHealth health)
    {
        var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        lock (this._statusLock)
        {
            this._status = this._status with
            {
                ConnectionHealth = health,
                LastRequestAt = DateTimeOffset.UtcNow,
                RoundTripMilliseconds = elapsed,
                SuccessfulRequests = this._status.SuccessfulRequests + (error is null ? 1 : 0),
                FailedRequests = this._status.FailedRequests + (error is null ? 0 : 1),
                LastHttpStatusCode = statusCode is null ? null : (int)statusCode,
                LastError = error
            };
        }
        // Paths contain no query token; bodies and exception objects are deliberately excluded.
        if (error is null)
        {
            this._logger.LogDebug("DRES {Method} {Path}: {StatusCode} in {ElapsedMs:F1} ms.", method.Method, path, (int?)statusCode, elapsed);
        }
        else
        {
            this._logger.LogWarning("DRES {Method} {Path}: {StatusCode} in {ElapsedMs:F1} ms. {Error}", method.Method, path, (int?)statusCode, elapsed, error);
        }
        this.PublishStatus();
    }

    private void PublishStatus()
    {
        var handlers = this.StatusChanged;
        if (handlers is null)
            return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<EvaluationServiceStatus>>())
        {
            try
            {
                handler(this.Status);
            }
            catch (Exception exception)
            {
                // An observer must not turn a completed submission into an apparent request failure.
                this._logger.LogWarning("An evaluation status observer threw {ExceptionType}.", exception.GetType().Name);
            }
        }
    }
}
