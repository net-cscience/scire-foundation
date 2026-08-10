using SCIRE.Foundation.Abstractions.Context;
using SCIRE.Foundation.Abstractions.Features;
using SCIRE.Foundation.Abstractions.Identity;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>
/// Describes a processing configuration that selects sources and feature capabilities from one SCIRE context.
/// </summary>
public interface ISchema : IIdentifiable
{
    /// <summary>
    /// Human-readable identifier for distinguishing the schema within an application.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Context whose available material and capabilities define the universe from which this schema selects.
    /// </summary>
    IContext Context { get; }

    /// <summary>
    /// Sources selected from the context for processing by this schema.
    /// </summary>
    IEnumerable<ISource> SelectedSources { get; }

    /// <summary>
    /// Feature capabilities selected from the context for processing by this schema.
    /// </summary>
    IEnumerable<IFeatureDescription> SelectedFeatures { get; }

    /// <summary>
    /// Tracks processing for the selected source and feature combinations.
    /// </summary>
    IEnumerable<ProcessingState> ProcessingStates { get; }

    /// <summary>
    /// Defines when processing states are created for selected source and feature combinations.
    /// </summary>
    ProcessingStateCreationMode StateCreationMode { get; }

    /// <summary>
    /// Defines how processing jobs are claimed by workers before execution.
    /// </summary>
    ProcessingReservationMode ReservationMode { get; }

}