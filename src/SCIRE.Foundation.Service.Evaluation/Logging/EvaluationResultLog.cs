namespace SCIRE.Foundation.Service.Evaluation.Logging;

public sealed record EvaluationResultLog(
    long Timestamp,
    string SortType,
    string ResultSetAvailability,
    IReadOnlyList<EvaluationRankedResult> Results,
    IReadOnlyList<EvaluationQueryEvent> Events
);