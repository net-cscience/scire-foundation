using SCIRE.Foundation.Abstractions.Features;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>
/// Describes the processing lifecycle of one source and feature combination within a schema.
/// </summary>
public class ProcessingState
{
/// <summary>
/// Source participating in this processing job.
/// Together with <see cref="Feature"/>, it identifies the processing unit represented by this state.
/// </summary>
public ISource Source { get; set; } = null!;

/// <summary>
/// Feature capability applied to <see cref="Source"/> by this processing job.
/// </summary>
public IFeatureDescription Feature { get; set; } = null!;

/// <summary>
/// Current lifecycle stage of the processing job.
/// </summary>
public ProcessingStateStatus State { get; set; }

/// <summary>
/// Identifies the worker currently claiming the processing job.
/// </summary>
public string? ReservedBy { get; set; }

/// <summary>
/// Time at which the current reservation was acquired.
/// </summary>
public DateTimeOffset? ReservedAt { get; set; }

/// <summary>
/// Time after which the reservation may be reclaimed if the owning worker has not completed it.
/// </summary>
public DateTimeOffset? ReservationExpiresAt { get; set; }

}


