using System.Collections.Generic;

[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public class BGM
{
    /// <summary>
    /// 
    /// </summary>
    public string MusicID { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string MusicPath { get; set; }

}
public class MusicTable
{
    /// <summary>
    /// BGM
    /// </summary>
    public List<BGM> BGM { get; set; }

}
public class Root
{
    /// <summary>
    /// MusicTable
    /// </summary>
    public MusicTable MusicTable { get; set; }

}