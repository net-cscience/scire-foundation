using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SCIRE.Foundation.Service.Evaluation.Diagnostics;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Metadata;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.State;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Submission;
using SCIRE.Foundation.Service.Evaluation.Dres.Mapping;
using SCIRE.Foundation.Service.Evaluation.Logging;
using SCIRE.Foundation.Service.Evaluation.Mapping;
using SCIRE.Foundation.Service.Evaluation.Metadata;
using SCIRE.Foundation.Service.Evaluation.State;
using SCIRE.Foundation.Service.Evaluation.Submission;
using SCIRE.Foundation.Service.Evaluation.User;

namespace SCIRE.Foundation.Service.Evaluation.Dres;

/// <summary>Handwritten DRES v2 client. Reuse one instance per endpoint and user; no polling or retries start implicitly.</summary>
public sealed partial class DresEvaluationService : IEvaluationService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly DresOptions _options;
    private readonly EvaluationScopeMappings _mappings;
    private readonly ILogger<DresEvaluationService> _logger;
    private readonly Uri _baseUri;
    private readonly object _statusLock = new();
    private readonly SemaphoreSlim _authenticationGate = new(1, 1);
    private EvaluationServiceStatus _status;
    private DresSession? _session;
    private long _sessionRevision;
    private bool _ownsHttpClient;
    private int _disposed;

    /// <summary>Uses a caller-owned HttpClient without changing its properties or disposing it. Prefer a dedicated client with automatic cookies disabled.</summary>
    public DresEvaluationService(HttpClient httpClient, DresOptions options, EvaluationScopeMappings mappings, ILogger<DresEvaluationService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(mappings);
        options.Validate();
        this._httpClient = httpClient;
        this._options = options;
        this._mappings = mappings;
        this._logger = logger ?? NullLogger<DresEvaluationService>.Instance;
        this._baseUri = new Uri(options.Endpoint.AbsoluteUri.TrimEnd('/') + "/");
        this._status = new EvaluationServiceStatus(options.Endpoint);
    }

    /// <summary>Creates a service with an owned transport and explicit session authentication. Dispose the returned service when finished.</summary>
    public static DresEvaluationService Create(DresOptions options, EvaluationScopeMappings? mappings = null, ILogger<DresEvaluationService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        var handler = new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = false, PooledConnectionLifetime = TimeSpan.FromMinutes(2) };
        var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        return new DresEvaluationService(client, options, mappings ?? new EvaluationScopeMappings(), logger) { _ownsHttpClient = true };
    }

    /// <inheritdoc />
    public bool IsAuthenticated => this.CurrentUser is not null;

    /// <inheritdoc />
    public EvaluationUser? CurrentUser
    {
        get { lock (this._statusLock) { return this._session?.User; } }
    }

    /// <inheritdoc />
    public EvaluationServiceStatus Status
    {
        get { lock (this._statusLock) { return this._status; } }
    }

    /// <inheritdoc />
    public event Action<EvaluationServiceStatus>? StatusChanged;

    /// <inheritdoc />
    public async Task<IReadOnlyList<EvaluationInfo>> GetEvaluationsAsync(CancellationToken cancellationToken = default)
    {
        var evaluations = await this.SendJsonAsync<List<DresApiClientEvaluationInfo>>(HttpMethod.Get, "api/v2/client/evaluation/list", null, this.RequireUser(), cancellationToken).ConfigureAwait(false);
        return evaluations.Select(x => x.ToEvaluation()).ToArray();
    }

    /// <inheritdoc />
    public async Task<EvaluationState> GetStateAsync(string evaluationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        var path = $"api/v2/evaluation/{Uri.EscapeDataString(evaluationId)}/state";
        var state = await this.SendJsonAsync<DresApiEvaluationState>(HttpMethod.Get, path, null, this.RequireUser(), cancellationToken).ConfigureAwait(false);
        return state.ToEvaluation();
    }

    /// <inheritdoc />
    public async Task<EvaluationTaskTemplateInfo> GetCurrentTaskAsync(string evaluationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        var path = $"api/v2/client/evaluation/currentTask/{Uri.EscapeDataString(evaluationId)}";
        var task = await this.SendJsonAsync<DresApiClientTaskTemplateInfo>(HttpMethod.Get, path, null, this.RequireUser(), cancellationToken).ConfigureAwait(false);
        return task.ToEvaluation();
    }

    /// <inheritdoc />
    public Task<EvaluationSubmissionResult> SubmitAsync<TScope>(TScope scope, CancellationToken cancellationToken = default) => this.SubmitAsync(this.ResolveEvaluationId(null), scope, cancellationToken);

    /// <inheritdoc />
    public Task<EvaluationSubmissionResult> SubmitAsync<TScope>(string evaluationId, TScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var answer = this._mappings.Resolve(scope);
        var submission = new EvaluationSubmission(new[] { new EvaluationAnswerSet(new[] { answer }) });
        return this.SubmitAsync(evaluationId, submission, cancellationToken);
    }

    /// <inheritdoc />
    public Task<EvaluationSubmissionResult> SubmitAsync(EvaluationSubmission submission, CancellationToken cancellationToken = default) => this.SubmitAsync(this.ResolveEvaluationId(null), submission, cancellationToken);

    /// <inheritdoc />
    public async Task<EvaluationSubmissionResult> SubmitAsync(string evaluationId, EvaluationSubmission submission, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        ArgumentNullException.ThrowIfNull(submission);
        var path = $"api/v2/submit/{Uri.EscapeDataString(evaluationId)}";
        var result = await this.SendJsonAsync<DresSuccessfulSubmissionsStatus>(HttpMethod.Post, path, submission.ToDres(), this.RequireUser(), cancellationToken).ConfigureAwait(false);
        return result.ToEvaluation();
    }

    /// <inheritdoc />
    public Task LogQueryAsync(string evaluationId, EvaluationQueryLog log, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        ArgumentNullException.ThrowIfNull(log);
        return this.SendJsonAsync<DresSuccessStatus>(HttpMethod.Post, $"api/v2/log/query/{Uri.EscapeDataString(evaluationId)}", log.ToDres(), this.RequireUser(), cancellationToken);
    }

    /// <inheritdoc />
    public Task LogResultsAsync(string evaluationId, EvaluationResultLog log, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        ArgumentNullException.ThrowIfNull(log);
        return this.SendJsonAsync<DresSuccessStatus>(HttpMethod.Post, $"api/v2/log/result/{Uri.EscapeDataString(evaluationId)}", log.ToDres(), this.RequireUser(), cancellationToken);
    }

    /// <inheritdoc />
    public IEvaluationLogger CreateLogger(string? evaluationId = null) => new EvaluationLogger(this, this.ResolveEvaluationId(evaluationId));

    /// <inheritdoc />
    public async Task<EvaluationMetadata> GetMetadataAsync(CancellationToken cancellationToken = default)
    {
        var time = await this.SendJsonAsync<DresCurrentTime>(HttpMethod.Get, "api/v2/status/time", null, null, cancellationToken).ConfigureAwait(false);
        var info = await this.SendJsonAsync<DresServerInfo>(HttpMethod.Get, "api/v2/status/info", null, null, cancellationToken).ConfigureAwait(false);
        return new EvaluationMetadata("DRES", this._options.Endpoint, time.TimeStamp) { Version = info.Version, ServerStartTimestamp = info.StartTime, ServerUptimeMilliseconds = info.Uptime };
    }

    /// <inheritdoc />
    public async Task<EvaluationServiceStatus> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await this.SendJsonAsync<DresCurrentTime>(HttpMethod.Get, "api/v2/status/time", null, null, cancellationToken).ConfigureAwait(false);
            if (this.GetSession() is { } session)
            {
                await this.RefreshUserAsync(session, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is DresApiException or HttpRequestException or TimeoutException)
        {
            // The transport already recorded the failure. Health probes return the observation.
        }
        cancellationToken.ThrowIfCancellationRequested();
        return this.Status;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<EvaluationServiceStatus> MonitorConnectionAsync(TimeSpan interval, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (interval <= TimeSpan.Zero || interval.TotalMilliseconds > uint.MaxValue - 1)
            throw new ArgumentOutOfRangeException(nameof(interval));
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return await this.CheckConnectionAsync(cancellationToken).ConfigureAwait(false);
            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }
    }

    private string ResolveEvaluationId(string? evaluationId)
    {
        var result = evaluationId ?? this._options.DefaultEvaluationId;
        if (string.IsNullOrWhiteSpace(result))
            throw new InvalidOperationException("Specify an evaluation ID or configure DefaultEvaluationId.");
        return result;
    }

    /// <summary>Disposes the owned transport, if any, and discards the local session. Does not perform remote logout.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref this._disposed, 1) != 0)
            return;
        this.ClearSession();
        if (this._ownsHttpClient)
        {
            this._httpClient.Dispose();
        }
    }
}
