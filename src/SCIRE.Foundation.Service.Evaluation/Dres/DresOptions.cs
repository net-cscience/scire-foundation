namespace SCIRE.Foundation.Service.Evaluation.Dres;

/// <summary>Connection settings for one DRES endpoint. Credentials are supplied to LoginAsync and are not retained.</summary>
public sealed record DresOptions
{
    /// <summary>Server root, optionally including a reverse-proxy path prefix.</summary>
    public required Uri Endpoint { get; init; }
    /// <summary>Evaluation used by overloads that omit an evaluation ID.</summary>
    public string? DefaultEvaluationId { get; init; }
    /// <summary>Deadline for each HTTP request, including reading its response body.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);

    internal void Validate()
    {
        if (this.Endpoint is null || !this.Endpoint.IsAbsoluteUri || this.Endpoint.Scheme is not ("http" or "https"))
            throw new ArgumentException("Endpoint must be an absolute HTTP or HTTPS URI.", nameof(this.Endpoint));
        if (this.Endpoint.Query.Length != 0 || this.Endpoint.Fragment.Length != 0 || this.Endpoint.UserInfo.Length != 0)
            throw new ArgumentException("Endpoint must not include a query, fragment or credentials.", nameof(this.Endpoint));
        if (this.Timeout <= TimeSpan.Zero || this.Timeout.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(this.Timeout));
    }
}
