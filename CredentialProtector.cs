using System;
using System.Security.Cryptography;
using System.Text;

namespace Flow.Launcher.Plugin.DeepLTranslate
{
    /// <summary>
    /// Encrypts/decrypts the DeepL API key using the Windows Data Protection API (DPAPI).
    /// The key is protected at rest so it never appears in plaintext in the settings JSON.
    /// </summary>
    public static class CredentialProtector
    {
        // Optional entropy ties the protected blob to this plugin (not strictly a secret).
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Flow.Launcher.Plugin.DeepLTranslate.v1");

        /// <summary>Encrypts a plaintext key and returns a Base64 string (empty in → empty out).</summary>
        public static string Protect(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
                return string.Empty;

            var data = Encoding.UTF8.GetBytes(plaintext);
            var encrypted = ProtectedData.Protect(data, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        /// <summary>Decrypts a Base64-encoded protected key. Returns empty string on any failure.</summary>
        public static string Unprotect(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64))
                return string.Empty;

            try
            {
                var encrypted = Convert.FromBase64String(encryptedBase64);
                var data = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(data);
            }
            catch (Exception)
            {
                // Decryption failed (e.g. settings copied from another Windows user/profile).
                // Treat as "not configured" so the user can re-enter the key.
                return string.Empty;
            }
        }
    }
}
