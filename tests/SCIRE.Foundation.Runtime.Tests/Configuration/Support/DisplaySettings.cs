namespace SCIRE.Foundation.Runtime.Tests.Configuration.Support;


public sealed class DisplaySettings
{
    public string Theme { get; set; } = "dark";
    public int ResultsPerPage { get; set; } = 24;


    public static DisplaySettings Default => new DisplaySettings
    {
        Theme = "dark",
        ResultsPerPage = 24
    };
}