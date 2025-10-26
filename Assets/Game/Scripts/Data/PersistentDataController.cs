using System;
using System.IO;
using UnityEngine;

public class PersistentDataController
{
    public static T LoadData<T>(string path) where T : new()
    {
        if (File.Exists(path))
        {
            using FileStream stream = new FileStream(path, FileMode.Open);
            using StreamReader reader = new StreamReader(stream);
            string data = SecurePersistentData.Decrypt(reader.ReadToEnd());

            return JsonUtility.FromJson<T>(data);
        }
        else
        {
            T data = new T();
            SaveData(data, path);

            return data;
        }
    }

    public static bool SaveData<T>(T data, string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            string dataStr = SecurePersistentData.Encrypt(JsonUtility.ToJson(data, true));
            using FileStream stream = new FileStream(path, FileMode.Create);
            using StreamWriter writer = new StreamWriter(stream);
            writer.Write(dataStr);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
}