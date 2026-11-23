using System;
using System.Security.Cryptography;
using System.Text;

namespace Afterglow.Utils;

/// <summary>
/// Encrypts secrets (e.g. an access token) for the current Windows user, so that they're not
/// stored as plain text in the settings file, and only this user on this PC can read them.
/// </summary>
internal static class SecretProtection
{
    // Ties the encrypted data to this use, so it can't be mixed up with other secrets
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Afterglow.Secret");

    public static string? Protect(string? secret)
    {
        if (string.IsNullOrEmpty(secret))
            return null;

        var data = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(secret),
            Entropy,
            DataProtectionScope.CurrentUser
        );

        return Convert.ToBase64String(data);
    }

    // Null if there's nothing, or if it can't be read (e.g. it was encrypted by another user, or
    // on another PC), in which case it has to be entered again
    public static string? TryUnprotect(string? protectedSecret)
    {
        if (string.IsNullOrEmpty(protectedSecret))
            return null;

        try
        {
            var data = ProtectedData.Unprotect(
                Convert.FromBase64String(protectedSecret),
                Entropy,
                DataProtectionScope.CurrentUser
            );

            return Encoding.UTF8.GetString(data);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return null;
        }
    }
}
