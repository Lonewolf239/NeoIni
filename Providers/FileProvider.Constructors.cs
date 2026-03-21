using System;

using NeoIni.Models;

namespace NeoIni.Providers;

internal partial class NeoIniFileProvider
{
    internal NeoIniFileProvider(string? filePath, IEncryptionProvider encryptionProvider)
    {
        ArgumentNullException.ThrowIfNull(filePath, "File path cannot be null.");
        FilePath = filePath;
        EncryptionProvider = encryptionProvider;
    }

    internal NeoIniFileProvider(string? filePath, EncryptionParameters encryptionParameters, bool autoModeEncryption, IEncryptionProvider encryptionProvider)
    {
        ArgumentNullException.ThrowIfNull(filePath, "File path cannot be null.");
        FilePath = filePath;
        EncryptionKey = encryptionParameters.Key;
        Salt = encryptionParameters.Salt;
        Encryption = true;
        AutoEncryption = autoModeEncryption;
        EncryptionProvider = encryptionProvider;
    }
}
