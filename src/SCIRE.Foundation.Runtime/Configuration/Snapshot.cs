using System.Text.Json;

namespace SCIRE.Foundation.Runtime.Configuration;

public sealed record Snapshot<T>(JsonElement Value)
{
    public T Copy() => this.Value.Deserialize<T>() ?? throw new JsonException("Configuration cannot be null.");
}