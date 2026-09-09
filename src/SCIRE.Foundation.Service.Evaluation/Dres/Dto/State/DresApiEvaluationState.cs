namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.State;

internal sealed record DresApiEvaluationState(
    string EvaluationId,
    string EvaluationStatus,
    string? TaskId,
    string TaskStatus,
    string? TaskTemplateId,
    long? TimeLeft,
    long TimeElapsed
);