namespace SCIRE.Foundation.Service.Evaluation.Diagnostics;

/// <summary>Transport and protocol health, independently of authentication and evaluation permissions.</summary>
public enum EvaluationConnectionHealth
{
    /// <summary>No request has completed.</summary>
    Unknown,
    /// <summary>The server answered a request without a server or protocol error.</summary>
    Healthy,
    /// <summary>The server responded with a server error or an invalid success payload.</summary>
    Degraded,
    /// <summary>A request could not reach completion because of a transport failure or timeout.</summary>
    Unreachable
}
