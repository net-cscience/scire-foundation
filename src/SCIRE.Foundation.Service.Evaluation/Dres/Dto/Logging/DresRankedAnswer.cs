using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Submission;

namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.Logging;

internal sealed record DresRankedAnswer(
    DresApiClientAnswer Answer,
    int Rank
);