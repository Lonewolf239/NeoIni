using System;
using NeoIni.Models;

namespace NeoIni.Providers
{
    internal partial class NeoIniFileProvider
    {
        internal NeoIniFileProvider(string? filePath, IEncryptionProvider encryptionProvider)
        {
            if (filePath is null) throw new ArgumentNullException("File path cannot be null.");
            FilePath = filePath;
            EncryptionProvider = encryptionProvider;
        }

        internal NeoIniFileProvider(string? filePath, EncryptionParameters encryptionParameters, bool autoModeEncryption, IEncryptionProvider encryptionProvider)
        {
            if (filePath is null) throw new ArgumentNullException("File path cannot be null.");
            FilePath = filePath;
            EncryptionKey = encryptionParameters.Key;
            Salt = encryptionParameters.Salt;
            Encryption = true;
            AutoEncryption = autoModeEncryption;
            EncryptionProvider = encryptionProvider;
        }
    }
}
