using SCIRE.Foundation.Abstractions.Context;
using SCIRE.Foundation.Abstractions.Schema;

namespace SCIRE.Foundation.Runtime.Context;

/// <summary>
/// Holds the application-level selection state for SCIRE contexts and schemas.
/// </summary>
/// <remarks>
/// This state is intentionally separate from the context and schema abstractions themselves.
/// A context may be available to an application without being the currently selected context.
/// </remarks>
public class ContextRuntimeState
{
    /// <summary>
    /// Contexts known to the application and available for selection.
    /// Availability does not imply that a context is currently active or fully loaded.
    /// </summary>
    public IEnumerable<IContext> AvailableContexts { get; }


    /// <summary>
    /// Context currently selected by the application, or <see langword="null"/> when no context is selected.
    /// </summary>
    public IContext? CurrentContext { get; set; }

    /// <summary>
    /// Schema currently selected for processing, or <see langword="null"/> when no schema is selected.
    /// </summary>
    public ISchema? CurrentSchema { get; set; }

}