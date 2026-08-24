using SCIRE.Foundation.Abstractions.Context;

namespace SCIRE.Foundation.Abstractions.Coordinates;

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
    /// <param name="context">Context whose universe gives this Coordinate its meaning.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty or the Context has no stable identity.
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
    public Guid Id { get; protected set; }

    /// <inheritdoc />
    public Guid ContextId { get; protected set; }

    /// <summary>
    /// Gets or sets the Context containing this Coordinate.
    /// </summary>
    public ContextBase Context { get; protected set; } = null!;

    /// <inheritdoc />
    IContext ICoordinate.Context => this.Context;
}