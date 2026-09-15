namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.State;

internal sealed record DresApiClientEvaluationInfo(
    string Id,
    string Name,
    string Type,
    string Status,
    string TemplateId,
    IReadOnlyList<string> Teams,
    IReadOnlyList<DresApiClientTaskTemplateInfo> TaskTemplates,
    string? TemplateDescription = null
);