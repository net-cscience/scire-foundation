namespace SCIRE.Foundation.Service.Evaluation.Submission;

public sealed record ItemSubmissionScope(
    string MediaItemName,
    string? MediaItemCollectionName = null
) : EvaluationSubmissionScope;