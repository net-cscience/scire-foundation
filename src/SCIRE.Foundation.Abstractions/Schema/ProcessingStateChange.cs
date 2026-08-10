namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>
/// Records a transition in the lifecycle of a processing job.
/// </summary>
/// <param name="State">Lifecycle state established by the change.</param>
/// <param name="ChangedAt">Time at which the change occurred.</param>
/// <param name="ChangedBy">Identifies the worker, application, or actor responsible for the change when known.</param>
/// <param name="Reason">Optional explanation for why the state was changed.</param>
public record ProcessingStateChange(
    ProcessingStateStatus State,
    DateTimeOffset ChangedAt,
    string? ChangedBy,
    string? Reason);