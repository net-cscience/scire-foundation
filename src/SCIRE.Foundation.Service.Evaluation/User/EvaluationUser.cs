namespace SCIRE.Foundation.Service.Evaluation.User;

/// <summary>Authenticated identity. SessionId is an in-memory credential and is excluded from ToString().</summary>
/// <param name="Id">DRES user ID.</param>
/// <param name="Username">DRES user name.</param>
/// <param name="Role">Role reported by DRES.</param>
/// <param name="SessionId">Session token; do not write this value to application logs.</param>
public sealed record EvaluationUser(
    string? Id,
    string? Username,
    string? Role,
    string? SessionId
    )
{
    /// <summary>Returns the user identity without the session credential.</summary>
    public override string ToString() => $"{this.Username} ({this.Role})";
}
