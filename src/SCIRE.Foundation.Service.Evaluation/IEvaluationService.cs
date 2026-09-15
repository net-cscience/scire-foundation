using SCIRE.Foundation.Service.Evaluation.Logging;
using SCIRE.Foundation.Service.Evaluation.Metadata;
using SCIRE.Foundation.Service.Evaluation.State;
using SCIRE.Foundation.Service.Evaluation.Submission;
using SCIRE.Foundation.Service.Evaluation.User;
using SCIRE.Foundation.Service.Evaluation.Diagnostics;

namespace SCIRE.Foundation.Service.Evaluation;

/// <summary>Stateful evaluation client. Reuse one instance per endpoint and logged-in user.</summary>
public interface IEvaluationService
{
    /// <summary>Whether the service currently holds a session that has not been rejected.</summary>
    bool IsAuthenticated { get; }
    /// <summary>Current user and session, or null when logged out or rejected. Treat the session ID as a credential.</summary>
    EvaluationUser? CurrentUser { get; }
    /// <summary>Latest connection and authentication observations.</summary>
    EvaluationServiceStatus Status { get; }
    /// <summary>Raised when status changes. Callbacks may run off the UI thread and must not block.</summary>
    event Action<EvaluationServiceStatus>? StatusChanged;

    /// <summary>Authenticates with DRES and retains the resulting session in memory.</summary>
    Task<EvaluationUser> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    /// <summary>Revokes the session remotely. A failed logout retains it so that logout can be retried.</summary>
    Task LogoutAsync(CancellationToken cancellationToken = default);
    /// <summary>Explicitly discards the local session without contacting DRES.</summary>
    void ClearSession();
    /// <summary>Checks the retained session with the server and refreshes the current user.</summary>
    Task<EvaluationUser> GetUserAsync(CancellationToken cancellationToken = default);

    /// <summary>Lists evaluations available to the participant.</summary>
    Task<IReadOnlyList<EvaluationInfo>> GetEvaluationsAsync(CancellationToken cancellationToken = default);
    /// <summary>Reads detailed state. DRES can deny participant viewing with HTTP 403 while permitting submissions.</summary>
    Task<EvaluationState> GetStateAsync(string evaluationId, CancellationToken cancellationToken = default);
    /// <summary>Reads participant task information. HTTP 404 can mean that no task is currently active.</summary>
    Task<EvaluationTaskTemplateInfo> GetCurrentTaskAsync(string evaluationId, CancellationToken cancellationToken = default);

    /// <summary>Maps and submits one answer to the default evaluation. Submissions are never retried automatically.</summary>
    Task<EvaluationSubmissionResult> SubmitAsync<TScope>(TScope scope, CancellationToken cancellationToken = default);
    /// <summary>Maps and submits one answer to the specified evaluation.</summary>
    Task<EvaluationSubmissionResult> SubmitAsync<TScope>(string evaluationId, TScope scope, CancellationToken cancellationToken = default);
    /// <summary>Submits explicit answer sets to the default evaluation.</summary>
    Task<EvaluationSubmissionResult> SubmitAsync(EvaluationSubmission submission, CancellationToken cancellationToken = default);
    /// <summary>Submits explicit answer sets, optionally targeting task IDs or names for asynchronous evaluations.</summary>
    Task<EvaluationSubmissionResult> SubmitAsync(string evaluationId, EvaluationSubmission submission, CancellationToken cancellationToken = default);

    /// <summary>Sends structured query interaction events to DRES.</summary>
    Task LogQueryAsync(string evaluationId, EvaluationQueryLog log, CancellationToken cancellationToken = default);
    /// <summary>Sends a ranked result list and its associated query events to DRES.</summary>
    Task LogResultsAsync(string evaluationId, EvaluationResultLog log, CancellationToken cancellationToken = default);
    /// <summary>Creates a lightweight logger bound to a fixed evaluation ID, or to the configured default ID.</summary>
    IEvaluationLogger CreateLogger(string? evaluationId = null);

    /// <summary>Reads public server metadata, including its clock and version information.</summary>
    Task<EvaluationMetadata> GetMetadataAsync(CancellationToken cancellationToken = default);
    /// <summary>Probes the clock endpoint and, when logged in, verifies the session. Expected connection errors are returned in Status; caller cancellation propagates.</summary>
    Task<EvaluationServiceStatus> CheckConnectionAsync(CancellationToken cancellationToken = default);
    /// <summary>Probes immediately and then at the requested interval while enumerated. No background task starts implicitly.</summary>
    IAsyncEnumerable<EvaluationServiceStatus> MonitorConnectionAsync(TimeSpan interval, CancellationToken cancellationToken = default);
}
