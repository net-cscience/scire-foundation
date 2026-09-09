namespace SCIRE.Foundation.Service.Evaluation.State;

public sealed record EvaluationTaskTemplateInfo(
    string Name,
    string TaskGroup,
    string TaskType,
    long? Duration
);