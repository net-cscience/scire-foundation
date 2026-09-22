namespace SCIRE.Foundation.Abstractions.Config;

/// <summary>Owns an isolated configuration draft and its persistence operation.</summary>
public interface IConfigurationEdit<T> : IDisposable where T : class, IPersistableConfig
{
    /// <summary>Gets the mutable draft. Concurrent access to one draft is unsupported.</summary>
    T Value { get; }

    /// <summary>Validates, saves, and publishes the draft.</summary>
    /// <remarks>
    /// A successful save completes the session.
    /// Failure before commit leaves the session open.
    /// Publication failure after commit still completes the session.
    /// </remarks>
    Task PersistAsync(CancellationToken cancellationToken = default);
}