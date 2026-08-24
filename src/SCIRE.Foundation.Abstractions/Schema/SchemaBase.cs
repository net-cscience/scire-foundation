using SCIRE.Foundation.Abstractions.Context;
using SCIRE.Foundation.Abstractions.Features;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>
/// Base implementation for schemas defining a processing configuration over one SCIRE context.
/// </summary>
/// <typeparam name="TContext">Concrete Context type this Schema belongs to.</typeparam>
public abstract class SchemaBase<TContext> : ISchema<TContext>
    where TContext : IContext
{
    /// <summary>
    /// Creates a schema defined against the provided context.
    /// </summary>
    /// <param name="id">Stable identity used to reference the schema.</param>
    /// <param name="name">Human-readable name used to distinguish the schema.</param>
    /// <param name="context">Context whose available Sources and feature capabilities define the universe from which this schema selects.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty or <paramref name="name"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    protected SchemaBase(Guid id, string name, TContext context)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The schema ID must not be empty.", nameof(id));

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(context);

        this.Id = id;
        this.Name = name.Trim();
        this.Context = context;
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Gets the concrete Context this Schema belongs to.
    /// </summary>
    public TContext Context { get; }

    /// <inheritdoc />
    IContext ISchema.Context => this.Context;

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