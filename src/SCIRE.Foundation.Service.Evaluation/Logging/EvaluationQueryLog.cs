namespace SCIRE.Foundation.Service.Evaluation.Logging;

public sealed record EvaluationQueryLog(
    long Timestamp,
    IReadOnlyList<EvaluationQueryEvent> Events
);