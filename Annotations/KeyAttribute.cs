using System;
using System.Globalization;

namespace NeoIni.Annotations;

/// <summary>
/// Specifies the INI section and key that a property is mapped to.
/// Use this attribute on configuration model properties
/// to bind them to specific INI entries.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class NeoIniKeyAttribute : Attribute
{
    /// <summary>INI section name.</summary>
    public string Section { get; }

    /// <summary>INI key name inside the section.</summary>
    public string Key { get; }

    /// <summary>
    /// Optional default value (as string) used when the key is missing.
    /// If null, NeoIniReader's default handling is used.
    /// </summary>
    public string? DefaultValue { get; }

    /// <summary>Initializes a new instance of the <see cref="NeoIniKeyAttribute"/> class.</summary>
    /// <param name="section">The INI section name.</param>
    /// <param name="key">The INI key name inside the section.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="section"/> or <paramref name="key"/> is <see langword="null"/>.
    /// </exception>
    public NeoIniKeyAttribute(string section, string key, object? defaultValue = null)
    {
        Section = section ?? throw new ArgumentNullException(nameof(section));
        Key = key ?? throw new ArgumentNullException(nameof(key));
        if (defaultValue is IFormattable formattable) DefaultValue = formattable.ToString(null, CultureInfo.InvariantCulture);
        else DefaultValue = defaultValue?.ToString();
    }
}
