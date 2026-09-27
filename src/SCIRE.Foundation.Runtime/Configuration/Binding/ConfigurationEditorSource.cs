using System;
using System.Threading;
using System.Threading.Tasks;
using SCIRE.Foundation.Abstractions.Configuration;
using SCIRE.Foundation.Abstractions.Configuration.Binding;

namespace SCIRE.Foundation.Runtime.Configuration.Binding;

/// <summary>Exposes a typed monitor to editors that do not know its configuration type.</summary>
/// <remarks>The caller retains ownership of the monitor.</remarks>
public sealed class ConfigurationEditorSource<T> : IConfigurationEditorSource where T : class, IPersistableConfig
{
    private readonly IPersistingOptionsMonitor<T> _monitor;

    public ConfigurationEditorSource(IPersistingOptionsMonitor<T> monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        this._monitor = monitor;
    }

    public Type ConfigurationType => typeof(T);

    public async Task<IPersistableConfig> LoadAsync(CancellationToken cancellationToken = default) => await this._monitor.LoadAsync(cancellationToken).ConfigureAwait(false);

    public Task SaveAsync(IPersistableConfig configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration is not T typed) throw new ArgumentException($"Expected configuration type '{typeof(T)}'.", nameof(configuration));

        return this._monitor.SaveAsync(typed, cancellationToken);
    }

    public Task ReloadAsync(CancellationToken cancellationToken = default) => this._monitor.ReloadAsync(cancellationToken);
}