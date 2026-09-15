namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.State;

internal sealed record DresApiEvaluationState(
    string EvaluationId,
    string EvaluationStatus,
    string TaskStatus,
    long TimeElapsed,
    string? TaskId = null,
    string? TaskTemplateId = null,
    long? TimeLeft = null
    );
