using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class XmlTools
{
    public static T XmlDeSerialize<T>()
    {
        string name = typeof(T).Name;
        using (FileStream fileStream = new FileStream($"{Application.dataPath}/Tables/{name.Substring(0, name.Length-5)}.xml", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            T model = (T)xs.Deserialize(fileStream);
            return model;
        }
    }

    public static void BinarySerialize<T>(T serialize)
    {
        using (FileStream fileStream = new FileStream($"{Application.dataPath}/Tables/{typeof(T).Name}.bytes", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, serialize);
        }
    }

    public static T BinaryDeSerialize<T>()
    {
        string name = typeof(T).Name;
        TextAsset bytes = Addressables.LoadAssetAsync<TextAsset>($"{name.Substring(0, name.Length - 5)}").WaitForCompletion();
        using (MemoryStream memoryStream = new MemoryStream(bytes.bytes))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            T model = (T)binaryFormatter.Deserialize(memoryStream);
            return model;
        }
    }
}