using SCIRE.Foundation.Abstractions.Identity;

namespace SCIRE.Foundation.Abstractions.Sources;

/// <summary>
/// Represents a source of material that can be made available within a SCIRE context.
/// </summary>
/// <remarks>
/// The concrete source type defines where the material originates and how broadly it is scoped,
/// for example as a single asset, a collection of assets, an API, a database, or another application-specific source.
/// </remarks>
public interface ISource : IIdentifiable
{
}