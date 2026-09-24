namespace SCIRE.Foundation.Abstractions.Config;

/// <summary>Identifies configuration data that can be persisted.</summary>
/// <remarks>The store owns storage identity and conflict tracking.</remarks>
public interface IPersistableConfig : IConfig
{
}