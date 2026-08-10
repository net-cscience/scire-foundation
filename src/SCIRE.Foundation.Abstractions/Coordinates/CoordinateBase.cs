namespace SCIRE.Foundation.Abstractions.Coordinates;

/// <summary>
/// Base implementation for coordinates whose identity is used as the stable reference point for feature results.
/// </summary>
public abstract class CoordinateBase : ICoordinate
{
    /// <summary>
    /// Creates a coordinate with the identity used by feature results to reference it.
    /// </summary>
    /// <param name="id">Stable identity of the coordinate.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty.
    /// </exception>
    protected CoordinateBase(Guid id)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The coordinate ID must not be empty.", nameof(id));

        this.Id = id;
    }

    /// <inheritdoc />
    public Guid Id { get; init; }
}