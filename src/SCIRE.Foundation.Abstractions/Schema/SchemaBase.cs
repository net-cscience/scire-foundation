using SCIRE.Foundation.Abstractions.Context;
using SCIRE.Foundation.Abstractions.Features;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>
/// Base implementation for schemas that define a processing configuration over one SCIRE context.
/// </summary>
public abstract class SchemaBase : ISchema
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; } = null!;

    /// <inheritdoc />
    public abstract IContext Context { get; }

    /// <inheritdoc />
    public abstract IEnumerable<ISource> SelectedSources { get; }

    /// <inheritdoc />
    public abstract IEnumerable<IFeatureDescription> SelectedFeatures { get; }

    /// <inheritdoc />
    public abstract IEnumerable<ProcessingState> ProcessingStates { get; }

    /// <inheritdoc />
    public abstract ProcessingStateCreationMode StateCreationMode { get; }

    /// <inheritdoc />
    public abstract ProcessingReservationMode ReservationMode { get; }
}