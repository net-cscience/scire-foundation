namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto;

internal sealed record DresErrorStatus(
    bool Status,
    string Description
);