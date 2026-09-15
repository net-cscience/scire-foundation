
namespace SCIRE.Foundation.Service.Evaluation.Submission;

/// <summary>A text answer for a DRES task.</summary>
/// <param name="Text">Non-empty answer text.</param>
public sealed record TextSubmissionScope(string Text) : EvaluationSubmissionScope;