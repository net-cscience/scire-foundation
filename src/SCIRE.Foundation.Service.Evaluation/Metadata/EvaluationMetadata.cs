namespace SCIRE.Foundation.Service.Evaluation.Metadata;

/// <summary>Public DRES metadata. ServerTimestamp is Unix milliseconds; optional uptime is milliseconds.</summary>
/// <param name="Provider">Provider name.</param>
/// <param name="Endpoint">Configured endpoint.</param>
/// <param name="ServerTimestamp">Server time in Unix milliseconds.</param>
public sealed record EvaluationMetadata(
    string Provider,
    Uri Endpoint,
    long ServerTimestamp
    )
{
    /// <summary>Version advertised by DRES.</summary>
    public string? Version { get; init; }
    /// <summary>Server start time in Unix milliseconds, when reported.</summary>
    public long? ServerStartTimestamp { get; init; }
    /// <summary>Server uptime in milliseconds, when reported.</summary>
    public long? ServerUptimeMilliseconds { get; init; }
}
