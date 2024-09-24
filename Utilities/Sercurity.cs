using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;


namespace WebNails.Payment.Utilities
{
    public class Sercurity
    {
        private static readonly string HashAlgorithm = "SHA1";
        private static readonly int PasswordIterations = 2;

        private static string Encrypt(string PlainText, string Password, string Salt, string Vector)
        {
            if (string.IsNullOrEmpty(PlainText))
                return "";

            byte[] InitialVectorBytes = Encoding.ASCII.GetBytes(Vector);
            byte[] SaltValueBytes = Encoding.ASCII.GetBytes(Salt);
            byte[] PlainTextBytes = Encoding.UTF8.GetBytes(PlainText);
            PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(Password, SaltValueBytes, HashAlgorithm, PasswordIterations);
            byte[] KeyBytes = DerivedPassword.GetBytes(32);
            RijndaelManaged SymmetricKey = new RijndaelManaged
            {
                Mode = CipherMode.CBC
            };
            byte[] CipherTextBytes = null;
            using (ICryptoTransform Encryptor = SymmetricKey.CreateEncryptor(KeyBytes, InitialVectorBytes))
            {
                using (MemoryStream MemStream = new MemoryStream())
                {
                    using (CryptoStream CryptoStream = new CryptoStream(MemStream, Encryptor, CryptoStreamMode.Write))
                    {
                        CryptoStream.Write(PlainTextBytes, 0, PlainTextBytes.Length);
                        CryptoStream.FlushFinalBlock();
                        CipherTextBytes = MemStream.ToArray();
                        MemStream.Close();
                        CryptoStream.Close();
                    }
                }
            }
            SymmetricKey.Clear();
            return Convert.ToBase64String(CipherTextBytes);
        }

        private static string Decrypt(string CipherText, string Password, string Salt, string Vector)
        {
            try
            {

                if (string.IsNullOrEmpty(CipherText))
                    return "";
                byte[] InitialVectorBytes = Encoding.ASCII.GetBytes(Vector);
                byte[] SaltValueBytes = Encoding.ASCII.GetBytes(Salt);
                byte[] CipherTextBytes = Convert.FromBase64String(CipherText);
                PasswordDeriveBytes DerivedPassword = new PasswordDeriveBytes(Password, SaltValueBytes, HashAlgorithm, PasswordIterations);
                byte[] KeyBytes = DerivedPassword.GetBytes(32);
                RijndaelManaged SymmetricKey = new RijndaelManaged
                {
                    Mode = CipherMode.CBC
                };
                byte[] PlainTextBytes = new byte[CipherTextBytes.Length];
                int ByteCount = 0;
                using (ICryptoTransform Decryptor = SymmetricKey.CreateDecryptor(KeyBytes, InitialVectorBytes))
                {
                    using (MemoryStream MemStream = new MemoryStream(CipherTextBytes))
                    {
                        using (CryptoStream CryptoStream = new CryptoStream(MemStream, Decryptor, CryptoStreamMode.Read))
                        {

                            ByteCount = CryptoStream.Read(PlainTextBytes, 0, PlainTextBytes.Length);
                            MemStream.Close();
                            CryptoStream.Close();
                        }
                    }
                }
                SymmetricKey.Clear();
                return Encoding.UTF8.GetString(PlainTextBytes, 0, ByteCount);
            }
            catch
            {
                return CipherText;
            }
        }

        public static string EncryptToBase64(string PlainText, string Password, string Salt, string Vector)
        {
            string text = Encrypt(PlainText, Password, Salt, Vector);
            var bytes = Encoding.UTF8.GetBytes(text);
            var base64 = Convert.ToBase64String(bytes);
            return base64;
        }

        public static string DecryptFromBase64(string CipherText, string Password, string Salt, string Vector)
        {
            var data_bytes = Convert.FromBase64String(CipherText);
            var text = Encoding.UTF8.GetString(data_bytes);
            string plain = Decrypt(text, Password, Salt, Vector);
            return plain;
        }
    }
}