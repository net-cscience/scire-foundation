namespace SCIRE.Foundation.Service.Evaluation.Submission;

public sealed record EvaluationSubmissionResult(
    bool Status,
    EvaluationVerdict Verdict,
    string Description
);