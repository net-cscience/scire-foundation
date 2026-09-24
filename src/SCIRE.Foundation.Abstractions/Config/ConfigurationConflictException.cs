namespace SCIRE.Foundation.Abstractions.Config;

/// <summary>Indicates that storage no longer matches the instance's original stored state.</summary>
public sealed class ConfigurationConflictException : IOException
{
    /// <summary>Gets the affected configuration identity.</summary>
    public string ConfigurationId { get; }

    /// <summary>Creates a conflict without exposing configuration values or internal hashes.</summary>
    public ConfigurationConflictException(string configurationId) : base($"Configuration '{configurationId}' changed in storage. Load it again before saving.")
    {
        this.ConfigurationId = configurationId;
    }
}