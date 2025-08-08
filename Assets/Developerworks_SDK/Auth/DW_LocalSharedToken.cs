using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class DW_LocalSharedToken
{
    private const string TokenFileName = "shared_token.txt";

    // !!! 建议每一套 Unity 应用使用独立的 Key + IV !!!
    private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("/wu4uTqdUBpCIhutfM50qQ=="); // 16字节
    private static readonly byte[] AesIV  = Encoding.UTF8.GetBytes("pCkXFJR0Ahco+YKvkNRq2Q=="); // 16字节

    private static string GetSharedFilePath()
    {
        string folderPath = "";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // %AppData%
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Application Support");
#else
        folderPath=""
#endif

        if (string.IsNullOrEmpty(folderPath))
        {
            return null;
        }
        string fullPath = Path.Combine(folderPath, "MyUnitySharedData");
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        return Path.Combine(fullPath, TokenFileName);
    }

    public static void SaveToken(string token)
    {
        try
        {
            byte[] encrypted = EncryptStringToBytes_Aes(token, AesKey, AesIV);
            var path = GetSharedFilePath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }
            File.WriteAllBytes(path, encrypted);
            Debug.Log("Token saved (encrypted).");
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to save token: " + e.Message);
        }
    }

    public static string LoadToken()
    {
        try
        {
            string path = GetSharedFilePath();
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }
            if (File.Exists(path))
            {
                byte[] encrypted = File.ReadAllBytes(path);
                return DecryptStringFromBytes_Aes(encrypted, AesKey, AesIV);
            }
            else
            {
                Debug.LogWarning("Token file not found.");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to load token: " + e.Message);
            return null;
        }
    }

    public static void EraseToken()
    {
        try
        {
            string path = GetSharedFilePath();
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log("Token erased.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to erase token: " + e.Message);
        }
    }

    // === AES 加密方法 ===
    private static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] iv)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV  = iv;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using MemoryStream msEncrypt = new MemoryStream();
            using CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }
            return msEncrypt.ToArray();
        }
    }

    private static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] key, byte[] iv)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV  = iv;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using MemoryStream msDecrypt = new MemoryStream(cipherText);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new StreamReader(csDecrypt);
            return srDecrypt.ReadToEnd();
        }
    }
}
