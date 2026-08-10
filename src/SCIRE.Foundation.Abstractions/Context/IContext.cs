using SCIRE.Foundation.Abstractions.Features;
using SCIRE.Foundation.Abstractions.Identity;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Abstractions.Context;

/// <summary>
/// Describes the universe of material and feature capabilities available within a SCIRE application.
/// </summary>
public interface IContext : IIdentifiable
{
    /// <summary>
    /// Human-readable identifier for distinguishing the context within an application.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Sources that define the material available within this context.
    /// </summary>
    IEnumerable<ISource> AvailableSources { get; }

    /// <summary>
    /// Feature capabilities that can be selected for processing within this context.
    /// </summary>
    IEnumerable<IFeatureDescription> AvailableFeatures { get; }

}