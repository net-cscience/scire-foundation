using SCIRE.Foundation.Abstractions.Config;

namespace SCIRE.Foundation.Runtime.Tests.Configuration.Support;

public sealed class TestConfiguration : IPersistableConfig
{
    public required string Endpoint { get; set; }
    public required RequestConfiguration Request { get; set; }

    public DisplaySettings Display { get; set; } = new();


    public static  TestConfiguration Default => new TestConfiguration
    {
        Endpoint = "https://example.invalid/",
        Request = new RequestConfiguration() { TimeoutSeconds = 30 },
        Display = DisplaySettings.Default
    };
}