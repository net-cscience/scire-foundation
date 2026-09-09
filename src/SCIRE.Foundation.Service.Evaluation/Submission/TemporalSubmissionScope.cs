namespace SCIRE.Foundation.Service.Evaluation.Submission;

public sealed record TemporalSubmissionScope(
    string MediaItemName,
    TimeSpan Start,
    TimeSpan End,
    string? MediaItemCollectionName = null
) : EvaluationSubmissionScope;