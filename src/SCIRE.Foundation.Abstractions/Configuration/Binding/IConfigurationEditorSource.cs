namespace SCIRE.Foundation.Abstractions.Configuration.Binding;

/// <summary>Provides configuration editing operations without exposing a generic model type.</summary>
public interface IConfigurationEditorSource
{
    Type ConfigurationType { get; }

    Task<IPersistableConfig> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IPersistableConfig configuration, CancellationToken cancellationToken = default);

    Task ReloadAsync(CancellationToken cancellationToken = default);
}