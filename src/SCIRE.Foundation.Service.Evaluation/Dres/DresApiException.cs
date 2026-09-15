using System.Net;

namespace SCIRE.Foundation.Service.Evaluation.Dres;

/// <summary>An HTTP or protocol error returned by DRES. RequestPath excludes the session query string.</summary>
public sealed class DresApiException : Exception
{
    /// <summary>Creates an error with an optional sanitized request path and underlying protocol exception.</summary>
    public DresApiException(HttpStatusCode statusCode, string message, string? requestPath = null, Exception? innerException = null) : base(message, innerException)
    {
        this.StatusCode = statusCode;
        this.RequestPath = requestPath;
    }

    /// <summary>HTTP status returned by the server.</summary>
    public HttpStatusCode StatusCode { get; }
    /// <summary>Relative request path without credentials or query parameters.</summary>
    public string? RequestPath { get; }
}
