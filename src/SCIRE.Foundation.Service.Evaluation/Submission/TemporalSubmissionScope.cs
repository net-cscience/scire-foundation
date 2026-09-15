namespace SCIRE.Foundation.Service.Evaluation.Submission;

/// <summary>A temporal range within a media item. Sub-millisecond precision is truncated when sent to DRES.</summary>
/// <param name="MediaItemName">Media item name as indexed in DRES.</param>
/// <param name="Start">Non-negative start offset.</param>
/// <param name="End">End offset, greater than or equal to Start.</param>
/// <param name="MediaItemCollectionName">Optional collection name.</param>
public sealed record TemporalSubmissionScope(
    string MediaItemName,
    TimeSpan Start,
    TimeSpan End,
    string? MediaItemCollectionName = null
) : EvaluationSubmissionScope;