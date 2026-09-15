namespace SCIRE.Foundation.Service.Evaluation.State;

/// <summary>Participant-visible metadata for an evaluation run.</summary>
/// <param name="Id">Evaluation run ID.</param>
/// <param name="Name">Evaluation name.</param>
/// <param name="Type">DRES evaluation type, for example SYNCHRONOUS or ASYNCHRONOUS.</param>
/// <param name="Status">Current evaluation status.</param>
/// <param name="TemplateId">Evaluation template ID.</param>
/// <param name="TemplateDescription">Optional template description.</param>
/// <param name="Teams">Team names as reported by the participant client API.</param>
/// <param name="TaskTemplates">Task metadata available to the participant.</param>
public sealed record EvaluationInfo(
    string Id,
    string Name,
    string Type,
    string Status,
    string TemplateId,
    string? TemplateDescription,
    IReadOnlyList<string> Teams,
    IReadOnlyList<EvaluationTaskTemplateInfo> TaskTemplates
);