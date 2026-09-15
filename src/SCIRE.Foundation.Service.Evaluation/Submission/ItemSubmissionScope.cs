namespace SCIRE.Foundation.Service.Evaluation.Submission;

/// <summary>A complete media item identified by its DRES name.</summary>
/// <param name="MediaItemName">Media item name as indexed in DRES.</param>
/// <param name="MediaItemCollectionName">Optional collection name used to disambiguate items.</param>
public sealed record ItemSubmissionScope(
    string MediaItemName,
    string? MediaItemCollectionName = null
) : EvaluationSubmissionScope;