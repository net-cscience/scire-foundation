namespace SCIRE.Foundation.Abstractions.Sources;

/// <summary>
/// Describes a set of sources and provides access to the concrete sources represented by it.
/// </summary>
public interface ISources
{
    /// <summary>
    /// Resolves the concrete sources represented by this source description.
    /// </summary>
    /// <returns>The resolved sources.</returns>
    IEnumerable<ISource> ResolveSources();

    /// <summary>
    /// Asynchronously resolves the concrete sources represented by this source description.
    /// </summary>
    /// <param name="cancellationToken">Allows cancellation of the source resolution.</param>
    /// <returns>The resolved sources as they become available.</returns>
    IAsyncEnumerable<ISource> ResolveSourcesAsync(
            CancellationToken cancellationToken = default);

}