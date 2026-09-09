namespace SCIRE.Foundation.Service.Evaluation.Dres;

public sealed record DresOptions
{
    public required Uri Endpoint { get; init; }
    public string? DefaultEvaluationId { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(10);
}