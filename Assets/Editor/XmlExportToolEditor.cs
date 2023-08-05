using UnityEditor;
using UnityEngine;

public partial class XmlExportToolEditor : EditorWindow
{
    static XmlExportToolEditor instance;

    [MenuItem("Tools/配置表导入导出工具", false, -1)]
    static void Init()
    {
        instance = (XmlExportToolEditor)EditorWindow.GetWindow(typeof(XmlExportToolEditor));
        instance.position = new Rect(400, 200, 500, 150);
        instance.titleContent = new GUIContent("配置表导入导出工具");
    }

    void OnGUI()
    {
        //todo
        /*if (GUILayout.Button("生成代码(如果有新表或者表结构改动需要点击)"))
        {
            Debug.Log("Clicked Button");
        }*/

        if (GUILayout.Button("生成bytes数据"))
        {
            GenerateAllBytes();
        }
    }

    private void GenerateAllBytes() {
        //todo 先手动加 后面我给改成自动生成这个脚本
        GenerateOneBytes<CharacterCreateTable>();
        GenerateOneBytes<MusicTable>();

        AssetDatabase.Refresh();
    }

    private void GenerateOneBytes<T>() {
        var table = XmlTools.XmlDeSerialize<T>();
        XmlTools.BinarySerialize<T>(table);
    }
}