using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Linage.Infrastructure.Security
{
    public static class EncryptionHelper
    {
        // In a real production environment, the key should be derived from a user secret (password/passphrase)
        // or stored in a secure OS storage (Keyring/DPAPI). 
        // For this implementation, we will use a machine-specific isolated storage approach or a fixed key path 
        // to ensure the application can decrypt its own data across runs, but strictly speaking, 
        // hardcoding or auto-generating a key without user input is a security trade-off.
        // We will generate a stable key based on machine identity for this "production-grade" prototype.
        
        private static readonly byte[] Key;
        private static readonly byte[] IV_Seed;

        static EncryptionHelper()
        {
            // Deterministic key derivation for the local machine context
            // Note: This protects against casual snooping but not against an attacker with full user access.
            // A true production app would prompt for a master password or use DPAPI (Windows-only).
            var machineId = Environment.MachineName + Environment.UserName;
            using (var sha = SHA256.Create())
            {
                Key = sha.ComputeHash(Encoding.UTF8.GetBytes(machineId + "LiNage_Master_Key_Salt"));
                var ivHash = sha.ComputeHash(Encoding.UTF8.GetBytes(machineId + "LiNage_IV_Salt"));
                IV_Seed = new byte[16];
                Array.Copy(ivHash, IV_Seed, 16);
            }
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.GenerateIV(); // Use random IV for each encryption
                var iv = aes.IV;

                using (var encryptor = aes.CreateEncryptor(aes.Key, iv))
                using (var ms = new MemoryStream())
                {
                    // Prepend IV to the stream (unencrypted)
                    ms.Write(iv, 0, iv.Length);
                    
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);
                
                using (var aes = Aes.Create())
                {
                    aes.Key = Key;
                    
                    // Extract IV
                    var iv = new byte[16];
                    if (fullCipher.Length < 16) throw new ArgumentException("Invalid cipher data");
                    Array.Copy(fullCipher, 0, iv, 0, 16);
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(fullCipher, 16, fullCipher.Length - 16))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException)
            {
                return null; // Decryption failed
            }
            catch (FormatException)
            {
                return null; // Invalid Base64
            }
        }
    }
}