using SCIRE.Foundation.Abstractions.Context;
using SCIRE.Foundation.Abstractions.Schema;

namespace SCIRE.Foundation.Runtime.Context;

/// <summary>
/// Base runtime state for an application-specific Context and Schema selection.
/// </summary>
/// <typeparam name="TContext">Concrete Context type used by the application.</typeparam>
/// <typeparam name="TSchema">Concrete Schema type used by the application.</typeparam>
/// <remarks>
/// When a Schema is selected, it must be defined against the selected Context.
/// Applications may derive from this type to add runtime state specific to their domain.
/// </remarks>
public abstract class ContextRuntimeState<TContext, TSchema>
    where TContext : IContext
    where TSchema : ISchema<TContext>
{
    /// <summary>
    /// Creates runtime state for the provided Context and optional Schema.
    /// </summary>
    /// <param name="context">Context that is active in the application.</param>
    /// <param name="schema">
    /// Optional Schema selected for processing. When provided, it must be defined against
    /// <paramref name="context"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="schema"/> belongs to a different Context.
    /// </exception>
    protected ContextRuntimeState(TContext context, TSchema? schema = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (schema is not null && schema.Context.Id != context.Id)
            throw new ArgumentException("The selected Schema must belong to the selected Context.", nameof(schema));

        this.CurrentContext = context;
        this.CurrentSchema = schema;
    }

    /// <summary>
    /// Context currently active in the application.
    /// </summary>
    public TContext CurrentContext { get; private set; }

    /// <summary>
    /// Schema currently active for processing, or <see langword="null"/> when no Schema is selected.
    /// </summary>
    public TSchema? CurrentSchema { get; private set; }

    /// <summary>
    /// Changes the currently active Context.
    /// </summary>
    /// <param name="context">Context to make active.</param>
    /// <remarks>
    /// The currently selected Schema is cleared when it does not belong to the new Context.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is <see langword="null"/>.
    /// </exception>
    protected void SetContext(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (ReferenceEquals(this.CurrentContext, context))
            return;

        this.CurrentContext = context;

        if (this.CurrentSchema is not null && this.CurrentSchema.Context.Id != context.Id)
            this.CurrentSchema = default;
    }

    /// <summary>
    /// Changes the Schema currently selected for processing.
    /// </summary>
    /// <param name="schema">Schema to select, or <see langword="null"/> to clear the current selection.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="schema"/> belongs to a different Context.
    /// </exception>
    protected void SetSchema(TSchema? schema)
    {
        if (schema is not null && schema.Context.Id != this.CurrentContext.Id)
            throw new ArgumentException("The selected Schema must belong to the selected Context.", nameof(schema));

        this.CurrentSchema = schema;
    }
}