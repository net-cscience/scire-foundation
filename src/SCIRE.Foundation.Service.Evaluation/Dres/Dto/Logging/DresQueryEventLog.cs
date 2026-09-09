namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.Logging;

internal sealed record DresQueryEventLog(
    long Timestamp,
    IReadOnlyList<DresQueryEvent> Events
);