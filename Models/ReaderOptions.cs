using System;

namespace NeoIni.Models;

/// <summary>
/// Represents configuration options that are applied when constructing a <see cref="NeoIniReader"/> instance.
/// </summary>
/// <remarks>
/// These options control how the reader will handle automatic saving, backup creation, missing keys,
/// checksum validation and saving on dispose. They are read and applied during <see cref="NeoIniReader"/>
/// initialization and do not affect instances that have already been created.
/// </remarks>
public sealed class NeoIniReaderOptions
{
    /// <summary>
    /// Determines whether changes are automatically written to the disk after every modification.
	/// Default is <c>true</c>.
    /// </summary>
    public bool UseAutoSave { get; set; } = true;

    /// <summary>
    /// Interval (in operations) between automatic saves when <see cref="UseAutoSave"/> is enabled.
    /// Default value is 0.
    /// </summary>
    public int AutoSaveInterval { get; set; } = 0;

    /// <summary>
    /// Determines whether backup files (.backup) are created during save operations.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool UseAutoBackup { get; set; } = true;

    /// <summary>
    /// Determines whether missing keys are automatically added to the file with a default value when requested via <see cref="NeoIniReader.GetValue{T}"/>. 
	/// Default is <c>true</c>.
    /// </summary>
    public bool UseAutoAdd { get; set; } = true;

    /// <summary>
    /// Determines whether a checksum is calculated and verified during file load and save operations to ensure data integrity.
    /// When enabled, the configuration file includes a checksum that detects corruption or tampering.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool UseChecksum { get; set; } = true;

    /// <summary>
    /// Determines whether the configuration is automatically saved when the instance is disposed.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool SaveOnDispose { get; set; } = true;

    /// <summary>
    /// Determines whether empty strings or null values are permitted for configuration keys.
    /// Default value is <c>true</c>.
    /// </summary>
    public bool AllowEmptyValues { get; set; } = true;

    /// <summary>
    /// Default behavior: automatic saving and backups enabled, checksum validation on,
    /// missing keys are added automatically and configuration is saved on dispose.
    /// </summary>
    public static NeoIniReaderOptions Default => new();

    /// <summary>
    /// Safe behavior: keeps the file structure strict by not adding missing keys automatically.
    /// Other options are the same as <see cref="Default"/>.
    /// </summary>
    public static NeoIniReaderOptions Safe => new() { UseAutoAdd = false };

    /// <summary>
    /// High-performance behavior: disables automatic saving, backups, checksum validation,
    /// automatic key creation and saving on dispose. The caller is responsible for
    /// explicitly saving changes when appropriate.
    /// </summary>
    public static NeoIniReaderOptions Performance => new()
    {
        UseAutoSave = false,
        UseAutoBackup = false,
        UseAutoAdd = false,
        UseChecksum = false,
        SaveOnDispose = false
    };

    /// <summary>
    /// Buffered automatic saving: behaves like <see cref="Default"/>, but saves changes
    /// every specified number of operations instead of after every modification.
    /// </summary>
    /// <param name="interval">Number of operations between automatic saves; must be greater than zero.</param>
    public static NeoIniReaderOptions BufferedAutoSave(int interval)
    {
        if (interval <= 0) throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");
        return new NeoIniReaderOptions { AutoSaveInterval = interval };
    }

    /// <summary>
    /// Read-only behavior: never writes to disk and does not create missing keys,
    /// but still verifies checksums when loading to detect corruption.
    /// </summary>
    public static NeoIniReaderOptions ReadOnly => new()
    {
        UseAutoSave = false,
        UseAutoBackup = false,
        UseAutoAdd = false,
        SaveOnDispose = false
    };
}
