namespace SCIRE.Foundation.Service.Evaluation.Logging;

/// <summary>A ranked retrieval result list and its associated interactions.</summary>
/// <param name="Timestamp">UTC Unix time in milliseconds.</param>
/// <param name="SortType">DRES sort descriptor, for example list.</param>
/// <param name="ResultSetAvailability">Result availability descriptor agreed with the evaluation organizers.</param>
/// <param name="Results">Ranked results; supplied order and ranks are preserved.</param>
/// <param name="Events">Interactions associated with this result list.</param>
public sealed record EvaluationResultLog(
    long Timestamp,
    string SortType,
    string ResultSetAvailability,
    IReadOnlyList<EvaluationRankedResult> Results,
    IReadOnlyList<EvaluationQueryEvent> Events
);