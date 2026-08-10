using SCIRE.Foundation.Abstractions.Context;
using SCIRE.Foundation.Abstractions.Schema;

namespace SCIRE.Foundation.Runtime.Context;

/// <summary>
/// Represents the Context and Schema currently selected by an application.
/// </summary>
/// <remarks>
/// When a Schema is selected, it must be defined against the selected Context.
/// </remarks>
public sealed class ContextRuntimeState
{
    private ContextRuntimeState(IContext context, ISchema? schema)
    {
        this.CurrentContext = context;
        this.CurrentSchema = schema;
    }

    /// <summary>
    /// Context currently active in the application.
    /// </summary>
    public IContext CurrentContext { get; }

    /// <summary>
    /// Schema currently active for processing, or <see langword="null"/> when no Schema is selected.
    /// </summary>
    public ISchema? CurrentSchema { get; }

    /// <summary>
    /// Creates runtime selection state for the provided Context and optional Schema.
    /// </summary>
    /// <param name="context">Context that becomes active.</param>
    /// <param name="schema">
    /// Optional Schema to select. When provided, it must be defined against <paramref name="context"/>.
    /// </param>
    /// <returns>A runtime state representing the validated Context and Schema selection.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="schema"/> belongs to a different Context.
    /// </exception>
    public static ContextRuntimeState Create(IContext context, ISchema? schema = null)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (schema is not null && schema.Context.Id != context.Id)
            throw new ArgumentException(
                "The selected schema must belong to the selected context.",
                nameof(schema));

        return new ContextRuntimeState(context, schema);
    }
}