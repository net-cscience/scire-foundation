namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.Submission;

internal sealed record DresApiClientAnswerSet(
    IReadOnlyList<DresApiClientAnswer> Answers,
    string? TaskId = null,
    string? TaskName = null
);