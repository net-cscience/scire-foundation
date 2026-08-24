using SCIRE.Foundation.Abstractions.Features;
using SCIRE.Foundation.Abstractions.Schema;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Abstractions.Context;

/// <summary>
/// Base implementation for a SCIRE Context defining one semantic processing universe.
/// </summary>
public abstract class ContextBase : IContext
{
    private readonly List<ISchema> availableSchemas = [];

    /// <summary>
    /// Creates an empty Context for persistence materialization.
    /// </summary>
    protected ContextBase()
    {
    }

    /// <summary>
    /// Creates a Context with stable identity and a human-readable name.
    /// </summary>
    /// <param name="id">Stable identity used to reference the Context.</param>
    /// <param name="name">Human-readable name used to distinguish the Context.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is empty or <paramref name="name"/> is empty or whitespace.
    /// </exception>
    protected ContextBase(Guid id, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("The context ID must not be empty.", nameof(id));

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        this.Id = id;
        this.Name = name.Trim();
    }

    /// <inheritdoc />
    public Guid Id { get; protected set; }

    /// <inheritdoc />
    public string Name { get; protected set; } = string.Empty;

    /// <inheritdoc />
    public abstract ISources AvailableSources { get; }

    /// <inheritdoc />
    public abstract IEnumerable<IFeatureDescription> AvailableFeatures { get; }

    /// <inheritdoc />
    public IEnumerable<ISchema> AvailableSchemas => this.availableSchemas;

    /// <summary>
    /// Adds a Schema that is available within this Context.
    /// </summary>
    /// <param name="schema">Schema to make available in this Context.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="schema"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the Schema belongs to another Context or a Schema with the same identity is already available.
    /// </exception>
    public void AddSchema(ISchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        if (schema.Context.Id != this.Id)
            throw new ArgumentException("The schema must belong to this context.", nameof(schema));

        if (this.availableSchemas.Any(candidate => candidate.Id == schema.Id))
            throw new ArgumentException($"Schema '{schema.Id}' is already available in this context.", nameof(schema));

        this.availableSchemas.Add(schema);
    }
}