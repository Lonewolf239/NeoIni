using System.Text;

namespace NeoIni.Providers
{
    internal partial class NeoIniFileProvider
    {
        private IEncryptionProvider EncryptionProvider;

        private const byte FileVersion = 2;
        private const byte LegacyAutoFileVersion = 1;
        private const int HeaderSize = 10;
        private const int IvSize = 16;
        private const int SaltSize = 16;
        private const int ChecksumSize = 32;
        private const string WarningText = "; WARNING: This file is auto-generated.\n; Any manual changes will be overwritten and may cause data loss.\n; The original data will be restored from backup.\n";

        private static readonly byte[] FileSignature = { (byte)'N', (byte)'I', (byte)'N', (byte)'I' };
        private static readonly byte[] WarningBytes = Encoding.UTF8.GetBytes(WarningText);
        private static readonly string[] LineSeparators = new[] { "\r\n", "\n", "\r" };

        private readonly string FilePath;
        private byte[]? EncryptionKey;
        private byte[]? Salt;
        private readonly bool AutoEncryption = false;

        /// <summary>Set when a version-1 auto-encrypted file was just decrypted with the legacy scheme and needs to be re-saved under the new one.</summary>
        internal bool PendingAutoMigration { get; private set; }

        private string TempFilePath => FilePath + ".tmp";
        private string BackupFilePath => FilePath + ".backup";
        internal bool UseBackup = true;

        internal readonly bool Encryption = false;
    }
}
