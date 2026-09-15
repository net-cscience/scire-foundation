namespace SCIRE.Foundation.Service.Evaluation.Submission;

/// <summary>Answer verdict returned by DRES.</summary>
public enum EvaluationVerdict
{
    /// <summary>The answer was judged correct.</summary>
    Correct,
    /// <summary>The answer was judged wrong.</summary>
    Wrong,
    /// <summary>No definitive verdict has been determined.</summary>
    Indeterminate,
    /// <summary>The answer cannot be decided by the applicable judgement process.</summary>
    Undecidable
}