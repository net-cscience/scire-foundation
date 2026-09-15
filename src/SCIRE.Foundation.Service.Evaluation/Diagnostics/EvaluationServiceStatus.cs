namespace SCIRE.Foundation.Service.Evaluation.Diagnostics;

/// <summary>An immutable snapshot of local request statistics. Counters reset when a new service is created.</summary>
/// <param name="Endpoint">Configured server endpoint.</param>
/// <param name="ConnectionHealth">Health of the last completed HTTP operation.</param>
/// <param name="Authentication">Locally known session state.</param>
/// <param name="LastRequestAt">UTC completion time of the last observed request.</param>
/// <param name="RoundTripMilliseconds">Elapsed HTTP operation time, including body reading; not one-way network latency.</param>
/// <param name="SuccessfulRequests">Completed requests with successful protocol responses.</param>
/// <param name="FailedRequests">Failed requests, excluding caller cancellation.</param>
/// <param name="LastHttpStatusCode">Last response status, or null for a transport failure.</param>
/// <param name="LastError">Sanitized last error, or null after success.</param>
public sealed record EvaluationServiceStatus(
    Uri Endpoint,
    EvaluationConnectionHealth ConnectionHealth = EvaluationConnectionHealth.Unknown,
    EvaluationAuthenticationState Authentication = EvaluationAuthenticationState.LoggedOut,
    DateTimeOffset? LastRequestAt = null,
    double? RoundTripMilliseconds = null,
    long SuccessfulRequests = 0,
    long FailedRequests = 0,
    int? LastHttpStatusCode = null,
    string? LastError = null
    );
