namespace SCIRE.Foundation.Service.Evaluation.Submission;

/// <summary>A group of answers submitted for the active task or an explicitly identified task.</summary>
/// <param name="Answers">Answers belonging to this set.</param>
/// <param name="TaskId">Optional task ID; mutually exclusive with TaskName in this client.</param>
/// <param name="TaskName">Optional task name.</param>
public sealed record EvaluationAnswerSet(
    IReadOnlyList<EvaluationSubmissionScope> Answers,
    string? TaskId = null,
    string? TaskName = null
    );
