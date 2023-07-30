
// 注意: 生成的代码可能至少需要 .NET Framework 4.5 或 .NET Core/Standard 2.0。
/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class CharacterCreateTable
{

    private CharacterCreateTableColor[] colorField;

    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("Color")]
    public CharacterCreateTableColor[] Color
    {
        get
        {
            return this.colorField;
        }
        set
        {
            this.colorField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class CharacterCreateTableColor
{

    private byte idField;

    private string descField;

    private string texturePathField;

    /// <remarks/>
    public byte id
    {
        get
        {
            return this.idField;
        }
        set
        {
            this.idField = value;
        }
    }

    /// <remarks/>
    public string desc
    {
        get
        {
            return this.descField;
        }
        set
        {
            this.descField = value;
        }
    }

    /// <remarks/>
    public string texturePath
    {
        get
        {
            return this.texturePathField;
        }
        set
        {
            this.texturePathField = value;
        }
    }
}

