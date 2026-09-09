namespace SCIRE.Foundation.Service.Evaluation.State;

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