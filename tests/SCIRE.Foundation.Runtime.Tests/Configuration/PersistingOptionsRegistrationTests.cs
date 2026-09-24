using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SCIRE.Foundation.Abstractions.Config;
using SCIRE.Foundation.Runtime.Configuration;
using SCIRE.Foundation.Runtime.Configuration.Json;
using SCIRE.Foundation.Runtime.Tests.Configuration.Support;
using Xunit;

namespace SCIRE.Foundation.Runtime.Tests.Configuration;

public sealed class PersistingOptionsRegistrationTests
{
    [Fact]
    public async Task BothInterfaces_UseTheSameMonitor_AndTheCallerRetainsOwnership()
    {
        var directory = Path.Combine(Path.GetTempPath(), "scire-registration-" + Guid.NewGuid().ToString("N"));

        try
        {
            var store = new JsonConfigurationStore<TestConfiguration>(directory);

            using var monitor =
                await PersistingOptionsMonitor<TestConfiguration>.CreateAsync(store, "display",
                    () => TestConfiguration.Default);


            var services = new ServiceCollection();

            // Our registration can coexist with the standard options services.
            services.AddOptions();
            services.AddPersistingOptionsMonitor(monitor);

            using (var provider = services.BuildServiceProvider())
            {
                var reader = provider.GetRequiredService<IOptionsMonitor<TestConfiguration>>();
                var editor = provider.GetRequiredService<IPersistingOptionsMonitor<TestConfiguration>>();

                Assert.Same(monitor, reader);
                Assert.Same(monitor, editor);

                using var scope = provider.CreateScope();

                Assert.Same(monitor, scope.ServiceProvider.GetRequiredService<IOptionsMonitor<TestConfiguration>>());
                Assert.Same(monitor, scope.ServiceProvider.GetRequiredService<IPersistingOptionsMonitor<TestConfiguration>>());

                var notifications = new List<int>();

                using var subscription = reader.OnChange((value, _) =>
                {
                    notifications.Add(value.Display.ResultsPerPage);
                });

                var draft = await editor.LoadAsync();
                draft.Display.ResultsPerPage = 48;

                await editor.SaveAsync(draft);

                Assert.Equal(48, reader.CurrentValue.Display.ResultsPerPage);
                Assert.Equal(new[] { 48 }, notifications);
            }

            // Disposing the provider must not dispose the externally owned monitor.
            Assert.Equal(48, monitor.CurrentValue.Display.ResultsPerPage);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DuplicateRegistration_IsRejected_AndPreservesTheOriginalMonitor()
    {
        var directory = Path.Combine(Path.GetTempPath(), "scire-registration-" + Guid.NewGuid().ToString("N"));

        try
        {
            var store = new JsonConfigurationStore<TestConfiguration>(directory);

            using var first = await PersistingOptionsMonitor<TestConfiguration>.CreateAsync(store, "first", () => TestConfiguration.Default);
            using var second = await PersistingOptionsMonitor<TestConfiguration>.CreateAsync(store, "second", () => TestConfiguration.Default);

            var services = new ServiceCollection();
            services.AddPersistingOptionsMonitor(first);

            Assert.Throws<InvalidOperationException>(() => services.AddPersistingOptionsMonitor(second));

            using var provider = services.BuildServiceProvider();

            Assert.Same(first, provider.GetRequiredService<IOptionsMonitor<TestConfiguration>>());
            Assert.Same(first, provider.GetRequiredService<IPersistingOptionsMonitor<TestConfiguration>>());
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}