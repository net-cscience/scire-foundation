using SCIRE.Foundation.Abstractions.Config;
using SCIRE.Foundation.Runtime.Configuration.Json;
using Xunit;

namespace SCIRE.Foundation.Runtime.Tests.Configuration;

public sealed class JsonConfigurationStoreTests
{
    [Fact]
    public async Task TwoLoadedCopies_SecondSaveConflicts()
    {
        var directory = Path.Combine(Path.GetTempPath(), "scire-config-" + Guid.NewGuid().ToString("N"));

        try
        {
            var firstStore = new JsonConfigurationStore<TestConfiguration>(directory);
            var secondStore = new JsonConfigurationStore<TestConfiguration>(directory);

            // Loading missing storage returns independent defaults without writing.
            var defaults = new TestConfiguration();
            var original = await firstStore.LoadAsync("dres", () => defaults);

            Assert.NotSame(defaults, original);
            Assert.NotSame(defaults.Request, original.Request);
            Assert.False(Directory.Exists(directory));

            // An explicit save creates the initial document.
            var committed = await firstStore.WriteAsync(original);

            Assert.NotSame(original, committed);
            Assert.NotSame(original.Request, committed.Request);
            Assert.True(File.Exists(firstStore.GetFilePath("dres")));

            // Both instances are loaded from the same stored state.
            var c1 = await firstStore.LoadAsync("dres", () => new TestConfiguration());
            var c2 = await secondStore.LoadAsync("dres", () => new TestConfiguration());

            Assert.NotSame(c1, c2);
            Assert.NotSame(c1.Request, c2.Request);

            // Editing c1 leaves c2 unchanged.
            c1.Request.TimeoutSeconds = 60;
            Assert.Equal(30, c2.Request.TimeoutSeconds);

            await firstStore.WriteAsync(c1);

            // c2 still has the original private baseline.
            c2.Request.TimeoutSeconds = 90;

            var conflict = await Assert.ThrowsAsync<ConfigurationConflictException>(() => secondStore.WriteAsync(c2));

            Assert.Equal("dres", conflict.ConfigurationId);

            // A fresh store confirms that the rejected save changed nothing.
            var freshStore = new JsonConfigurationStore<TestConfiguration>(directory);
            var persisted = await freshStore.LoadAsync("dres", () => new TestConfiguration());

            Assert.Equal(60, persisted.Request.TimeoutSeconds);

            // Saving c1 again works because its baseline was refreshed automatically.
            c1.Request.TimeoutSeconds = 120;
            await firstStore.WriteAsync(c1);

            var savedAgain = await freshStore.LoadAsync("dres", () => new TestConfiguration());

            Assert.Equal(120, savedAgain.Request.TimeoutSeconds);
            Assert.Equal(60, persisted.Request.TimeoutSeconds);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    public sealed class TestConfiguration : IPersistableConfig
    {
        public string Endpoint { get; set; } = "https://example.invalid/";
        public RequestConfiguration Request { get; set; } = new();
    }

    public sealed class RequestConfiguration
    {
        public int TimeoutSeconds { get; set; } = 30;
    }
}