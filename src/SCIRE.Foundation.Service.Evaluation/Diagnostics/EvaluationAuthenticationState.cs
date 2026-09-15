namespace SCIRE.Foundation.Service.Evaluation.Diagnostics;

/// <summary>Locally known authentication state; it is not a guarantee of access to every evaluation.</summary>
public enum EvaluationAuthenticationState
{
    /// <summary>No session is held.</summary>
    LoggedOut,
    /// <summary>A session was obtained or validated successfully.</summary>
    Authenticated,
    /// <summary>DRES rejected the current session with HTTP 401; log in again.</summary>
    Rejected
}
