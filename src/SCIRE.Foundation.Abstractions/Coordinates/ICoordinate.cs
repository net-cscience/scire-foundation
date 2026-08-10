using SCIRE.Foundation.Abstractions.Identity;

namespace SCIRE.Foundation.Abstractions.Coordinates;

/// <summary>
/// Represents an identifiable point to which information or feature results can refer within a SCIRE context.
/// </summary>
/// <remarks>
/// The concrete coordinate type defines the dimensions and structure of the referenced point.
/// Coordinate identity is independent of application-specific semantic equality.
/// </remarks>
public interface ICoordinate : IIdentifiable
{
}