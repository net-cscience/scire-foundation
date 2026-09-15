namespace SCIRE.Foundation.Service.Evaluation.Logging;

internal sealed class EvaluationLogger : IEvaluationLogger
{
    private readonly IEvaluationService _service;

    public EvaluationLogger(IEvaluationService service, string evaluationId)
    {
        this._service = service;
        this.EvaluationId = evaluationId;
    }

    public string EvaluationId { get; }

    public Task LogEventAsync(string category, string type, string value, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var queryEvent = new EvaluationQueryEvent(timestamp, category, type, value);
        return this.LogQueryAsync(new EvaluationQueryLog(timestamp, new[] { queryEvent }), cancellationToken);
    }

    public Task LogQueryAsync(EvaluationQueryLog log, CancellationToken cancellationToken = default) => this._service.LogQueryAsync(this.EvaluationId, log, cancellationToken);
    public Task LogResultsAsync(EvaluationResultLog log, CancellationToken cancellationToken = default) => this._service.LogResultsAsync(this.EvaluationId, log, cancellationToken);
}
