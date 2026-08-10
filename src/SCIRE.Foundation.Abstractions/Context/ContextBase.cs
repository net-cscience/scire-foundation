using SCIRE.Foundation.Abstractions.Features;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Abstractions.Context;

/// <summary>
/// Base implementation for contexts that define the material and feature capabilities available to a SCIRE application.
/// </summary>
public abstract class ContextBase : IContext
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = null!;

    /// <inheritdoc />
    public abstract IEnumerable<ISource> AvailableSources { get; }

    /// <inheritdoc />
    public abstract IEnumerable<IFeatureDescription> AvailableFeatures { get; }
}