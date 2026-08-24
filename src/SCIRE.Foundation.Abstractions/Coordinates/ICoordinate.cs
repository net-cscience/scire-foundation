using SCIRE.Foundation.Abstractions.Context;
using SCIRE.Foundation.Abstractions.Identity;

namespace SCIRE.Foundation.Abstractions.Coordinates;

/// <summary>
/// Represents an identifiable point within one SCIRE Context.
/// </summary>
/// <remarks>
/// The concrete Coordinate type defines the structure of the referenced point.
/// Every Coordinate belongs to exactly one Context, while Source relationships remain specific to concrete Coordinate types.
/// </remarks>
public interface ICoordinate : IIdentifiable
{
    /// <summary>
    /// Gets the stable identifier of the Context containing this Coordinate.
    /// </summary>
    Guid ContextId { get; }

    /// <summary>
    /// Gets the Context whose universe gives this Coordinate its meaning.
    /// </summary>
    IContext Context { get; }
}