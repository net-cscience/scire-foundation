namespace SCIRE.Foundation.Abstractions.Config;

/// <summary>Reports a publication or notification failure after an accepted operation.</summary>
public sealed class ConfigurationPublicationException : Exception
{
    /// <summary>Gets the affected configuration identity.</summary>
    public string ConfigurationId { get; }

    /// <summary>Gets whether this operation's storage write was accepted.</summary>
    public bool WasPersisted { get; }

    /// <summary>Creates an error that must not be interpreted as a storage rollback.</summary>
    public ConfigurationPublicationException(string configurationId, bool wasPersisted, Exception innerException) : base($"Configuration '{configurationId}' was accepted, but publication or notification failed. The accepted state was not rolled back.", innerException)
    {
        this.ConfigurationId = configurationId;
        this.WasPersisted = wasPersisted;
    }
}