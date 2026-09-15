namespace SCIRE.Foundation.Service.Evaluation.Submission;

/// <summary>Submission acknowledgement and the verdict returned by DRES.</summary>
/// <param name="Status">Whether DRES accepted the submission request; this does not imply a correct answer.</param>
/// <param name="Verdict">DRES judgement of the submitted answer.</param>
/// <param name="Description">Message returned by DRES.</param>
public sealed record EvaluationSubmissionResult(
    bool Status,
    EvaluationVerdict Verdict,
    string Description
);