using SCIRE.Foundation.Abstractions.Context;

namespace SCIRE.Foundation.Abstractions.Coordinates;

/// <summary>
/// Base implementation for Coordinates with stable identity within one SCIRE context.
/// </summary>
public abstract class CoordinateBase : ICoordinate
{
    /// <summary>
    /// Creates a Coordinate identified within the provided Context.
    /// </summary>
    /// <param name="id">Stable identity used by feature results to reference the Coordinate.</param>
    /// <param name="context">Context whose universe contains the Coordinate.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <see langword="null"/>.</exception>
    protected CoordinateBase(Guid id, IContext context)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The coordinate ID must not be empty.", nameof(id));

        ArgumentNullException.ThrowIfNull(context);

        Id = id;
        Context = context;
    }

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public IContext Context { get; }
}