using SCIRE.Foundation.Abstractions.Config;
using SCIRE.Foundation.Runtime.Configuration.Json;
using SCIRE.Foundation.Runtime.Tests.Configuration.Support;
using Xunit;

namespace SCIRE.Foundation.Runtime.Tests.Configuration;

public sealed class JsonConfigurationPersistenceTests
{
    [Fact]
    public async Task PersistBasicConfiguration()
    {
        var directory = Path.Combine(Path.GetTempPath(), "scire-config-" + Guid.NewGuid().ToString("N"));

        try
        {
            var cfgStore = new JsonConfigurationStore<TestConfiguration>(directory);


            // Loading missing storage returns independent defaults without writing.
            var defaults = await cfgStore.LoadAsync("dres", () => TestConfiguration.Default);
            defaults.Endpoint = "https://example.com/";

            var c1 = await cfgStore.WriteAsync(defaults);

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