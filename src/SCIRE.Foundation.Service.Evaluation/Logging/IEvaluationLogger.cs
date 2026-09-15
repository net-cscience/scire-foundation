namespace SCIRE.Foundation.Service.Evaluation.Logging;

/// <summary>Asynchronous DRES interaction logging bound to one evaluation. Each call is awaited and sent once.</summary>
public interface IEvaluationLogger
{
    /// <summary>Evaluation to which this logger sends events and result lists.</summary>
    string EvaluationId { get; }
    /// <summary>Sends one interaction with a UTC Unix-millisecond timestamp.</summary>
    Task LogEventAsync(string category, string type, string value, CancellationToken cancellationToken = default);
    /// <summary>Sends a complete query log, preserving its supplied timestamps.</summary>
    Task LogQueryAsync(EvaluationQueryLog log, CancellationToken cancellationToken = default);
    /// <summary>Sends a complete result log, preserving its supplied ranks and timestamps.</summary>
    Task LogResultsAsync(EvaluationResultLog log, CancellationToken cancellationToken = default);
}
