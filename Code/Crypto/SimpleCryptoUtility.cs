using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Nonsliep.Core.Crypto
{
    /// <summary>
    /// Provides simple AES encryption and decryption for strings.
    /// </summary>
    public static class SimpleCryptoUtility
    {
        private const string Prefix = "d1p::";
        private static readonly byte[] Key = CreateKey();

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using var aes = Aes.Create();
            if (aes == null) throw new InvalidOperationException("AES provider not available");

            aes.Key = Key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var ms = new MemoryStream();
            using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                var bytes = Encoding.UTF8.GetBytes(plainText);
                cryptoStream.Write(bytes, 0, bytes.Length);
            }

            var iv = aes.IV;
            var cipher = ms.ToArray();

            var payload = new byte[iv.Length + cipher.Length];
            Buffer.BlockCopy(iv, 0, payload, 0, iv.Length);
            Buffer.BlockCopy(cipher, 0, payload, iv.Length, cipher.Length);

            return Prefix + Convert.ToBase64String(payload);
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            if (!cipherText.StartsWith(Prefix, StringComparison.Ordinal)) return cipherText;

            var base64 = cipherText.Substring(Prefix.Length);
            var payload = Convert.FromBase64String(base64);

            using var aes = Aes.Create();
            if (aes == null) throw new InvalidOperationException("AES provider not available");

            aes.Key = Key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var ivLength = aes.BlockSize / 8;
            if (payload.Length < ivLength) throw new InvalidDataException("Cipher payload too short");

            var iv = new byte[ivLength];
            Buffer.BlockCopy(payload, 0, iv, 0, ivLength);
            aes.IV = iv;

            var cipher = new byte[payload.Length - ivLength];
            Buffer.BlockCopy(payload, ivLength, cipher, 0, cipher.Length);

            using var decryptor = aes.CreateDecryptor();
            using var ms = new MemoryStream(cipher);
            using var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static byte[] CreateKey()
        {
            // Derive a 256-bit key from a compile-time secret.
            const string secret = "D1Pro_SaveKey_v1";
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }
    }
}