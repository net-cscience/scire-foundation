namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.User;

internal sealed record DresApiUser(
    string? Id = null,
    string? Username = null,
    string? Role = null,
    string? SessionId = null
);