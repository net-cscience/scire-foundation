
using System.Reflection;
using SCIRE.Foundation.Abstractions.Configuration;
using SCIRE.Foundation.Abstractions.Configuration.Binding;

namespace SCIRE.Foundation.Runtime.Configuration.Binding;

/// <summary>Creates field bindings for annotated properties of a configuration instance.</summary>
public static class ConfigurationFieldBinder
{
    /// <summary>Binds public, readable, non-indexed properties marked with ConfigAttribute.</summary>
    /// <remarks>The returned fields retain the supplied instance. Rebind after replacing the draft.</remarks>
    public static IReadOnlyList<IConfigurationField> Bind(IPersistableConfig configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var type = configuration.GetType();
        if (type.IsValueType) throw new ArgumentException("Configuration binding requires a reference-type instance.", nameof(configuration));

        var fields = new List<IConfigurationField>();
        var nullability = new NullabilityInfoContext();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0) continue;

            var attribute = (ConfigAttribute?)Attribute.GetCustomAttribute(property, typeof(ConfigAttribute), inherit: true);
            if (attribute is null || !attribute.Show) continue;

            var writeState = nullability.Create(property).WriteState;
            fields.Add(new ReflectionConfigurationField(configuration, property, attribute, writeState));
        }

        var ordered = fields
            .OrderBy(field => field.Group ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(field => field.Order)
            .ThenBy(field => field.PropertyName, StringComparer.Ordinal)
            .ToArray();

        return Array.AsReadOnly(ordered);
    }
}