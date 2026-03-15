using System;

namespace NeoIni.Models;

/// <summary>
/// Represents the error that occurs when an encrypted configuration file is accessed without providing the required password.
/// </summary>
public class MissingEncryptionKeyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingEncryptionKeyException"/> class.
    /// </summary>
    public MissingEncryptionKeyException()
        : base("The configuration file is encrypted with a custom password. Please provide a password string to the NeoIniReader constructor.") { }
}

/// <summary>
/// Represents the error that occurs when decryption of a configuration file fails, typically due to an incorrect password or environment mismatch.
/// </summary>
public class InvalidEncryptionKeyException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidEncryptionKeyException"/> class with a reference to the inner cryptographic exception.
    /// </summary>
    /// <param name="ex">The inner cryptographic exception that is the cause of this exception.</param>
    public InvalidEncryptionKeyException(System.Security.Cryptography.CryptographicException ex)
        : base("Failed to decrypt configuration file.\nCheck that you are using the same encryption password or environment as during file creation", ex) { }
}

/// <summary>
/// Represents the error that occurs when a string contains characters that are invalid or reserved in INI formatting.
/// </summary>
public class UnsupportedIniCharacterException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedIniCharacterException"/> class.
    /// </summary>
    public UnsupportedIniCharacterException() : base("The string contains unsupported characters (such as ; \" ' =).") { }
}

/// <summary>
/// Represents the error that occurs when a specified hot reload polling delay is below the minimum allowed threshold.
/// </summary>
public class InvalidHotReloadDelayException : ArgumentOutOfRangeException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidHotReloadDelayException"/> class with the name of the parameter that caused the exception.
    /// </summary>
    /// <param name="paramName">The name of the parameter that caused the exception.</param>
    public InvalidHotReloadDelayException(string paramName)
        : base(paramName, "Hot reload polling delay must be at least 1000 milliseconds.") { }
}

/// <summary>
/// Represents the error that occurs when an empty or null string is provided to a configuration setting that requires a value.
/// </summary>
public class EmptyValueNotAllowedException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyValueNotAllowedException"/> class with the name of the parameter that caused the exception.
    /// </summary>
    /// <param name="paramName">The name of the parameter that caused the exception.</param>
    public EmptyValueNotAllowedException(string paramName)
        : base("Empty or null strings are not allowed when AllowEmptyValues is disabled.", paramName) { }
}

/// <summary>
/// Represents the error that occurs when an operation, such as managing backups or deleting files, is not supported by the current INeoIniProvider.
/// </summary>
public class UnsupportedProviderOperationException : NotSupportedException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedProviderOperationException"/>.
    /// </summary>
    public UnsupportedProviderOperationException() : base("The current INeoIniProvider does not support backup operations.") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsupportedProviderOperationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public UnsupportedProviderOperationException(string message) : base(message) { }
}
