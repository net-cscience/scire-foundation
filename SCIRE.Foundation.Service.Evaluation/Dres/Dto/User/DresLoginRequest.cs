namespace SCIRE.Foundation.Service.Evaluation.Dres.Dto.User;

internal sealed record DresLoginRequest(
    string Username,
    string Password
);