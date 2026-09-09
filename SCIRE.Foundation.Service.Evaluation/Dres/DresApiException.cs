using System.Net;

namespace SCIRE.Foundation.Service.Evaluation.Dres;

public sealed class DresApiException : Exception
{
    public DresApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        this.StatusCode = statusCode;
    }


    public HttpStatusCode StatusCode { get; }
}