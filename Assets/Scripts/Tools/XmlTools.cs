using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class XmlTools
{
    public static T XmlDeSerialize<T>()
    {
        using (FileStream fileStream = new FileStream($"{Application.dataPath}/Tables/{typeof(T).Name}.xml", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            T model = (T)xs.Deserialize(fileStream);
            return model;
        }
    }

    public static void BinarySerialize<T>(T serialize)
    {
        using (FileStream fileStream = new FileStream($"{Application.dataPath}/Tables/Bytes/{typeof(T).Name}.bytes", FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, serialize);
        }
    }

    public static T BinaryDeSerialize<T>()
    {
        TextAsset bytes = Addressables.LoadAssetAsync<TextAsset>($"{typeof(T).Name}").WaitForCompletion();
        using (MemoryStream memoryStream = new MemoryStream(bytes.bytes))
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            T model = (T)binaryFormatter.Deserialize(memoryStream);
            return model;
        }
    }
}