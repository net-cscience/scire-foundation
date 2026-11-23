using System.Text.Json.Serialization;

namespace SCIRE.Foundation.Abstractions.Schema;

/// <summary>
/// Records a transition in the lifecycle of a processing job.
/// </summary>
/// <param name="Processing">Processing state established by the change.</param>
/// <param name="ChangedAt">Time at which the change occurred.</param>
/// <param name="ChangedBy">Identifies the worker, application, or actor responsible for the change when known.</param>
/// <param name="Reason">Optional explanation for why the state was changed.</param>
public record ProcessingStateChange(
    [property: JsonRequired, JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    SourceFeatureProcessingState Processing,
    DateTimeOffset ChangedAt,
    string? ChangedBy,
    string? Reason);