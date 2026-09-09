using SCIRE.Foundation.Service.Evaluation.Submission;

namespace SCIRE.Foundation.Service.Evaluation.Logging;

public sealed record EvaluationRankedResult(
    EvaluationSubmissionScope Answer,
    int Rank
);