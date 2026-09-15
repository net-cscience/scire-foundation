namespace SCIRE.Foundation.Service.Evaluation.Submission;

/// <summary>Explicit answer sets for one submission request.</summary>
/// <param name="AnswerSets">Answer sets, each optionally associated with a task.</param>
public sealed record EvaluationSubmission(
    IReadOnlyList<EvaluationAnswerSet> AnswerSets
    );
