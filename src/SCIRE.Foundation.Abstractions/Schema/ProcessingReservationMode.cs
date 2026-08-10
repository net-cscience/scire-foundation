namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>
/// Defines how processing jobs are claimed before execution.
/// </summary>
public enum ProcessingReservationMode
{
    /// <summary>
    /// A worker must explicitly claim a processing job before execution.
    /// </summary>
    Manual,

    /// <summary>
    /// Selecting an available processing job also claims it for the requesting worker.
    /// </summary>
    Auto

}