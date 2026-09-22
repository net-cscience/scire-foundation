namespace SCIRE.Foundation.Abstractions.Config.Persistence;

/// <summary>Loads configuration instances and privately tracks their original stored state.</summary>
public interface IConfigurationStore<T> where T : class, IPersistableConfig
{
    /// <summary>Loads a tracked instance, using independent defaults when storage is absent.</summary>
    /// <remarks>Loading does not create storage.</remarks>
    Task<T> LoadAsync(string configurationId, Func<T> createDefaults, CancellationToken cancellationToken = default);

    /// <summary>Creates an independent copy that retains the original instance's stored-state baseline.</summary>
    T Clone(T value);

    /// <summary>Saves an instance if storage still matches its privately retained baseline.</summary>
    /// <returns>An independent, tracked copy of the committed value.</returns>
    /// <remarks>
    /// Use the store that loaded the instance. A successful write also refreshes
    /// the original instance's baseline, allowing it to be edited and saved again.
    /// </remarks>
    Task<T> WriteAsync(T value, CancellationToken cancellationToken = default);
}