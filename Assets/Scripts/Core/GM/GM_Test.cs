using UnityEngine;

public class GM_Test : MonoBehaviour, IManager
{
    public static GM_Test Instance => GameManager.Instance.gmTest;
    public void OnManagerInit()
    {
    }

    public void OnManagerUpdate(float deltTime)
    {
        if (Input.GetKeyDown(KeyCode.A)) {
            var loginUI = GM_UI.OpenUI<UI_Login>();
        }
        if (Input.GetKeyDown(KeyCode.D)) {
            var loginUI = GM_UI.CloseUI<UI_Login>();
        }
        if (Input.GetKeyDown(KeyCode.J)) {
            GM_UI.ActiveUI<UI_Login>();
        }
        if (Input.GetKeyDown(KeyCode.K)) {
            GM_UI.DeactiveUI<UI_Login>();
        }
    }

    public void OnManagerDestroy()
    {
    }
}
