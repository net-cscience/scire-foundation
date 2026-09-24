using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SCIRE.Foundation.Abstractions.Config;
using SCIRE.Foundation.Abstractions.Config.Persistence;

namespace SCIRE.Foundation.Runtime.Configuration;

/// <summary>Validates, saves, and publishes one configuration through the default options name.</summary>
public sealed class PersistingOptionsMonitor<T> : IPersistingOptionsMonitor<T>, IDisposable where T : class, IPersistableConfig
{
    private readonly IConfigurationStore<T> _store;
    private readonly string _configurationId;
    private readonly Snapshot<T> _defaults;
    private readonly IValidateOptions<T>[] _validators;
    private readonly ConditionalWeakTable<T, object> _drafts = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _listenersGate = new();

    private Action<T, string?>? _listeners;
    private volatile Snapshot<T> _current;
    private volatile bool _disposed;

    private PersistingOptionsMonitor(IConfigurationStore<T> store, string configurationId, T defaults, IEnumerable<IValidateOptions<T>>? validators)
    {
        this._store = store;
        this._configurationId = configurationId;
        this._defaults = new Snapshot<T>(JsonSerializer.SerializeToElement(defaults));
        this._validators = validators?.ToArray() ?? [];
        this._current = this._defaults;
    }

    /// <summary>Loads and validates the configuration before returning the monitor.</summary>
    public static async Task<PersistingOptionsMonitor<T>> CreateAsync(IConfigurationStore<T> store, string configurationId, Func<T> createDefaults, IEnumerable<IValidateOptions<T>>? validators = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationId);
        ArgumentNullException.ThrowIfNull(createDefaults);
        cancellationToken.ThrowIfCancellationRequested();

        var defaults = createDefaults();
        ArgumentNullException.ThrowIfNull(defaults);

        var monitor = new PersistingOptionsMonitor<T>(store, configurationId, defaults, validators);

        try
        {
            await monitor.ReloadAsync(cancellationToken);
            return monitor;
        }
        catch
        {
            monitor.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public T CurrentValue => this.Get(Options.DefaultName);

    /// <inheritdoc />
    public T Get(string? name)
    {
        this.ThrowIfDisposed();
        if (!string.IsNullOrEmpty(name)) throw new NotSupportedException("This monitor supports only the default options name.");

        return this._current.Copy();
    }

    /// <inheritdoc />
    public async Task<T> LoadAsync(CancellationToken cancellationToken = default)
    {
        this.ThrowIfDisposed();

        var value = await this._store.LoadAsync(this._configurationId, this._defaults.Copy, cancellationToken);
        this.Prepare(value);
        cancellationToken.ThrowIfCancellationRequested();

        this._drafts.Add(value, new object());
        return value;
    }

    /// <inheritdoc />
    public async Task SaveAsync(T value, CancellationToken cancellationToken = default)
    {
        this.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(value);
        if (!this._drafts.TryGetValue(value, out _)) throw new InvalidOperationException("Load the configuration through this monitor before saving it.");

        cancellationToken.ThrowIfCancellationRequested();
        this.Prepare(value);

        Snapshot<T>? changed;
        await this._gate.WaitAsync(cancellationToken);

        try
        {
            this.ThrowIfDisposed();

            // Save the original instance so the store refreshes its private baseline.
            var committed = await this._store.WriteAsync(value, cancellationToken);

            try
            {
                changed = this.Accept(this.Prepare(committed));
            }
            catch (Exception exception)
            {
                throw new ConfigurationPublicationException(this._configurationId, wasPersisted: true, exception);
            }
        }
        finally
        {
            this._gate.Release();
        }

        this.Notify(changed, wasPersisted: true);
    }

    /// <inheritdoc />
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        this.ThrowIfDisposed();

        Snapshot<T>? changed;
        await this._gate.WaitAsync(cancellationToken);

        try
        {
            this.ThrowIfDisposed();

            var value = await this._store.LoadAsync(this._configurationId, this._defaults.Copy, cancellationToken);
            var next = this.Prepare(value);
            cancellationToken.ThrowIfCancellationRequested();

            changed = this.Accept(next);
        }
        finally
        {
            this._gate.Release();
        }

        this.Notify(changed, wasPersisted: false);
    }

    /// <inheritdoc />
    public IDisposable OnChange(Action<T, string?> listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        lock (this._listenersGate)
        {
            this.ThrowIfDisposed();
            this._listeners += listener;
        }

        return new Subscription(() =>
        {
            lock (this._listenersGate)
            {
                this._listeners -= listener;
            }
        });
    }

    private Snapshot<T> Prepare(T value)
    {
        var snapshot = new Snapshot<T>(JsonSerializer.SerializeToElement(value));
        var failures = new List<string>();

        foreach (var validator in this._validators)
        {
            var result = validator.Validate(Options.DefaultName, snapshot.Copy());

            if (result.Failed)
            {
                failures.AddRange(result.Failures);
            }
        }

        if (failures.Count != 0) throw new OptionsValidationException(Options.DefaultName, typeof(T), failures);

        return snapshot;
    }

    private Snapshot<T>? Accept(Snapshot<T> next)
    {
        if (JsonElement.DeepEquals(this._current.Value, next.Value)) return null;

        this._current = next;
        return next;
    }

    private void Notify(Snapshot<T>? changed, bool wasPersisted)
    {
        if (changed is null) return;

        Action<T, string?>? listeners;

        lock (this._listenersGate)
        {
            listeners = this._listeners;
        }

        if (listeners is null) return;

        var failures = new List<Exception>();

        foreach (Action<T, string?> listener in listeners.GetInvocationList())
        {
            try
            {
                listener(changed.Copy(), Options.DefaultName);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        if (failures.Count != 0) throw new ConfigurationPublicationException(this._configurationId, wasPersisted, new AggregateException(failures));
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(this._disposed, this);

    /// <summary>Unsubscribes listeners and releases resources. Await active operations before disposal.</summary>
    public void Dispose()
    {
        lock (this._listenersGate)
        {
            if (this._disposed) return;

            this._disposed = true;
            this._listeners = null;
        }

        this._gate.Dispose();
    }


}