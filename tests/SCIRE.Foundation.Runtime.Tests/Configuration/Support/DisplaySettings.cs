using System.ComponentModel.DataAnnotations;

namespace SCIRE.Foundation.Runtime.Tests.Configuration.Support;


public sealed class DisplaySettings
{
    /// <summary>Gets or sets the selected theme.</summary>
    [Required(ErrorMessage = "A theme is required.")]
    public string Theme { get; set; } = "dark";

    /// <summary>Gets or sets the number of results displayed per page.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Results per page must be positive.")]
    public int ResultsPerPage { get; set; } = 24;


    public static DisplaySettings Default => new DisplaySettings
    {
        Theme = "dark",
        ResultsPerPage = 24
    };
}