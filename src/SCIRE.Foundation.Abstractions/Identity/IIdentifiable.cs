namespace SCIRE.Foundation.Abstractions.Identity;

/// <summary>
/// Represents an object that can be referenced through a stable identity.
/// </summary>
public interface IIdentifiable
{
    /// <summary>
    /// Stable identifier used to reference the object independently of its concrete representation.
    /// </summary>
    Guid Id { get; }
}