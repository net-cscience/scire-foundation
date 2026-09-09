namespace SCIRE.Foundation.Service.Evaluation.User;

public sealed record EvaluationUser(
    string? Id,
    string? Username,
    string? Role,
    string? SessionId
);