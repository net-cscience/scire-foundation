using SCIRE.Foundation.Abstractions.Context;
using SCIRE.Foundation.Abstractions.Sources;

namespace SCIRE.Foundation.Runtime.Sources;

/// <summary>A file with a stable identity across the schemas of its owning context.</summary>
public sealed class RegisteredFileSource : ISource
{
    /// <inheritdoc />
    public Guid Id { get; }

    /// <summary>Identifies the context that owns the file registration.</summary>
    public Guid ContextId { get; }

    /// <summary>Gets the registered file path.</summary>
    public string FilePath { get; }

    /// <summary>Creates a source representing a file in the given context.</summary>
    /// <param name="id">Stable identity of the registered file.</param>
    /// <param name="context">Context that owns the registration.</param>
    /// <param name="filePath">Path of the registered file.</param>
    public RegisteredFileSource(Guid id, IContext context, string filePath)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (id == Guid.Empty)
            throw new ArgumentException("A registered source needs an ID.", nameof(id));

        if (context.Id == Guid.Empty)
            throw new ArgumentException("The source Context needs an ID.", nameof(context));

        this.Id = id;
        this.ContextId = context.Id;
        this.FilePath = filePath;
    }
}