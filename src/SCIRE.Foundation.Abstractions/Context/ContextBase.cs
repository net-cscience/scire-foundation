using SCIRE.Foundation.Abstractions.Features;
using SCIRE.Foundation.Abstractions.Schema;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Abstractions.Context;

/// <summary>
/// Base implementation for contexts describing the material, feature capabilities,
/// and schemas available to a SCIRE application.
/// </summary>
public abstract class ContextBase : IContext
{
    /// <summary>
    /// Creates a context with a stable identity and human-readable name.
    /// </summary>
    /// <param name="id">Identity used to reference the context.</param>
    /// <param name="name">Human-readable name used to distinguish the context.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty or <paramref name="name"/> is empty or whitespace.
    /// </exception>
    protected ContextBase(Guid id, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The context ID must not be empty.", nameof(id));

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name.Trim();
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public abstract ISources AvailableSources { get; }

    /// <inheritdoc />
    public abstract IEnumerable<IFeatureDescription> AvailableFeatures { get; }

    /// <inheritdoc />
    public abstract IEnumerable<ISchema> AvailableSchemas { get; }
}