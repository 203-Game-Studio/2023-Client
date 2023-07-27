using UnityEngine;
using UnityEngine.AddressableAssets;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance { get { return _instance; } }

    //挂载节点
    public Transform SceneRoot { get { return gameObject.transform; } }

    #region 游戏管理器类实例

    //网络管理
    public NetManager NetMgr { get; set; }
    //UI管理
    public UIManager UIMgr { get; set; }
    //资源管理
    public AssetManager AssetMgr { get; set; }
    //测试脚本挂载
    public TestManager TestMgr { get; set; }
    #endregion

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(_instance);

        //提前初始化Addressables 否则第一次调用WaitForCompletion会卡死
        Addressables.InitializeAsync();

        NetMgr = gameObject.GetComponentInChildren<NetManager>();
        AssetMgr = gameObject.GetComponentInChildren<AssetManager>();
        UIMgr = gameObject.GetComponentInChildren<UIManager>();
        TestMgr = gameObject.GetComponentInChildren<TestManager>();
    }

    private void Start()
    {
        NetMgr.OnManagerInit();
        AssetMgr.OnManagerInit();
        UIMgr.OnManagerInit();
        TestMgr.OnManagerInit();
    }

    private void Update()
    {
        float deltTime = Time.deltaTime;

        NetMgr.OnManagerUpdate(deltTime);
        AssetMgr.OnManagerUpdate(deltTime);
        UIMgr.OnManagerUpdate(deltTime);
        TestMgr.OnManagerUpdate(deltTime);
    }

    private void OnDestroy()
    {
        NetMgr.OnManagerDestroy();
        AssetMgr.OnManagerDestroy();
        UIMgr.OnManagerDestroy();
        TestMgr.OnManagerDestroy();
    }
}
