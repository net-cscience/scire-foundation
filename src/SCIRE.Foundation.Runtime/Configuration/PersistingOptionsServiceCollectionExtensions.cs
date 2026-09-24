using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SCIRE.Foundation.Abstractions.Config;

namespace SCIRE.Foundation.Runtime.Configuration;

/// <summary>Registers initialized persisting options monitors.</summary>
public static class PersistingOptionsServiceCollectionExtensions
{
    /// <summary>Registers the same monitor for reading and persistence.</summary>
    /// <remarks>The caller owns the monitor and must dispose it after the service provider and its consumers have stopped.</remarks>
    public static IServiceCollection AddPersistingOptionsMonitor<T>(this IServiceCollection services, IPersistingOptionsMonitor<T> monitor) where T : class, IPersistableConfig
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(monitor);

        var alreadyRegistered = services.Any(descriptor => !descriptor.IsKeyedService && (descriptor.ServiceType == typeof(IPersistingOptionsMonitor<T>) || descriptor.ServiceType == typeof(IOptionsMonitor<T>)));

        if (alreadyRegistered) throw new InvalidOperationException($"An options monitor for '{typeof(T).FullName}' is already registered.");

        services.AddSingleton<IPersistingOptionsMonitor<T>>(monitor);
        services.AddSingleton<IOptionsMonitor<T>>(monitor);

        return services;
    }
}