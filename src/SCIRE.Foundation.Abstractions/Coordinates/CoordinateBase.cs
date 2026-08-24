using SCIRE.Foundation.Abstractions.Context;

namespace SCIRE.Foundation.Abstractions.Coordinates;

/// <summary>
/// Base implementation for a persisted Coordinate belonging to exactly one Context.
/// </summary>
public abstract class CoordinateBase : ICoordinate
{
    /// <summary>
    /// Creates an empty Coordinate for persistence materialization.
    /// </summary>
    protected CoordinateBase()
    {
    }

    /// <summary>
    /// Creates a Coordinate belonging to the provided Context.
    /// </summary>
    /// <param name="id">Stable identity used to reference the Coordinate.</param>
    /// <param name="context">Context in which the Coordinate has meaning.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty or the Context has no persisted identity.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    protected CoordinateBase(Guid id, ContextBase context)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The coordinate ID must not be empty.", nameof(id));

        ArgumentNullException.ThrowIfNull(context);

        if (context.Id == Guid.Empty)
            throw new ArgumentException("The context must be persisted before creating the coordinate.", nameof(context));

        this.Id = id;
        this.ContextId = context.Id;
        this.Context = context;
    }

    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the Context containing this Coordinate.
    /// </summary>
    public Guid ContextId { get; set; }

    /// <summary>
    /// Gets or sets the Context containing this Coordinate.
    /// </summary>
    public ContextBase Context { get; set; } = null!;

    /// <inheritdoc />
    IContext ICoordinate.Context => this.Context;
}