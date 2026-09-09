namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.Logging;

internal sealed record DresQueryEvent(
    long Timestamp,
    string Category,
    string Type,
    string Value
);