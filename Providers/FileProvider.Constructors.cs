using NeoIni.Models;

namespace NeoIni.Providers;

internal partial class NeoIniFileProvider
{
    internal NeoIniFileProvider(string filePath, IEncryptionProvider encryptionProvider)
    {
        FilePath = filePath;
        EncryptionProvider = encryptionProvider;
    }

    internal NeoIniFileProvider(string filePath, EncryptionParameters encryptionParameters, bool autoModeEncryption, IEncryptionProvider encryptionProvider)
    {
        FilePath = filePath;
        EncryptionKey = encryptionParameters.Key;
        Salt = encryptionParameters.Salt;
        Encryption = true;
        AutoEncryption = autoModeEncryption;
        EncryptionProvider = encryptionProvider;
    }
}
