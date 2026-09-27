using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using SCIRE.Foundation.Abstractions.Configuration.Binding;

namespace SCIRE.Foundation.Runtime.Configuration.Binding;

/// <summary>Reads and writes a property on an existing configuration instance.</summary>
internal sealed class ReflectionConfigurationField : IConfigurationField
{
    private readonly object _owner;
    private readonly PropertyInfo _property;
    private readonly Type _effectiveType;

    public string PropertyName { get; }
    public string Name { get; }
    public string? Description { get; }
    public string? Group { get; }
    public int Order { get; }
    public Type ValueType { get; }
    public bool IsReadOnly { get; }
    public bool AllowsNull { get; }
    public string? EditorHint { get; }
    public IReadOnlyList<ValidationAttribute> ValidationAttributes { get; }

    internal ReflectionConfigurationField(object owner, PropertyInfo property, ConfigAttribute attribute, NullabilityState writeState)
    {
        this._owner = owner;
        this._property = property;
        this._effectiveType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        this.PropertyName = property.Name;
        this.Name = attribute.Name ?? property.Name;
        this.Description = attribute.Description;
        this.Group = attribute.Group;
        this.Order = attribute.Order;
        this.ValueType = property.PropertyType;
        this.EditorHint = attribute.Editor;

        var setter = property.SetMethod;
        var isInitOnly = setter?.ReturnParameter.GetRequiredCustomModifiers().Any(type => type.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

        this.IsReadOnly = attribute.ReadOnly || setter is null || !setter.IsPublic || isInitOnly;
        this.AllowsNull = Nullable.GetUnderlyingType(this.ValueType) is not null || (!this.ValueType.IsValueType && writeState != NullabilityState.NotNull);

        this.ValidationAttributes = Array.AsReadOnly(Attribute.GetCustomAttributes(property, typeof(ValidationAttribute), inherit: true).Cast<ValidationAttribute>().ToArray());
    }

    public object? GetValue() => this._property.GetValue(this._owner);

    public void SetValue(object? value)
    {
        if (this.IsReadOnly) throw new InvalidOperationException($"Configuration property '{this.PropertyName}' is read-only.");

        if (value is null)
        {
            if (!this.AllowsNull) throw new ArgumentException($"Configuration property '{this.PropertyName}' does not accept null.", nameof(value));
        }
        else if (!this._effectiveType.IsInstanceOfType(value))
        {
            throw new ArgumentException($"Configuration property '{this.PropertyName}' expects '{this.ValueType}', but received '{value.GetType()}'.", nameof(value));
        }

        this._property.SetValue(this._owner, value);
    }
}