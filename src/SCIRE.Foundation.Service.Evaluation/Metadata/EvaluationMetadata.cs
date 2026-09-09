namespace SCIRE.Foundation.Service.Evaluation.Metadata;

public sealed record EvaluationMetadata(
    string Provider,
    Uri Endpoint,
    long ServerTimestamp
);