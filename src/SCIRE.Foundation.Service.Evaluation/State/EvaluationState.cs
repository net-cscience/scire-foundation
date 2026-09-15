namespace SCIRE.Foundation.Service.Evaluation.State;

/// <summary>Detailed evaluation state. DRES timeLeft and timeElapsed are seconds, unlike submission offsets and log timestamps.</summary>
/// <param name="EvaluationId">Evaluation ID.</param>
/// <param name="EvaluationStatus">Evaluation status reported by DRES.</param>
/// <param name="TaskId">Active task run ID, if any.</param>
/// <param name="TaskStatus">Current task status.</param>
/// <param name="TaskTemplateId">Selected task template ID, if any.</param>
/// <param name="TimeLeft">Remaining task duration in seconds, if bounded.</param>
/// <param name="TimeElapsed">Elapsed task duration in seconds.</param>
public sealed record EvaluationState(
    string EvaluationId,
    string EvaluationStatus,
    string? TaskId,
    string TaskStatus,
    string? TaskTemplateId,
    long? TimeLeft,
    long TimeElapsed
    )
{
    /// <summary>Typed representation of TimeLeft.</summary>
    public TimeSpan? RemainingTime => this.TimeLeft is { } seconds ? TimeSpan.FromSeconds(seconds) : null;
    /// <summary>Typed representation of TimeElapsed.</summary>
    public TimeSpan ElapsedTime => TimeSpan.FromSeconds(this.TimeElapsed);
}
