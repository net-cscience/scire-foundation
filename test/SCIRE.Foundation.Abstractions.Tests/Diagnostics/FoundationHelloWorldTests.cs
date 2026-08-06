using SCIRE.Foundation.Abstractions.Diagnostics;

namespace SCIRE.Foundation.Abstractions.Tests.Diagnostics;

public sealed class FoundationHelloWorldTests
{
    [Fact]
    public void GetMessageReturnsPackageMessage()
    {
        var message = FoundationHelloWorld.GetMessage();

        Assert.Equal("Hello from SCIRE.Foundation.Abstractions", message);
    }
}