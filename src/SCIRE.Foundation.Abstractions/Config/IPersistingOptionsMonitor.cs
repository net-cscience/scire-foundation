using Microsoft.Extensions.Options;

namespace SCIRE.Foundation.Abstractions.Config;

/// <summary>Provides independent reader values and persistence for one configuration.</summary>
/// <remarks>Only the default options name is supported. CurrentValue returns a reader copy; use LoadAsync to obtain an editable instance.</remarks>
public interface IPersistingOptionsMonitor<T> : IOptionsMonitor<T> where T : class, IPersistableConfig
{
    /// <summary>Loads an independent editable instance without changing the published value.</summary>
    Task<T> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates, conditionally saves, and publishes an instance loaded by this monitor.</summary>
    /// <remarks>Do not modify or otherwise use the instance until saving finishes. Successful storage acceptance refreshes its private baseline, allowing another save.</remarks>
    Task SaveAsync(T value, CancellationToken cancellationToken = default);

    /// <summary>Reloads storage and publishes it only if validation succeeds.</summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);
}