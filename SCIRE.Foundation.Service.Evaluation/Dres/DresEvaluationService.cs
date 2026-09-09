using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Logging;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Metadata;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.State;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Submission;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.User;
using SCIRE.Foundation.Service.Evaluation.Logging;
using SCIRE.Foundation.Service.Evaluation.Mapping;
using SCIRE.Foundation.Service.Evaluation.Metadata;
using SCIRE.Foundation.Service.Evaluation.State;
using SCIRE.Foundation.Service.Evaluation.Submission;
using SCIRE.Foundation.Service.Evaluation.User;

namespace SCIRE.Foundation.Service.Evaluation.Dres;

public sealed class DresEvaluationService : IEvaluationService
{
    private readonly HttpClient _httpClient;
    private readonly DresOptions _options;
    private readonly EvaluationScopeMappings _mappings;
    private readonly JsonSerializerOptions _jsonOptions;

    private string? _sessionId;


    public DresEvaluationService(HttpClient httpClient, DresOptions options, EvaluationScopeMappings mappings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(mappings);

        this._httpClient = httpClient;
        this._options = options;
        this._mappings = mappings;

        this._httpClient.BaseAddress = EnsureTrailingSlash(options.Endpoint);
        this._httpClient.Timeout = options.Timeout;

        this._jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        this._jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }


    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(this._sessionId);

    private async Task<string> ReadTextAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            await this.ThrowApiExceptionAsync(response, cancellationToken);
            throw new UnreachableException();
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task ThrowApiExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string? description = null;

        try
        {
            var error = await response.Content.ReadFromJsonAsync<DresErrorStatus>(
                this._jsonOptions, cancellationToken);

            description = error?.Description;
        }
        catch (JsonException)
        {
            description = await response.Content.ReadAsStringAsync(cancellationToken);
        }

