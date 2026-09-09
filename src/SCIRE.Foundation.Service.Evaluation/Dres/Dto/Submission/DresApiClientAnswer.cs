namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.Submission;

internal sealed record DresApiClientAnswer(
    string? Text = null,
    string? MediaItemName = null,
    string? MediaItemCollectionName = null,
    long? Start = null,
    long? End = null
);