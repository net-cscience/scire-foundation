namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>
/// Defines when processing states are created for selected source and feature combinations.
/// </summary>
public enum ProcessingStateCreationMode
{
    /// <summary>
    /// Creates a processing state only when processing is explicitly requested.
    /// </summary>
    OnDemand,

    /// <summary>
    /// Creates processing states when a source is added to the schema.
    /// </summary>
    OnSourceAdded
}