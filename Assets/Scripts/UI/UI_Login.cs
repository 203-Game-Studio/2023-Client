using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Login : UIBase
{
    public Button loginBtn;
    public TMP_InputField uidInputField;
    public TMP_InputField pwdInputField;
    public TMP_Text debug;

    public UI_Login() : base("UI_Login", UILayerType.Upper, UILoadType.Async){}

    private void OnClickLoginBtn() {
        string uidStr = uidInputField.text;
        string pwdStr = pwdInputField.text;
        int uid = -1;
        int.TryParse(uidStr, out uid);
        var req = Net_Login.Get(uid, pwdStr);
        req.OnSuccess = (NetMsg msg) =>
        {
            debug.SetText($"收到服务器登录回复！{msg.Code} {msg.LoginRes.Code}  {msg.LoginRes.DeviceId}");
        };
        req.Request();
        debug.SetText($"发送登录请求 uid:{uid} pwd:{pwdStr}");
    }

    protected override void OnInit()
    {
        loginBtn.onClick.RemoveAllListeners();
        loginBtn.onClick.AddListener(OnClickLoginBtn);
    }

    protected override void OnActive()
    {
        Debug.Log("UI_Login OnActive");
    }

    protected override void OnDeActive()
    {
        Debug.Log("UI_Login OnDeActive");
    }

    protected override void OnDestory()
    {
        Debug.Log("UI_Login OnDestory");
        loginBtn.onClick.RemoveAllListeners();
    }
}