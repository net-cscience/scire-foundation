using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SCIRE.Foundation.Abstractions.Config;
using SCIRE.Foundation.Runtime.Configuration.Persistence;

namespace SCIRE.Foundation.Runtime.Configuration.Json;

/// <summary>Stores typed configuration as JSON with automatic conflict detection.</summary>
/// <remarks>
/// Cooperating writers on local Windows/Linux filesystems share a sidecar lock.
/// Lock files remain in place; closing the file handle releases the lock.
/// </remarks>
public sealed partial class JsonConfigurationStore<T> : ConfigurationStoreBase<T> where T : class, IPersistableConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    private readonly string _directory;

    /// <summary>Creates a store without creating its directory or writing configuration.</summary>
    public JsonConfigurationStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        this._directory = Path.GetFullPath(directory);
    }

    /// <summary>Gets the document path for a lowercase configuration identity.</summary>
    public string GetFilePath(string configurationId)
    {
        ArgumentNullException.ThrowIfNull(configurationId);

        if (!IdentityPattern().IsMatch(configurationId)) throw new ArgumentException("Use 1–128 lowercase ASCII letters, digits, dots, underscores or hyphens, starting with a letter or digit.", nameof(configurationId));

        return Path.Combine(this._directory, $"cfg-{configurationId}.json");
    }

    /// <inheritdoc />
    protected override byte[] Serialize(T value) => JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

    /// <inheritdoc />
    protected override T Deserialize(ReadOnlySpan<byte> content) => JsonSerializer.Deserialize<T>(content, JsonOptions) ?? throw new JsonException("Stored configuration must be a non-null JSON value.");

    /// <inheritdoc />
    protected override async Task<byte[]?> ReadContentAsync(string configurationId, CancellationToken cancellationToken)
    {
        var path = this.GetFilePath(configurationId);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 4096, FileOptions.Asynchronous);
            using var buffer = new MemoryStream();

            await input.CopyToAsync(buffer, cancellationToken);

            return buffer.ToArray();
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    protected override async Task WriteConditionallyAsync(string configurationId, ReadOnlyMemory<byte> content, string? originalHash, CancellationToken cancellationToken)
    {
        var path = this.GetFilePath(configurationId);
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(this._directory);

        await using var fileLock = await this.AcquireFileLockAsync(path + ".lock", cancellationToken);

        // The lock covers the fresh read, comparison, and write.
        var current = await this.ReadContentAsync(configurationId, cancellationToken);
        EnsureUnchanged(configurationId, originalHash, current);

        // Check the original hash even when the candidate equals current content.
        if (current is not null && current.AsSpan().SequenceEqual(content.Span)) return;

        string? temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
            {
                await output.WriteAsync(content, cancellationToken);
                await output.FlushAsync(cancellationToken);
                output.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();

            File.Move(temporaryPath, path, overwrite: true);
            temporaryPath = null;

            // Commit succeeded. Do not report subsequent cancellation as a failed save.
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                    // Cleanup must not hide the original write failure.
                }
                catch (UnauthorizedAccessException)
                {
                    // Cleanup must not hide the original write failure.
                }
            }
        }
    }

    private async Task<FileStream> AcquireFileLockAsync(string path, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException exception) when (IsLockContention(exception))
            {
                await Task.Delay(25, cancellationToken);
            }
        }
    }

    private static bool IsLockContention(IOException exception) => OperatingSystem.IsWindows() ? (exception.HResult & 0xFFFF) is 32 or 33 : OperatingSystem.IsLinux() && exception.HResult == 11;

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]{0,127}\\z", RegexOptions.CultureInvariant)]
    private static partial Regex IdentityPattern();
}