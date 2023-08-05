using UnityEngine;

/// <summary>
/// 资源管理器
/// </summary>
public partial class DataManager : MonoBehaviour, IManager
{
    public static CharacterCreateTable characterCreateTable;
    public static MusicTable musicTable;

    public void OnManagerInit()
    {
        //todo
        //后面我改成自动生成 先这么用着
        characterCreateTable = XmlTools.BinaryDeSerialize<CharacterCreateTable>();
        musicTable = XmlTools.BinaryDeSerialize<MusicTable>();
    }

    public void OnManagerUpdate(float deltTime) { }

    public void OnManagerDestroy(){}
}