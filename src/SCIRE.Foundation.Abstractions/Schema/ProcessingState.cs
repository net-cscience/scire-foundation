using System.Text.Json.Serialization;
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

    /// <summary>Selection of this feature for this source within the schema.</summary>
    [JsonInclude]
    public SourceFeatureSelectionState Selection { get; private set; } = SourceFeatureSelectionState.None;

    /// <summary>Current extraction and integration state.</summary>
    [JsonRequired, JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public SourceFeatureProcessingState Processing { get; set; } = SourceFeatureProcessingState.Unprocessed;

    /// <summary>Selects or unselects this pair for future processing.</summary>
    public void SetSelected(bool selected)
    {
        if (this.Selection == SourceFeatureSelectionState.Excluded)
            throw new InvalidOperationException(
                "Recheck compatibility before changing the selection of an excluded pair.");

        this.Selection = selected ? SourceFeatureSelectionState.Selected : SourceFeatureSelectionState.Unselected;
    }

    /// <summary>Updates the selection after checking source/feature compatibility.</summary>
    public void SetCompatibility(bool compatible)
    {
        if (!compatible)
        {
            this.Selection = SourceFeatureSelectionState.Excluded;
        }
        else if (this.Selection == SourceFeatureSelectionState.Excluded)
        {
            this.Selection = SourceFeatureSelectionState.None;
        }
    }

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