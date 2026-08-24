using Microsoft.Extensions.Configuration;

namespace SCIRE.Foundation.Runtime.Tests;

public sealed class LectureCatalogServiceTests
{
    [Fact]
    public void TestConfig()
    {
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        // Build a config object, using env vars and JSON providers.
        var config = new ConfigurationBuilder()
            .AddJsonFile(
                @"D:\myRepo\net.cscience\scire-foundation\tests\SCIRE.Foundation.Runtime.Tests\appsettings.json")
            .AddEnvironmentVariables()
            .Build();

        // Get values from the config given their key and their target type.
        Settings settings = new Settings();

        config.GetSection("Settings").Bind(settings);

        // Write the values to the console.
        Console.WriteLine($"KeyOne = {settings?.KeyOne}");
        Console.WriteLine($"KeyTwo = {settings?.KeyTwo}");
        Console.WriteLine($"KeyThree:Message = {settings?.KeyThree?.Message}");

        settings?.KeyOne = 10;
        Console.WriteLine($"KeyOne = {settings?.KeyOne}");
        config.Reload();
        Console.WriteLine($"KeyOne = {settings?.KeyOne}");

        // Application code which might rely on the config could start here.

        // This will output the following:
        //   KeyOne = 1
        //   KeyTwo = True
        //   KeyThree:Message = Oh, that's nice...
    }
}