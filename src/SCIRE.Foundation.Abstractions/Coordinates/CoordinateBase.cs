namespace SCIRE.Foundation.Abstractions.Coordinates;

/// <summary>
/// Base implementation for coordinates whose identity is used as the stable reference point for feature results.
/// </summary>
public abstract class CoordinateBase : ICoordinate
{
    /// <inheritdoc />
    public Guid Id { get; set; }
}