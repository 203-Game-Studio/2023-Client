using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharacterCreat : UIBase
{
    /*public Button LoginBtn;
    public TMP_InputField UIDInputField;
    public TMP_InputField pwdInputField;
    public TMP_Text DebugText;*/

    public UICharacterCreat() : base("UICharacterCreat", EUILayerType.Normal, true) { }

    private void OnClickLoginBtn()
    {
    }

    protected override void OnInit()
    {
        /*LoginBtn = uiGameObject.transform.Find("LoginWindow/LoginBtn").GetComponent<Button>();
        UIDInputField = uiGameObject.transform.Find("LoginWindow/UID").GetComponent<TMP_InputField>();
        pwdInputField = uiGameObject.transform.Find("LoginWindow/Pwd").GetComponent<TMP_InputField>();
        DebugText = uiGameObject.transform.Find("Debug").GetComponent<TMP_Text>();

        LoginBtn.onClick.RemoveAllListeners();
        LoginBtn.onClick.AddListener(OnClickLoginBtn);*/
    }

    protected override void OnActive()
    {
        Debug.Log("UICharacterCreat OnActive");
    }

    protected override void OnDeActive()
    {
        Debug.Log("UICharacterCreat OnDeActive");
    }

    protected override void OnDestory()
    {
        Debug.Log("UICharacterCreat OnDestory");
    }
}