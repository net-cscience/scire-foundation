using SCIRE.Foundation.Abstractions.Features;
using SCIRE.Foundation.Abstractions.Identity;
using SCIRE.Foundation.Abstractions.Schema;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Abstractions.Context;

/// <summary>
/// Describes the universe of material, feature capabilities, and processing configurations available within a SCIRE application.
/// </summary>
public interface IContext : IIdentifiable
{
    /// <summary>
    /// Human-readable identifier for distinguishing the context within an application.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Describes the sources from which concrete processing sources can be resolved for this context.
    /// </summary>
    ISources AvailableSources { get; }

    /// <summary>
    /// Feature capabilities that can be selected for processing within this context.
    /// </summary>
    IEnumerable<IFeatureDescription> AvailableFeatures { get; }

    /// <summary>
    /// Processing configurations defined against this context and available for selection.
    /// </summary>
    /// <remarks>
    /// Availability does not imply that a schema is currently selected.
    /// Each schema references this context as the universe from which its sources and features are selected.
    /// </remarks>
    IEnumerable<ISchema> AvailableSchemas { get; }
}