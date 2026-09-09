namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.Submission;

internal sealed record DresSuccessfulSubmissionsStatus(
    bool Status,
    DresVerdictStatus Submission,
    string Description
);