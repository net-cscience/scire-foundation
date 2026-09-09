namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.User;

internal sealed record DresApiUser(
    string? Id,
    string? Username,
    string? Role,
    string? SessionId
);