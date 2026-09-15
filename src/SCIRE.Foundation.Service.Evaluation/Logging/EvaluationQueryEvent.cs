namespace SCIRE.Foundation.Service.Evaluation.Logging;

/// <summary>One retrieval interaction, independent of application diagnostic logging.</summary>
/// <param name="Timestamp">UTC Unix time in milliseconds.</param>
/// <param name="Category">DRES category: TEXT, IMAGE, SKETCH, FILTER, BROWSING, COOPERATION or OTHER.</param>
/// <param name="Type">Interaction type agreed with the evaluation organizers, such as jointEmbedding.</param>
/// <param name="Value">Interaction payload, such as query text.</param>
public sealed record EvaluationQueryEvent(
    long Timestamp,
    string Category,
    string Type,
    string Value
);