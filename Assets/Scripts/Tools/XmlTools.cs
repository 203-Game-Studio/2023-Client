using Google.Protobuf;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class XmlTools
{
    public static CharacterCreateTable XmlDeSerialize()
    {
        using (FileStream fs = new FileStream($"{Application.dataPath}/Tables/CharacterCreate.xml", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            XmlSerializer xs = new XmlSerializer(typeof(CharacterCreateTable));
            CharacterCreateTable testSerialize = (CharacterCreateTable)xs.Deserialize(fs);
            return testSerialize;
        }
    }
}