using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Logging;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Metadata;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.State;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.Submission;
using SCIRE.Foundation.Service.Evaluation.Dres.Dto.User;
using SCIRE.Foundation.Service.Evaluation.Logging;
using SCIRE.Foundation.Service.Evaluation.Metadata;
using SCIRE.Foundation.Service.Evaluation.State;
using SCIRE.Foundation.Service.Evaluation.Submission;
using SCIRE.Foundation.Service.Evaluation.User;

namespace SCIRE.Foundation.Service.Evaluation.Dres.Mapping;

internal static class DresEvaluationMappings
{
    extension(EvaluationSubmissionScope scope)
    {
        public DresApiClientSubmission ToDres()
        {
            return new DresApiClientSubmission([
                new DresApiClientAnswerSet([
                    scope.ToDresAnswer()
                ])
            ]);
        }

        public DresApiClientAnswer ToDresAnswer()
        {
            return scope switch
            {
                TextSubmissionScope text => new DresApiClientAnswer(Text: text.Text),

                ItemSubmissionScope item => new DresApiClientAnswer(
                    MediaItemName: item.MediaItemName,
                    MediaItemCollectionName: item.MediaItemCollectionName),

                TemporalSubmissionScope temporal => new DresApiClientAnswer(
                    MediaItemName: temporal.MediaItemName,
                    MediaItemCollectionName: temporal.MediaItemCollectionName,
                    Start: (long)temporal.Start.TotalMilliseconds,
                    End: (long)temporal.End.TotalMilliseconds),

                _ => throw new NotSupportedException($"Submission scope '{scope.GetType().Name}' is not supported.")
            };
        }
    }


    extension(EvaluationQueryEvent queryEvent)
    {
        public DresQueryEvent ToDres()
        {
            return new DresQueryEvent(
                queryEvent.Timestamp,
                queryEvent.Category.ToUpperInvariant(),
                queryEvent.Type,
                queryEvent.Value);
        }
    }


    extension(EvaluationQueryLog log)
    {
        public DresQueryEventLog ToDres()
        {
            return new DresQueryEventLog(
                log.Timestamp,
                log.Events.Select(x => x.ToDres()).ToList());
        }
    }


    extension(EvaluationResultLog log)
    {
        public DresQueryResultLog ToDres()
        {
            return new DresQueryResultLog(
                log.Timestamp,
                log.SortType,
                log.ResultSetAvailability,
                log.Results
                    .Select(result => new DresRankedAnswer(
                        result.Answer.ToDresAnswer(),
                        result.Rank))
                    .ToList(),
                log.Events.Select(x => x.ToDres()).ToList());
        }
    }


    extension(DresApiUser user)
    {
        public EvaluationUser ToEvaluation(string sessionId)
        {
            return new EvaluationUser(
                user.Id,
                user.Username,
                user.Role,
                sessionId);
        }
    }


    extension(DresApiClientTaskTemplateInfo task)
    {
        public EvaluationTaskTemplateInfo ToEvaluation()
        {
            return new EvaluationTaskTemplateInfo(
                task.Name,
                task.TaskGroup,
                task.TaskType,
                task.Duration);
        }
    }


    extension(DresApiClientEvaluationInfo evaluation)
    {
        public EvaluationInfo ToEvaluation()
        {
            return new EvaluationInfo(
                evaluation.Id,
                evaluation.Name,
                evaluation.Type,
                evaluation.Status,
                evaluation.TemplateId,
                evaluation.TemplateDescription,
                evaluation.Teams,
                evaluation.TaskTemplates.Select(x => x.ToEvaluation()).ToList());
        }
    }


    extension(DresApiEvaluationState state)
    {
        public EvaluationState ToEvaluation()
        {
            return new EvaluationState(
                state.EvaluationId,
                state.EvaluationStatus,
                state.TaskId,
                state.TaskStatus,
                state.TaskTemplateId,
                state.TimeLeft,
                state.TimeElapsed);
        }
    }


    extension(DresSuccessfulSubmissionsStatus result)
    {
        public EvaluationSubmissionResult ToEvaluation()
        {
            return new EvaluationSubmissionResult(
                result.Status,
                result.Submission.ToEvaluation(),
                result.Description);
        }
    }


    extension(DresVerdictStatus verdict)
    {
        public EvaluationVerdict ToEvaluation()
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
    }


    extension(DresCurrentTime currentTime)
    {
        public EvaluationMetadata ToEvaluation(Uri endpoint)
        {
            return new EvaluationMetadata(
                "DRES",
                endpoint,
                currentTime.TimeStamp);
        }
    }
}