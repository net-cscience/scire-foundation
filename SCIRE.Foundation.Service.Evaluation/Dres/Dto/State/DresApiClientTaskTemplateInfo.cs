namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.State;

internal sealed record DresApiClientTaskTemplateInfo(
    string Name,
    string TaskGroup,
    string TaskType,
    long? Duration
);