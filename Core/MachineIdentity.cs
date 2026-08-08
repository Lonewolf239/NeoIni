using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace NeoIni.Core
{
    /// <summary>
    /// Provides high-entropy, machine-bound secret material used to derive automatic encryption keys.
    /// Unlike identity strings (user name, machine name), the values produced here are not observable
    /// over the network and are not guessable from public information.
    /// </summary>
    /// <remarks>
    /// Windows-specific APIs (DPAPI, registry) are always called behind an <see cref="IsWindows"/> runtime
    /// check; the platform-compatibility analyzer cannot see through that indirection, hence the suppression below.
    /// </remarks>
#pragma warning disable CA1416
    internal static class MachineIdentity
    {
        private const int SecretSize = 32;
        private static readonly object FileLock = new object();
        private static byte[]? CachedSecret;
        private static byte[]? CachedMachineId;

        internal static byte[] GetMachineSecret()
        {
            if (!(CachedSecret is null)) return CachedSecret;
            lock (FileLock)
            {
                if (!(CachedSecret is null)) return CachedSecret;
                string path = GetSecretFilePath();
                byte[]? secret = TryReadSecret(path);
                if (secret is null)
                {
                    byte[] candidate = GenerateRandomBytes(SecretSize);
                    secret = TryCreateSecretExclusively(path, candidate) ?? candidate;
                }
                CachedSecret = secret;
                return secret;
            }
        }

        internal static byte[] GetMachineId()
        {
            if (!(CachedMachineId is null)) return CachedMachineId;
            CachedMachineId = IsWindows() ? GetWindowsMachineGuid() : IsLinux() ? GetLinuxMachineId() : Array.Empty<byte>();
            return CachedMachineId;
        }

        private static string GetSecretFilePath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
            return Path.Combine(root, "NeoIni", "machine.key");
        }

        private static byte[] GenerateRandomBytes(int size)
        {
            var bytes = new byte[size];
#if NETSTANDARD2_0
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
#else
            RandomNumberGenerator.Fill(bytes);
#endif
            return bytes;
        }

        private static byte[]? TryReadSecret(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                byte[] raw = File.ReadAllBytes(path);
                if (raw.Length == 0) return null;
                return IsWindows() ? Unprotect(raw) : raw;
            }
            catch { return null; }
        }

        /// <summary>
        /// Attempts to atomically create the secret file so two processes racing on first run cannot each
        /// generate and persist a different secret. If another process wins the race, reads what it wrote instead.
        /// </summary>
        private static byte[]? TryCreateSecretExclusively(string path, byte[] candidate)
        {
            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                byte[] toWrite = IsWindows() ? Protect(candidate) : candidate;
                using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    fs.Write(toWrite, 0, toWrite.Length);
                TryRestrictPermissions(path);
                return candidate;
            }
            catch (IOException)
            {
                return TryReadSecret(path);
            }
            catch { return null; }
        }

#if NET7_0_OR_GREATER
        private static void TryRestrictPermissions(string path)
        {
            try
            {
                if (!IsWindows())
                    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch { }
        }
#else
        private const int UserReadWriteMode = 384;

        [DllImport("libc", SetLastError = true)]
        private static extern int chmod(string pathname, int mode);

        private static void TryRestrictPermissions(string path)
        {
            try
            {
                if (!IsWindows()) chmod(path, UserReadWriteMode);
            }
            catch { }
        }
#endif

#if NET5_0
        /// <remarks>
        /// System.Security.Cryptography.ProtectedData and Microsoft.Win32.Registry do not ship a net5.0-compatible
        /// asset, so net5.0 falls back to an unprotected secret file with no registry-derived machine identifier.
        /// </remarks>
        private static byte[] Protect(byte[] data) => data;

        private static byte[] Unprotect(byte[] data) => data;

        private static byte[] GetWindowsMachineGuid() => Array.Empty<byte>();
#else
        private static byte[] Protect(byte[] data) =>
            ProtectedData.Protect(data, null, DataProtectionScope.LocalMachine);

        private static byte[] Unprotect(byte[] data) =>
            ProtectedData.Unprotect(data, null, DataProtectionScope.LocalMachine);

        private static byte[] GetWindowsMachineGuid()
        {
            try
            {
                using var hklm = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64);
                using var key = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                if (key?.GetValue("MachineGuid") is string guid && !string.IsNullOrEmpty(guid))
                    return Encoding.UTF8.GetBytes(guid);
            }
            catch { }
            return Array.Empty<byte>();
        }
#endif

        private static byte[] GetLinuxMachineId()
        {
            foreach (var path in new[] { "/etc/machine-id", "/var/lib/dbus/machine-id" })
            {
                try
                {
                    if (File.Exists(path))
                    {
                        string id = File.ReadAllText(path).Trim();
                        if (id.Length > 0) return Encoding.UTF8.GetBytes(id);
                    }
                }
                catch { }
            }
            return Array.Empty<byte>();
        }

        private static bool IsWindows()
        {
#if NETSTANDARD2_0
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            return OperatingSystem.IsWindows();
#endif
        }

        private static bool IsLinux()
        {
#if NETSTANDARD2_0
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#else
            return OperatingSystem.IsLinux();
#endif
        }
    }
}
#pragma warning restore CA1416
