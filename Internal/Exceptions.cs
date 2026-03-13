using System;

namespace NeoIni.Internal;

internal sealed class MissingEncryptionKeyException : Exception
{
    public MissingEncryptionKeyException()
        : base("The configuration file is encrypted with a custom password. Please provide a password string to the NeoIniReader constructor.") { }
}

internal sealed class InvalidEncryptionKeyException : InvalidOperationException
{
    public InvalidEncryptionKeyException(System.Security.Cryptography.CryptographicException ex)
        : base("Failed to decrypt configuration file.\nCheck that you are using the same encryption password or environment as during file creation", ex) { }
}

internal sealed class UnsupportedIniCharacterException : ArgumentException
{
    public UnsupportedIniCharacterException() : base("The string contains unsupported characters (such as ; \" ' =).") { }
}

internal sealed class InvalidHotReloadDelayException : ArgumentOutOfRangeException
{
    public InvalidHotReloadDelayException(string paramName)
        : base(paramName, "Hot reload polling delay must be at least 1000 milliseconds.") { }
}

internal sealed class EmptyValueNotAllowedException : ArgumentException
{
    public EmptyValueNotAllowedException(string paramName)
        : base("Empty or null strings are not allowed when AllowEmptyValues is disabled.", paramName) { }
}
