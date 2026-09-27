using System.ComponentModel.DataAnnotations;

namespace SCIRE.Foundation.Abstractions.Configuration.Binding;

/// <summary>Provides metadata and access to one property of a bound configuration.</summary>
public interface IConfigurationField
{
    /// <summary>Gets the CLR property name.</summary>
    string PropertyName { get; }

    /// <summary>Gets the display name.</summary>
    string Name { get; }

    string? Description { get; }

    string? Group { get; }

    int Order { get; }

    /// <summary>Gets the declared property type, including Nullable&lt;T&gt;.</summary>
    Type ValueType { get; }

    /// <summary>Gets whether assignment through this binding is prohibited.</summary>
    bool IsReadOnly { get; }

    /// <summary>Gets whether the declared property accepts null.</summary>
    bool AllowsNull { get; }

    /// <summary>Gets the requested editor hint, or null for automatic selection.</summary>
    string? EditorHint { get; }

    /// <summary>Gets validation annotations available to the editor.</summary>
    IReadOnlyList<ValidationAttribute> ValidationAttributes { get; }

    /// <summary>Reads the current property value from the bound instance.</summary>
    object? GetValue();

    /// <summary>Assigns a value to the bound instance without persisting it.</summary>
    void SetValue(object? value);
}