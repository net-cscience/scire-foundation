using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Logging;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.State;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Submission;
using SCIRE.Foundation.Service.Evaluation.Logging;
using SCIRE.Foundation.Service.Evaluation.State;
using SCIRE.Foundation.Service.Evaluation.Submission;

namespace SCIRE.Foundation.Service.Evaluation.Dres.Mapping;

internal static class DresEvaluationMappings
{
    private static readonly HashSet<string> EventCategories = new(StringComparer.OrdinalIgnoreCase) { "TEXT", "IMAGE", "SKETCH", "FILTER", "BROWSING", "COOPERATION", "OTHER" };

    public static DresApiClientSubmission ToDres(this EvaluationSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission.AnswerSets);
        if (submission.AnswerSets.Count == 0)
            throw new ArgumentException("A submission must contain at least one answer set.", nameof(submission));
        return new DresApiClientSubmission(submission.AnswerSets.Select(ToDres).ToArray());
    }

    private static DresApiClientAnswerSet ToDres(EvaluationAnswerSet answerSet)
    {
        ArgumentNullException.ThrowIfNull(answerSet);
        ArgumentNullException.ThrowIfNull(answerSet.Answers);
        if (answerSet.Answers.Count == 0)
            throw new ArgumentException("An answer set must contain at least one answer.", nameof(answerSet));
        if (answerSet.TaskId is not null && answerSet.TaskName is not null)
            throw new ArgumentException("Specify either a task ID or a task name.", nameof(answerSet));
        if (answerSet.TaskId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(answerSet.TaskId);
        }
        if (answerSet.TaskName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(answerSet.TaskName);
        }
        return new DresApiClientAnswerSet(answerSet.Answers.Select(ToDresAnswer).ToArray(), answerSet.TaskId, answerSet.TaskName);
    }

    public static DresApiClientAnswer ToDresAnswer(this EvaluationSubmissionScope scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        switch (scope)
        {
            case TextSubmissionScope text:
                ArgumentException.ThrowIfNullOrWhiteSpace(text.Text);
                return new DresApiClientAnswer(Text: text.Text);
            case ItemSubmissionScope item:
                ArgumentException.ThrowIfNullOrWhiteSpace(item.MediaItemName);
                return new DresApiClientAnswer(MediaItemName: item.MediaItemName, MediaItemCollectionName: item.MediaItemCollectionName);
            case TemporalSubmissionScope temporal:
                ArgumentException.ThrowIfNullOrWhiteSpace(temporal.MediaItemName);
                if (temporal.Start < TimeSpan.Zero || temporal.End < temporal.Start)
                    throw new ArgumentException("Temporal offsets must satisfy 0 <= start <= end.", nameof(scope));
                return new DresApiClientAnswer(MediaItemName: temporal.MediaItemName, MediaItemCollectionName: temporal.MediaItemCollectionName, Start: temporal.Start.Ticks / TimeSpan.TicksPerMillisecond, End: temporal.End.Ticks / TimeSpan.TicksPerMillisecond);
            default:
                throw new NotSupportedException($"Submission scope '{scope.GetType().Name}' is not supported.");
        }
    }

    public static DresQueryEvent ToDres(this EvaluationQueryEvent queryEvent)
    {
        ArgumentNullException.ThrowIfNull(queryEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryEvent.Category);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryEvent.Type);
        ArgumentNullException.ThrowIfNull(queryEvent.Value);
        if (!EventCategories.Contains(queryEvent.Category))
            throw new ArgumentException($"Unknown DRES event category '{queryEvent.Category}'.", nameof(queryEvent));
        return new DresQueryEvent(queryEvent.Timestamp, queryEvent.Category.ToUpperInvariant(), queryEvent.Type, queryEvent.Value);
    }

    public static DresQueryEventLog ToDres(this EvaluationQueryLog log)
    {
        ArgumentNullException.ThrowIfNull(log.Events);
        return new DresQueryEventLog(log.Timestamp, log.Events.Select(ToDres).ToArray());
    }

    public static DresQueryResultLog ToDres(this EvaluationResultLog log)
    {
        ArgumentNullException.ThrowIfNull(log.Results);
        ArgumentNullException.ThrowIfNull(log.Events);
        ArgumentNullException.ThrowIfNull(log.SortType);
        ArgumentNullException.ThrowIfNull(log.ResultSetAvailability);
        return new DresQueryResultLog(log.Timestamp, log.SortType, log.ResultSetAvailability, log.Results.Select(ToDres).ToArray(), log.Events.Select(ToDres).ToArray());
    }

    private static DresRankedAnswer ToDres(EvaluationRankedResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.Rank < 1)
            throw new ArgumentOutOfRangeException(nameof(result), "Result ranks are one-based.");
        return new DresRankedAnswer(result.Answer.ToDresAnswer(), result.Rank);
    }

    public static EvaluationTaskTemplateInfo ToEvaluation(this DresApiClientTaskTemplateInfo task) => new(task.Name, task.TaskGroup, task.TaskType, task.Duration);
    public static EvaluationInfo ToEvaluation(this DresApiClientEvaluationInfo evaluation) => new(evaluation.Id, evaluation.Name, evaluation.Type, evaluation.Status, evaluation.TemplateId, evaluation.TemplateDescription, evaluation.Teams, evaluation.TaskTemplates.Select(ToEvaluation).ToArray());
    public static EvaluationState ToEvaluation(this DresApiEvaluationState state) => new(state.EvaluationId, state.EvaluationStatus, state.TaskId, state.TaskStatus, state.TaskTemplateId, state.TimeLeft, state.TimeElapsed);

    public static EvaluationSubmissionResult ToEvaluation(this DresSuccessfulSubmissionsStatus result)
    {
        var verdict = result.Submission switch
        {
            DresVerdictStatus.Correct => EvaluationVerdict.Correct,
            DresVerdictStatus.Wrong => EvaluationVerdict.Wrong,
            DresVerdictStatus.Indeterminate => EvaluationVerdict.Indeterminate,
            DresVerdictStatus.Undecidable => EvaluationVerdict.Undecidable,
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };
        return new EvaluationSubmissionResult(result.Status, verdict, result.Description);
    }
}
