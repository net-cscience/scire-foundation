using System.Text.Json;

public sealed record Snapshot<T>(JsonElement Value)
{
    public T Copy() => this.Value.Deserialize<T>() ?? throw new JsonException("Configuration cannot be null.");
}