        throw new DresApiException(
            response.StatusCode,
            string.IsNullOrWhiteSpace(description)
                ? $"DRES request failed with status {(int)response.StatusCode} ({response.StatusCode})."
                : description);
    }

    public async Task<EvaluationUser> LoginAsync(string username, string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var request = new DresLoginRequest(username, password);

        using var response = await this._httpClient.PostAsJsonAsync(
            "api/v2/login", request, this._jsonOptions, cancellationToken);

        var user = await this.ReadAsync<DresApiUser>(response, cancellationToken);

        var sessionId = user.SessionId;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            using var sessionResponse = await this._httpClient.GetAsync("api/v2/user/session", cancellationToken);
            sessionId = await this.ReadTextAsync(sessionResponse, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(sessionId))
            throw new DresApiException(response.StatusCode, "DRES login succeeded but no session ID could be resolved.");

        this._sessionId = sessionId;

        return new EvaluationUser(
            user.Id,
            user.Username,
            user.Role,
            sessionId);
    }


    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (!this.IsAuthenticated)
            return;

        using var response = await this._httpClient.GetAsync(
            this.CreateAuthenticatedUri("api/v2/logout"), cancellationToken);

        var result = await this.ReadAsync<DresSuccessStatus>(response, cancellationToken);

        if (!result.Status)
            throw new DresApiException(response.StatusCode, result.Description);

        this._sessionId = null;
    }


    public async Task<IReadOnlyList<EvaluationInfo>> GetEvaluationsAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await this._httpClient.GetAsync(
            this.CreateAuthenticatedUri("api/v2/client/evaluation/list"), cancellationToken);

        var evaluations = await this.ReadAsync<List<DresApiClientEvaluationInfo>>(response, cancellationToken);

        return evaluations
            .Select(evaluation => new EvaluationInfo(
                evaluation.Id,
                evaluation.Name,
                evaluation.Type,
                evaluation.Status,
                evaluation.TemplateId,
                evaluation.TemplateDescription,
                evaluation.Teams,
                evaluation.TaskTemplates
                    .Select(task => new EvaluationTaskTemplateInfo(
                        task.Name,
                        task.TaskGroup,
                        task.TaskType,
                        task.Duration))
                    .ToList()))
            .ToList();
    }


    public async Task<EvaluationState> GetStateAsync(string evaluationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        this.RequireAuthentication();

        var endpoint = $"api/v2/evaluation/{Uri.EscapeDataString(evaluationId)}/state";

        using var response = await this._httpClient.GetAsync(endpoint, cancellationToken);
        var state = await this.ReadAsync<DresApiEvaluationState>(response, cancellationToken);

        return new EvaluationState(
            state.EvaluationId,
            state.EvaluationStatus,
            state.TaskId,
            state.TaskStatus,
            state.TaskTemplateId,
            state.TimeLeft,
            state.TimeElapsed);
    }


    public async Task<EvaluationSubmissionResult> SubmitAsync<TScope>(
        TScope scope, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(this._options.DefaultEvaluationId))
            throw new InvalidOperationException("No default evaluation ID has been configured.");

        return await this.SubmitAsync(this._options.DefaultEvaluationId, scope, cancellationToken);
    }


    public async Task<EvaluationSubmissionResult> SubmitAsync<TScope>(
        string evaluationId, TScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        ArgumentNullException.ThrowIfNull(scope);

        var submissionScope = this._mappings.Resolve(scope);
        var submission = CreateSubmission(submissionScope);

        var endpoint = this.CreateAuthenticatedUri(
            $"api/v2/submit/{Uri.EscapeDataString(evaluationId)}");

        using var response = await this._httpClient.PostAsJsonAsync(
            endpoint, submission, this._jsonOptions, cancellationToken);

        var result = await this.ReadAsync<DresSuccessfulSubmissionsStatus>(response, cancellationToken);

        return new EvaluationSubmissionResult(
            result.Status,
            MapVerdict(result.Submission),
            result.Description);
    }


    public async Task LogQueryAsync(string evaluationId, EvaluationQueryLog log,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        ArgumentNullException.ThrowIfNull(log);

        var request = new DresQueryEventLog(
            log.Timestamp,
            log.Events
                .Select(MapEvent)
                .ToList());

        var endpoint = this.CreateAuthenticatedUri(
            $"api/v2/log/query/{Uri.EscapeDataString(evaluationId)}");

        using var response = await this._httpClient.PostAsJsonAsync(
            endpoint, request, this._jsonOptions, cancellationToken);

        var result = await this.ReadAsync<DresSuccessStatus>(response, cancellationToken);

        if (!result.Status)
            throw new DresApiException(response.StatusCode, result.Description);
    }


    public async Task LogResultsAsync(string evaluationId, EvaluationResultLog log,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evaluationId);
        ArgumentNullException.ThrowIfNull(log);

        var request = new DresQueryResultLog(
            log.Timestamp,
            log.SortType,
            log.ResultSetAvailability,
            log.Results
                .Select(result => new DresRankedAnswer(
                    CreateAnswer(result.Answer),
                    result.Rank))
                .ToList(),
            log.Events
                .Select(MapEvent)
                .ToList());

        var endpoint = this.CreateAuthenticatedUri(
            $"api/v2/log/result/{Uri.EscapeDataString(evaluationId)}");

        using var response = await this._httpClient.PostAsJsonAsync(
            endpoint, request, this._jsonOptions, cancellationToken);

        var result = await this.ReadAsync<DresSuccessStatus>(response, cancellationToken);

        if (!result.Status)
            throw new DresApiException(response.StatusCode, result.Description);
    }


    public async Task<EvaluationMetadata> GetMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await this._httpClient.GetAsync("api/v2/status/time", cancellationToken);
        var currentTime = await this.ReadAsync<DresCurrentTime>(response, cancellationToken);

        return new EvaluationMetadata(
            "DRES",
            this._options.Endpoint,
            currentTime.TimeStamp);
    }


    private static DresApiClientSubmission CreateSubmission(EvaluationSubmissionScope scope)
    {
        return new DresApiClientSubmission([
            new DresApiClientAnswerSet([
                CreateAnswer(scope)
            ])
        ]);
    }


    private static DresApiClientAnswer CreateAnswer(EvaluationSubmissionScope scope)
    {
        return scope switch
        {
            TextSubmissionScope text => new DresApiClientAnswer(
                Text: text.Text),

            ItemSubmissionScope item => new DresApiClientAnswer(
                MediaItemName: item.MediaItemName,
                MediaItemCollectionName: item.MediaItemCollectionName),

            TemporalSubmissionScope temporal => new DresApiClientAnswer(
                MediaItemName: temporal.MediaItemName,
                MediaItemCollectionName: temporal.MediaItemCollectionName,
                Start: (long)temporal.Start.TotalMilliseconds,
                End: (long)temporal.End.TotalMilliseconds),

            _ => throw new NotSupportedException(
                $"Submission scope '{scope.GetType().Name}' is not supported.")
        };
    }


    private static DresQueryEvent MapEvent(EvaluationQueryEvent queryEvent)
    {
        return new DresQueryEvent(
            queryEvent.Timestamp,
            queryEvent.Category.ToUpperInvariant(),
            queryEvent.Type,
            queryEvent.Value);
    }


    private static EvaluationVerdict MapVerdict(DresVerdictStatus verdict)
    {
        return verdict switch
        {
            DresVerdictStatus.Correct => EvaluationVerdict.Correct,
            DresVerdictStatus.Wrong => EvaluationVerdict.Wrong,
            DresVerdictStatus.Indeterminate => EvaluationVerdict.Indeterminate,
            DresVerdictStatus.Undecidable => EvaluationVerdict.Undecidable,
            _ => throw new ArgumentOutOfRangeException(nameof(verdict))
        };
    }


    private Uri CreateAuthenticatedUri(string path)
    {
        this.RequireAuthentication();

        var separator = path.Contains('?') ? '&' : '?';
        var session = Uri.EscapeDataString(this._sessionId!);

        return new Uri($"{path}{separator}session={session}", UriKind.Relative);
    }


    private void RequireAuthentication()
    {
        if (!this.IsAuthenticated)
            throw new InvalidOperationException("The evaluation service is not authenticated.");
    }


    private async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
            await this.ThrowApiExceptionAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<T>(
            this._jsonOptions, cancellationToken);

        return result ?? throw new DresApiException(
            response.StatusCode,
            $"DRES returned an empty response for '{response.RequestMessage?.RequestUri}'.");
    }

    private static Uri EnsureTrailingSlash(Uri endpoint)
    {
        var value = endpoint.AbsoluteUri.EndsWith('/')
            ? endpoint.AbsoluteUri
            : endpoint.AbsoluteUri + "/";

        return new Uri(value);
    }
}