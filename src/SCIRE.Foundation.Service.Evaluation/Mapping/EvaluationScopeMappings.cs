using SCIRE.Foundation.Service.Evaluation.Submission;

namespace SCIRE.Foundation.Service.Evaluation.Mapping;

public sealed class EvaluationScopeMappings
{
    private readonly Dictionary<Type, Func<object, EvaluationSubmissionScope>> _mappings = [];


    public EvaluationScopeMappings Map<TSource, TScope>(Func<TSource, TScope> mapping)
        where TScope : EvaluationSubmissionScope
    {
        ArgumentNullException.ThrowIfNull(mapping);

        this._mappings[typeof(TSource)] = value => mapping((TSource)value);
        return this;
    }


    public EvaluationSubmissionScope Resolve<TSource>(TSource value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return this.Resolve((object)value);
    }


    public EvaluationSubmissionScope Resolve(object value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value is EvaluationSubmissionScope scope)
            return scope;

        var sourceType = value.GetType();

        if (this._mappings.TryGetValue(sourceType, out var exactMapping))
            return exactMapping(value);

        var mappings = this._mappings
            .Where(mapping => mapping.Key.IsAssignableFrom(sourceType))
            .ToList();

        return mappings.Count switch
        {
            1 => mappings[0].Value(value),
            > 1 => throw new InvalidOperationException(
                $"Multiple evaluation submission mappings match '{sourceType.FullName}'."),
            _ => throw new InvalidOperationException(
                $"No evaluation submission mapping registered for '{sourceType.FullName}'.")
        };
    }
}