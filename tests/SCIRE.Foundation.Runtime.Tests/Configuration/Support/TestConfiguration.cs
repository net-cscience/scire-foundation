using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using SCIRE.Foundation.Abstractions.Config;

namespace SCIRE.Foundation.Runtime.Tests.Configuration.Support;

public sealed class TestConfiguration : IPersistableConfig
{
    public required string Endpoint { get; set; }
    public required RequestConfiguration Request { get; set; }


    /// <summary>Gets or sets the display settings.</summary>
    [Required(ErrorMessage = "Display settings are required.")]
    [ValidateObjectMembers]
    public DisplaySettings Display { get; set; } = new();


    public static  TestConfiguration Default => new TestConfiguration
    {
        Endpoint = "https://example.invalid/",
        Request = new RequestConfiguration() { TimeoutSeconds = 30 },
        Display = DisplaySettings.Default
    };
}