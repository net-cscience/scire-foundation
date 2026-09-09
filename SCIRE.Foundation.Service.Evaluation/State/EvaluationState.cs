namespace SCIRE.Foundation.Service.Evaluation.State;

public sealed record EvaluationState(
    string EvaluationId,
    string EvaluationStatus,
    string? TaskId,
    string TaskStatus,
    string? TaskTemplateId,
    long? TimeLeft,
    long TimeElapsed
);