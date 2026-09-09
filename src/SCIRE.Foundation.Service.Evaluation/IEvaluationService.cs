using SCIRE.Foundation.Service.Evaluation.Logging;
using SCIRE.Foundation.Service.Evaluation.Metadata;
using SCIRE.Foundation.Service.Evaluation.State;
using SCIRE.Foundation.Service.Evaluation.Submission;
using SCIRE.Foundation.Service.Evaluation.User;

namespace SCIRE.Foundation.Service.Evaluation;

public interface IEvaluationService
{
    bool IsAuthenticated { get; }

    Task<EvaluationUser> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationInfo>> GetEvaluationsAsync(CancellationToken cancellationToken = default);
    Task<EvaluationState> GetStateAsync(string evaluationId, CancellationToken cancellationToken = default);

    Task<EvaluationSubmissionResult> SubmitAsync<TScope>(TScope scope, CancellationToken cancellationToken = default);
    Task<EvaluationSubmissionResult> SubmitAsync<TScope>(string evaluationId, TScope scope,
        CancellationToken cancellationToken = default);

    Task LogQueryAsync(string evaluationId, EvaluationQueryLog log, CancellationToken cancellationToken = default);
    Task LogResultsAsync(string evaluationId, EvaluationResultLog log, CancellationToken cancellationToken = default);

    Task<EvaluationMetadata> GetMetadataAsync(CancellationToken cancellationToken = default);
}