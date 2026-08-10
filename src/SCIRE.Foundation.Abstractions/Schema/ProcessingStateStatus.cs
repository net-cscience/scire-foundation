namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>
/// Defines the lifecycle stages of a processing job.
/// </summary>
public enum ProcessingStateStatus
{
    /// <summary>
    /// The source and feature combination is scheduled for processing but has not yet been prepared.
    /// </summary>
    Planned,

    /// <summary>
    /// Required coordinates are available, but feature processing has not yet started.
    /// </summary>
    Prepared,

    /// <summary>
    /// The processing job has been claimed by a worker.
    /// </summary>
    Reserved,

    /// <summary>
    /// Feature processing is currently being performed.
    /// </summary>
    Processing,

    /// <summary>
    /// Feature processing completed successfully.
    /// </summary>
    Processed,

    /// <summary>
    /// Processing could not be completed successfully.
    /// </summary>
    Error
}