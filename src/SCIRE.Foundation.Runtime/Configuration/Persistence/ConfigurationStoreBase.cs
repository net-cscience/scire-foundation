using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using SCIRE.Foundation.Abstractions.Config;
using SCIRE.Foundation.Abstractions.Config.Persistence;

namespace SCIRE.Foundation.Runtime.Configuration.Persistence;

/// <summary>Tracks the original stored content privately for each loaded configuration instance.</summary>
/// <remarks>Concrete stores provide serialization and an atomic check-and-write operation.</remarks>
public abstract class ConfigurationStoreBase<T> : IConfigurationStore<T> where T : class, IPersistableConfig
{
    private readonly ConditionalWeakTable<T, Tracking> _tracking = new();

    /// <inheritdoc />
    public async Task<T> LoadAsync(string configurationId, Func<T> createDefaults, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationId);
        ArgumentNullException.ThrowIfNull(createDefaults);
        cancellationToken.ThrowIfCancellationRequested();

        var content = await this.ReadContentAsync(configurationId, cancellationToken);
        var value = content is null ? this.Copy(createDefaults()) : this.Deserialize(content);
        var originalHash = content is null ? null : CalculateHash(content);

        this._tracking.Add(value, new Tracking(configurationId, originalHash));

        return value;
    }

    /// <inheritdoc />
    public T Clone(T value)
    {
        var tracking = this.GetTracking(value);
        Enter(tracking);

        try
        {
            var copy = this.Copy(value);
            this._tracking.Add(copy, new Tracking(tracking.Id, tracking.Hash));

            return copy;
        }
        finally
        {
            Volatile.Write(ref tracking.Busy, 0);
        }
    }

    /// <inheritdoc />
    public async Task<T> WriteAsync(T value, CancellationToken cancellationToken = default)
    {
        var tracking = this.GetTracking(value);
        cancellationToken.ThrowIfCancellationRequested();
        Enter(tracking);

        try
        {
            // Prepare the exact candidate and return value before acquiring a storage lock.
            var content = this.Serialize(value);
            var committed = this.Deserialize(content);
            var committedHash = CalculateHash(content);

            this._tracking.Add(committed, new Tracking(tracking.Id, committedHash));

            await this.WriteConditionallyAsync(tracking.Id, content, tracking.Hash, cancellationToken);

            // Refresh only after the backend confirms that the write succeeded.
            tracking.Hash = committedHash;

            return committed;
        }
        finally
        {
            Volatile.Write(ref tracking.Busy, 0);
        }
    }

    /// <summary>Serializes a value into an independently owned representation.</summary>
    protected abstract byte[] Serialize(T value);

    /// <summary>Deserializes content into a fresh, non-null object graph.</summary>
    protected abstract T Deserialize(ReadOnlySpan<byte> content);

    /// <summary>Reads complete stored content, or returns null when storage is absent.</summary>
    /// <remarks>This operation does not acquire the backend's write lock.</remarks>
    protected abstract Task<byte[]?> ReadContentAsync(string configurationId, CancellationToken cancellationToken);

    /// <summary>Coordinates a fresh read, original-hash comparison, and atomic write.</summary>
    /// <remarks>
    /// Acquire write coordination, read current content, call EnsureUnchanged,
    /// and write the supplied content without modification.
    /// Release coordination before returning.
    /// Do not report cancellation after a confirmed commit.
    /// </remarks>
    protected abstract Task WriteConditionallyAsync(string configurationId, ReadOnlyMemory<byte> content, string? originalHash, CancellationToken cancellationToken);

    /// <summary>Throws when freshly read storage differs from the original stored state.</summary>
    /// <remarks>The backend must hold write coordination while checking and subsequently writing.</remarks>
    protected static void EnsureUnchanged(string configurationId, string? originalHash, byte[]? currentContent)
    {
        var currentHash = currentContent is null ? null : CalculateHash(currentContent);

        if (!StringComparer.Ordinal.Equals(originalHash, currentHash)) throw new ConfigurationConflictException(configurationId);
    }

    private T Copy(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return this.Deserialize(this.Serialize(value));
    }

    private Tracking GetTracking(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return this._tracking.TryGetValue(value, out var tracking) ? tracking : throw new InvalidOperationException("Load the configuration through this store before saving or cloning it.");
    }

    private static string CalculateHash(ReadOnlySpan<byte> content) => Convert.ToHexString(SHA256.HashData(content));

    private static void Enter(Tracking tracking)
    {
        if (Interlocked.CompareExchange(ref tracking.Busy, 1, 0) != 0) throw new InvalidOperationException("This configuration instance is already being saved or copied.");
    }

}