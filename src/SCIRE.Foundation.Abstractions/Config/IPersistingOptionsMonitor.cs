using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace SCIRE.Foundation.Abstractions.Config;

/// <summary>Provides active options, isolated editing, and validated storage reload.</summary>
public interface IPersistingOptionsMonitor<T> : IOptionsMonitor<T> where T : class, IPersistableConfig
{
    /// <summary>Gets the default registration's read-only effective configuration section.</summary>
    IConfiguration Configuration { get; }

    /// <summary>Opens an independent draft from writable storage, excluding deployment overrides.</summary>
    /// <remarks>Null or empty selects the default registration.</remarks>
    Task<IConfigurationEdit<T>> BeginEditAsync(string? name = null, CancellationToken cancellationToken = default);

    /// <summary>Reloads and validates the default registration.</summary>
    Task ReloadAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets the selected registration's read-only effective configuration section.</summary>
    IConfiguration GetConfiguration(string? name);

    /// <summary>Reloads and validates the selected named registration.</summary>
    Task ReloadAsync(string name, CancellationToken cancellationToken = default);
}