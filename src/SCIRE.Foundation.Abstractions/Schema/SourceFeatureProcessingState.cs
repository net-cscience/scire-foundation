namespace SCIRE.Foundation.Abstractions.Schema;


/// <summary>Tracks extraction and integration of a feature for one source within a schema.</summary>
public enum SourceFeatureProcessingState
{
    /// <summary>Initial or reset state. A worker can claim the pair when its selection is <see cref="SourceFeatureSelectionState.Selected"/>.</summary>
    Unprocessed = 0,

    /// <summary>A rerun has been requested. Existing outputs are retained until the rerun starts.</summary>
    /// <remarks>A worker can claim the rerun unless the pair is <see cref="SourceFeatureSelectionState.Excluded"/>.</remarks>
    Renew = 1,

    /// <summary>A worker has claimed the pair and may start extraction.</summary>
    Reserved = 2,

    /// <summary>The worker holding the reservation is extracting the feature.</summary>
    Processing = 3,

    /// <summary>Extraction completed and its output is ready for integration. An integration failure leaves the pair in this state.</summary>
    Successful = 4,

    /// <summary>Extraction failed. The pair awaits an explicit retry or rerun request.</summary>
    Failed = 5,

    /// <summary>Integration of the extracted output into the schema has committed.</summary>
    Integrated = 6
}