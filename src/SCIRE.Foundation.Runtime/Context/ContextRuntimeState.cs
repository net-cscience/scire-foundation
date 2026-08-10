using SCIRE.Foundation.Abstractions.Context;
using SCIRE.Foundation.Abstractions.Schema;

namespace SCIRE.Foundation.Runtime.Context;

/// <summary>
/// Holds the application-level selection state for SCIRE contexts and schemas.
/// </summary>
/// <remarks>
/// Available contexts are independent of the currently selected context and schema.
/// </remarks>
public class ContextRuntimeState
{
    /// <summary>
    /// Context currently selected by the application, or <see langword="null"/> when none is selected.
    /// </summary>
    public IContext? CurrentContext { get; set; }

    /// <summary>
    /// Schema currently selected for processing, or <see langword="null"/> when none is selected.
    /// </summary>
    public ISchema? CurrentSchema { get; set; }
}