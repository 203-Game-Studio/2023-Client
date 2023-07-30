using UnityEngine;

public class TestManager : MonoBehaviour, IManager
{
    public static TestManager Instance => GameManager.Instance.TestMgr;
    public void OnManagerInit()
    {
        CharacterCreateTable a = XmlTools.XmlDeSerialize<CharacterCreateTable>();
        XmlTools.BinarySerialize<CharacterCreateTable>(a);
        CharacterCreateTable b = XmlTools.BinaryDeSerialize<CharacterCreateTable>();
        Debug.Log(b.Color.Length);
    }

    public void OnManagerUpdate(float deltTime)
    {
        if (Input.GetKeyDown(KeyCode.A)) {
            var loginUI = UIManager.OpenUI<UICharacterCreat>();
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            var loginUI = UIManager.CloseUI<UICharacterCreat>();
        }
        if (Input.GetKeyDown(KeyCode.J)) {
            UIManager.ActiveUI<UICharacterCreat>();
        }
        if (Input.GetKeyDown(KeyCode.K)) {
            UIManager.DeactiveUI<UICharacterCreat>();
        }
    }

    public void OnManagerDestroy()
    {
    }
}
