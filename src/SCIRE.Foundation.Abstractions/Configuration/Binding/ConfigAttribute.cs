namespace SCIRE.Foundation.Abstractions.Configuration.Binding;

/// <summary>Exposes a configuration property to generated editors.</summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ConfigAttribute : Attribute
{
    /// <summary>Gets or sets the display name. Defaults to the property name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the description shown by an editor.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the logical group.</summary>
    public string? Group { get; set; }

    /// <summary>Gets or sets the order within the group.</summary>
    public int Order { get; set; }

    /// <summary>Gets or sets whether the property is exposed.</summary>
    public bool Show { get; set; } = true;

    /// <summary>Gets or sets whether editors must prevent changes.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>Gets or sets a semantic editor hint. Null selects by property type.</summary>
    public string? Editor { get; set; }
}