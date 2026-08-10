using System.Runtime.CompilerServices;

namespace SCIRE.Foundation.Abstractions.Sources;

/// <summary>
/// Represents a source description that resolves to no concrete sources.
/// </summary>
public sealed class EmptySources : ISources
{
    /// <summary>
    /// Shared empty source description.
    /// </summary>
    public static EmptySources Instance { get; } = new();

    private EmptySources()
    {
    }

    /// <inheritdoc />
    public IEnumerable<ISource> ResolveSources()
    {
        return [];
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ISource> ResolveSourcesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.CompletedTask;
        yield break;
    }
}