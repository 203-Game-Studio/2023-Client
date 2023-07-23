using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Login : MonoBehaviour
{
    public Button loginBtn;
    public TMP_InputField uidInputField;
    public TMP_InputField pwdInputField;
    public TMP_Text debug;

    void Start()
    {
        InitUI();
    }

    private void InitUI() {
        loginBtn.onClick.RemoveAllListeners();
        loginBtn.onClick.AddListener(OnClickLoginBtn);
    }

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
}