using System;
using NeoIni.Models;
using NeoIni.Providers;

namespace NeoIni
{
    public partial class NeoIniDocument
    {
        /// <summary>
        /// Determines whether changes are automatically written to the disk after every modification.
        /// Default is <c>true</c>.
        /// </summary>
        public bool UseAutoSave { get; set; }

        /// <summary>
        /// Interval (in operations) between automatic saves when <see cref="UseAutoSave"/> is enabled.
        /// Default value is 0.
        /// </summary>
        public int AutoSaveInterval
        {
            get => _AutoSaveInterval;
            set
            {
                if (value < 0) throw new ArgumentException("Interval cannot be negative.");
                _AutoSaveInterval = value;
            }
        }

        /// <summary>
        /// Determines whether backup files (.backup) are created during save operations.
        /// Default value is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// This property is only supported by the built-in file provider (<see cref="NeoIniFileProvider"/>).
        /// Setting it to <c>true</c> on a custom <see cref="INeoIniProvider"/> that does not support backups
        /// will throw an <see cref="UnsupportedProviderOperationException"/>.
        /// Getting this property on a non-file provider always returns <c>false</c>.
        /// </remarks>
        /// <exception cref="UnsupportedProviderOperationException">
        /// Thrown when attempting to enable backups on a provider that does not support them.
        /// </exception>
        public bool UseAutoBackup
        {
            get => Provider is NeoIniFileProvider fileProvider && fileProvider.UseBackup;
            set
            {
                if (Provider is NeoIniFileProvider fileProvider) fileProvider.UseBackup = value;
                else if (value) throw new UnsupportedProviderOperationException("The current INeoIniProvider does not support backup configuration.");
            }
        }

        /// <summary>
        /// Determines whether missing keys are automatically added to the file with a default value when requested via <see cref="GetValue{T}"/>. 
        /// Default is <c>true</c>.
        /// </summary>
        public bool UseAutoAdd { get; set; }

        /// <summary>
        /// Determines whether a checksum is calculated and verified during file load and save operations to ensure data integrity.
        /// When enabled, the configuration file includes a checksum that detects corruption or tampering.
        /// Default value is <c>true</c>.
        /// </summary>
        public bool UseChecksum
        {
            get => !HumanMode && _UseChecksum;
            set => _UseChecksum = value;
        }

        /// <summary>
        /// Determines whether the configuration is automatically saved when the instance is disposed.
        /// Default value is <c>true</c>.
        /// </summary>
        public bool SaveOnDispose { get; set; }

        /// <summary>
        /// Determines whether empty strings or null values are permitted for configuration keys.
        /// Default value is <c>true</c>.
        /// </summary>
        public bool AllowEmptyValues { get; set; }

        /// <summary>Determines whether data shielding is applied to the configuration.</summary>
        /// Default value is <c>false</c>.
        /// <exception cref="ModeConflictException">
        /// Thrown when attempting to set this property while Human Mode is active. 
        /// Shielding is incompatible with the manual editing capabilities of Human Mode.
        /// </exception>
        public bool UseShielding
        {
            get => _UseShielding;
            set
            {
                if (HumanMode) throw new ModeConflictException();
                _UseShielding = value;
            }
        }
    }
}
