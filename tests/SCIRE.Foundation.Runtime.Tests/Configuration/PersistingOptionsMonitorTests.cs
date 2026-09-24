using Microsoft.Extensions.Options;
using SCIRE.Foundation.Abstractions.Config;
using SCIRE.Foundation.Runtime.Configuration;
using SCIRE.Foundation.Runtime.Configuration.Json;
using SCIRE.Foundation.Runtime.Tests.Configuration.Support;
using Xunit;

namespace SCIRE.Foundation.Runtime.Tests.Configuration;

public sealed class PersistingOptionsMonitorTests
{
    [Fact]
    public async Task Save_PublishesChanges_RejectsStaleCopies_AndAllowsRepeatedSaves()
    {
        var directory = Path.Combine(Path.GetTempPath(), "scire-monitor-" + Guid.NewGuid().ToString("N"));

        try
        {
            var store = new JsonConfigurationStore<TestConfiguration>(directory);
            var validator = new ValidateOptions<TestConfiguration>(null, value => value.Display.ResultsPerPage > 0, "ResultsPerPage must be positive.");

            using var monitor = await PersistingOptionsMonitor<TestConfiguration>.CreateAsync(store, "display", () => TestConfiguration.Default, [validator]);

            IOptionsMonitor<TestConfiguration> options = monitor;
            var notifications = new List<int>();

            using var subscription = options.OnChange((value, name) =>
            {
                Assert.Equal(Options.DefaultName, name);
                notifications.Add(value.Display.ResultsPerPage);
            });

            // Initialization uses defaults without creating a document.
            Assert.Equal(24, options.CurrentValue.Display.ResultsPerPage);
            Assert.False(Directory.Exists(directory));

            // Two loads produce independent drafts with the same initial baseline.
            var first = await monitor.LoadAsync();
            var second = await monitor.LoadAsync();
            var initialReader = options.CurrentValue;

            Assert.NotSame(first, second);
            Assert.NotSame(first.Display, second.Display);

            // Editing a draft does not change readers or another draft.
            first.Display.ResultsPerPage = 48;

            Assert.Equal(24, options.CurrentValue.Display.ResultsPerPage);
            Assert.Equal(24, second.Display.ResultsPerPage);
            Assert.Empty(notifications);

            // Saving publishes the accepted value and sends one notification.
            await monitor.SaveAsync(first);

            Assert.Equal(48, options.CurrentValue.Display.ResultsPerPage);
            Assert.Equal(24, initialReader.Display.ResultsPerPage);
            Assert.Equal(new[] { 48 }, notifications);

            // Mutating a reader copy cannot change the accepted value.
            var readerCopy = options.CurrentValue;
            readerCopy.Display.ResultsPerPage = 999;

            Assert.Equal(48, options.CurrentValue.Display.ResultsPerPage);

            // The second draft still carries the original baseline.
            second.Display.ResultsPerPage = 96;

            var conflict = await Assert.ThrowsAsync<ConfigurationConflictException>(() => monitor.SaveAsync(second));

            Assert.Equal("display", conflict.ConfigurationId);
            Assert.Equal(48, options.CurrentValue.Display.ResultsPerPage);
            Assert.Equal(new[] { 48 }, notifications);

            var persisted = await store.LoadAsync("display", () =>TestConfiguration.Default);
            Assert.Equal(48, persisted.Display.ResultsPerPage);

            // A successful save refreshed the first instance's private baseline.
            first.Display.ResultsPerPage = 72;
            await monitor.SaveAsync(first);

            Assert.Equal(72, options.CurrentValue.Display.ResultsPerPage);
            Assert.Equal(new[] { 48, 72 }, notifications);

            // Validation rejects the value before storage or publication changes.
            first.Display.ResultsPerPage = 0;

            await Assert.ThrowsAsync<OptionsValidationException>(() => monitor.SaveAsync(first));

            Assert.Equal(72, options.CurrentValue.Display.ResultsPerPage);
            Assert.Equal(new[] { 48, 72 }, notifications);

            persisted = await store.LoadAsync("display", () => TestConfiguration.Default);
            Assert.Equal(72, persisted.Display.ResultsPerPage);

            // The rejected instance can be corrected and saved.
            first.Display.ResultsPerPage = 120;
            await monitor.SaveAsync(first);

            Assert.Equal(120, options.CurrentValue.Display.ResultsPerPage);
            Assert.Equal(new[] { 48, 72, 120 }, notifications);

            persisted = await store.LoadAsync("display", () => TestConfiguration.Default);
            Assert.Equal(120, persisted.Display.ResultsPerPage);
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