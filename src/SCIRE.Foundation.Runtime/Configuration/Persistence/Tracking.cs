namespace SCIRE.Foundation.Runtime.Configuration.Persistence;

/// <summary>Tracks the state of a configuration instance.</summary>
public sealed class Tracking(string id, string? hash)
{
    public string Id { get; } = id;
    public string? Hash = hash;
    public int Busy;
}