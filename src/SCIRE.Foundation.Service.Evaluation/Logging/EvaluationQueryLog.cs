namespace SCIRE.Foundation.Service.Evaluation.Logging;

/// <summary>An ordered group of retrieval interactions.</summary>
/// <param name="Timestamp">UTC Unix time in milliseconds for this log.</param>
/// <param name="Events">Events in their original order with their own timestamps.</param>
public sealed record EvaluationQueryLog(
    long Timestamp,
    IReadOnlyList<EvaluationQueryEvent> Events
);