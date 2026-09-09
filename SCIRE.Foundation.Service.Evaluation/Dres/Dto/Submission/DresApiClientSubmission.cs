namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.Submission;

internal sealed record DresApiClientSubmission(
    IReadOnlyList<DresApiClientAnswerSet> AnswerSets
);