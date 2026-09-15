using SCIRE.Foundation.Service.Evaluation.Submission;

namespace SCIRE.Foundation.Service.Evaluation.Logging;

/// <summary>One result in a ranked retrieval list.</summary>
/// <param name="Answer">Media item, temporal range or text represented by this result.</param>
/// <param name="Rank">One-based position in the ranking.</param>
public sealed record EvaluationRankedResult(
    EvaluationSubmissionScope Answer,
    int Rank
);