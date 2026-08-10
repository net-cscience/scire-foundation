using SCIRE.Foundation.Abstractions.Identity;

namespace SCIRE.Foundation.Abstractions.Features;

/// <summary>
/// Describes a feature capability that can be made available within a SCIRE context.
/// </summary>
/// <remarks>
/// A feature description represents a selectable capability and its associated metadata,
/// not an executed feature or a persisted feature result.
/// </remarks>
public interface IFeatureDescription : IIdentifiable
{

}