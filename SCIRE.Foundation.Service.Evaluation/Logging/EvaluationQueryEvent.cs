namespace SCIRE.Foundation.Service.Evaluation.Logging;

public sealed record EvaluationQueryEvent(
    long Timestamp,
    string Category,
    string Type,
    string Value
);