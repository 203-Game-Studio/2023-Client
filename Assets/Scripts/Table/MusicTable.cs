
// 注意: 生成的代码可能至少需要 .NET Framework 4.5 或 .NET Core/Standard 2.0。
/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class MusicTable
{

    private MusicTableBGM[] bGMField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("BGM")]
    public MusicTableBGM[] BGM
    {
        get
        {
            return this.bGMField;
        }
        set
        {
            this.bGMField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class MusicTableBGM
{

    private string musicIDField;

    private string musicPathField;

    /// <remarks/>
    public string MusicID
    {
        get
        {
            return this.musicIDField;
        }
        set
        {
            this.musicIDField = value;
        }
    }

    /// <remarks/>
    public string MusicPath
    {
        get
        {
            return this.musicPathField;
        }
        set
        {
            this.musicPathField = value;
        }
    }
}

