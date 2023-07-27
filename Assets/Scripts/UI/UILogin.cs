using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILogin : UIBase
{
    public Button LoginBtn;
    public TMP_InputField UIDInputField;
    public TMP_InputField pwdInputField;
    public TMP_Text DebugText;

    public UILogin() : base("UILogin", EUILayerType.Upper, true) { }

    private void OnClickLoginBtn()
    {
        string uidStr = UIDInputField.text;
        string pwdStr = pwdInputField.text;
        int uid = -1;
        int.TryParse(uidStr, out uid);
        var req = NetLogin.Get(uid, pwdStr);
        req.OnSuccess = (NetMsg msg) =>
        {
            DebugText.SetText($"收到服务器登录回复！{msg.Code} {msg.LoginRes.Code}  {msg.LoginRes.DeviceId}");
        };
        req.Request();
        DebugText.SetText($"发送登录请求 uid:{uid} pwd:{pwdStr}");
    }

    protected override void OnInit()
    {
        LoginBtn = uiGameObject.transform.Find("LoginWindow/LoginBtn").GetComponent<Button>();
        UIDInputField = uiGameObject.transform.Find("LoginWindow/UID").GetComponent<TMP_InputField>();
        pwdInputField = uiGameObject.transform.Find("LoginWindow/Pwd").GetComponent<TMP_InputField>();
        DebugText = uiGameObject.transform.Find("Debug").GetComponent<TMP_Text>();

        LoginBtn.onClick.RemoveAllListeners();
        LoginBtn.onClick.AddListener(OnClickLoginBtn);
    }

    protected override void OnActive()
    {
        Debug.Log("UILogin OnActive");
    }

    protected override void OnDeActive()
    {
        Debug.Log("UILogin OnDeActive");
    }

    protected override void OnDestory()
    {
        Debug.Log("UILogin OnDestory");
        LoginBtn?.onClick.RemoveAllListeners();
    }
}