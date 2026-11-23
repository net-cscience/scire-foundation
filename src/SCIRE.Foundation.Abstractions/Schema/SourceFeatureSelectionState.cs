

namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>Specifies whether a feature is selected for a source within one schema.</summary>
public enum SourceFeatureSelectionState
{
    /// <summary>Default for a pair with no recorded selection decision.</summary>
    None = 0,

    /// <summary>The feature has been explicitly deselected for ordinary extraction.</summary>
    Unselected = 1,

    /// <summary>The feature is requested for this source. A worker can claim the pair when it is unprocessed.</summary>
    Selected = 2,

    /// <summary>The feature is incompatible with this source. A successful compatibility recheck returns the pair to <see cref="None"/>.</summary>
    Excluded = 3
}