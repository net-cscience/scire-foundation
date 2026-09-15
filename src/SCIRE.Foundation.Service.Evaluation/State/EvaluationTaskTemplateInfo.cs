namespace SCIRE.Foundation.Service.Evaluation.State;

/// <summary>Participant-visible information about a task template.</summary>
/// <param name="Name">Task name.</param>
/// <param name="TaskGroup">Task group name.</param>
/// <param name="TaskType">Task type name.</param>
/// <param name="Duration">Task duration in seconds, if bounded.</param>
public sealed record EvaluationTaskTemplateInfo(
    string Name,
    string TaskGroup,
    string TaskType,
    long? Duration
);