using Microsoft.Extensions.Options;
using SCIRE.Foundation.Runtime.Configuration;
using SCIRE.Foundation.Runtime.Configuration.Json;
using SCIRE.Foundation.Runtime.Tests.Configuration.Support;
using Xunit;

namespace SCIRE.Foundation.Runtime.Tests.Configuration;

public sealed class PersistingOptionsValidationTests
{
    [Fact]
    public async Task Validation_RejectsInvalidSaves_AndPreservesTheActiveValueOnInvalidReload()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"scire-validation-{Guid.NewGuid():N}");

        try
        {
            var store = new JsonConfigurationStore<TestConfiguration>(directory);
            var validator = new DataAnnotationValidateOptions<TestConfiguration>(Options.DefaultName);
            using var monitor = await PersistingOptionsMonitor<TestConfiguration>.CreateAsync(store, "display", () => TestConfiguration.Default, [validator]);

            var initialResultsPerPage = monitor.CurrentValue.Display.ResultsPerPage;
            var changes = new List<int>();
            using var subscription = monitor.OnChange((value, _) => changes.Add(value.Display.ResultsPerPage));

            // Nested validation rejects the edit before storage is written.
            var draft = await monitor.LoadAsync();
            draft.Display.ResultsPerPage = 0;

            var saveFailure = await Assert.ThrowsAsync<OptionsValidationException>(() => monitor.SaveAsync(draft));

            Assert.Contains(saveFailure.Failures, failure => failure.Contains("Results per page must be positive.", StringComparison.Ordinal));
            Assert.Equal(initialResultsPerPage, monitor.CurrentValue.Display.ResultsPerPage);
            Assert.False(Directory.Exists(directory));
            Assert.Empty(changes);

            // The same draft can be corrected and saved.
            draft.Display.ResultsPerPage = 48;
            await monitor.SaveAsync(draft);

            Assert.Equal(48, monitor.CurrentValue.Display.ResultsPerPage);
            Assert.Equal(48, Assert.Single(changes));

            var external = await store.LoadAsync("display", () => TestConfiguration.Default);
            Assert.Equal(48, external.Display.ResultsPerPage);

            // A direct store write simulates invalid data arriving externally.
            external.Display = null!;
            await store.WriteAsync(external);

            var reloadFailure = await Assert.ThrowsAsync<OptionsValidationException>(() => monitor.ReloadAsync());

            Assert.Contains(reloadFailure.Failures, failure => failure.Contains("Display settings are required.", StringComparison.Ordinal));
            Assert.Equal(48, monitor.CurrentValue.Display.ResultsPerPage);
            Assert.Equal(48, Assert.Single(changes));

            // Reload retains the active value without rewriting invalid storage.
            var invalidStored = await store.LoadAsync("display", () => TestConfiguration.Default);
            Assert.Null(invalidStored.Display);

            await Assert.ThrowsAsync<OptionsValidationException>(() => monitor.LoadAsync());

            // Correct the stored configuration, then reload successfully.
            external.Display = monitor.CurrentValue.Display;
            external.Display.ResultsPerPage = 72;
            await store.WriteAsync(external);

            await monitor.ReloadAsync();

            Assert.Equal(72, monitor.CurrentValue.Display.ResultsPerPage);
            Assert.Equal(new[] { 48, 72 }, changes);
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