using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SampleApp.Services
{
    public class RSAEncryption
    {
        private static readonly string KeyDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SecureKeys");
        private static readonly string PublicKeyPath = Path.Combine(KeyDirectory, "publicKey.xml");
        private static readonly string PrivateKeyPath = Path.Combine(KeyDirectory, "privateKey.xml");

        private static RSA rsa = RSA.Create();

        static RSAEncryption()
        {
            if (!Directory.Exists(KeyDirectory))
                Directory.CreateDirectory(KeyDirectory);

            if (File.Exists(PublicKeyPath) && File.Exists(PrivateKeyPath))
            {
                LoadKeys();
            }
            else
            {
                GenerateAndSaveKeys();
            }
        }

        private static void GenerateAndSaveKeys()
        {
            File.WriteAllText(PublicKeyPath, rsa.ToXmlString(false)); // Public Key
            File.WriteAllText(PrivateKeyPath, rsa.ToXmlString(true)); // Private Key
        }

        private static void LoadKeys()
        {
            rsa.FromXmlString(File.ReadAllText(PrivateKeyPath)); // Load Private Key
        }

        public static string Encrypt(string plainText)
        {
            rsa.FromXmlString(File.ReadAllText(PublicKeyPath)); // Load Public Key
            byte[] encryptedBytes = rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA256);

            // Convert to Base64 and make it URL-safe
            return Convert.ToBase64String(encryptedBytes)
                .Replace("+", "-")  // Replace '+' with '-'
                .Replace("/", "_")  // Replace '/' with '_'
                .TrimEnd('=');      // Remove padding '='
        }

        public static string Decrypt(string encryptedText)
        {
            rsa.FromXmlString(File.ReadAllText(PrivateKeyPath)); // Load Private Key

            // Convert back to standard Base64
            string base64 = encryptedText
                .Replace("-", "+")
                .Replace("_", "/");

            int padding = (4 - base64.Length % 4) % 4;
            base64 = base64.PadRight(base64.Length + padding, '=');

            byte[] decryptedBytes = rsa.Decrypt(Convert.FromBase64String(base64), RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
