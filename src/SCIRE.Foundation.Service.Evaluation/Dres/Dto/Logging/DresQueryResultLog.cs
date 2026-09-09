namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.Logging;

internal sealed record DresQueryResultLog(
    long Timestamp,
    string SortType,
    string ResultSetAvailability,
    IReadOnlyList<DresRankedAnswer> Results,
    IReadOnlyList<DresQueryEvent> Events
);