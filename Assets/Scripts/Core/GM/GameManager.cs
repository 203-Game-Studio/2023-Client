using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance { get { return _instance; } }

    //挂载节点
    public Transform SceneRoot { get { return gameObject.transform; } }

    #region 游戏管理器类实例

    //网络管理
    public GM_Net gmNet { get; set; }
    //UI管理
    public GM_UI gmUI { get; set; }
    //测试脚本挂载
    public GM_Test gmTest { get; set; }
    #endregion

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(_instance);

        gmNet = gameObject.GetComponentInChildren<GM_Net>();
        gmUI = gameObject.GetComponentInChildren<GM_UI>();
        gmTest = gameObject.GetComponentInChildren<GM_Test>();
    }

    private void Start()
    {
        gmNet.OnManagerInit();
        gmUI.OnManagerInit();
        gmTest.OnManagerInit();
    }

    private void Update()
    {
        float deltTime = Time.deltaTime;

        gmNet.OnManagerUpdate(deltTime);
        gmUI.OnManagerUpdate(deltTime);
        gmTest.OnManagerUpdate(deltTime);
    }

    private void OnDestroy()
    {
        gmNet.OnManagerDestroy();
        gmUI.OnManagerDestroy();
        gmTest.OnManagerDestroy();
    }
}
