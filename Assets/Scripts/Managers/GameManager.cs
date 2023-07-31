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
    //配置表数据管理
    public DataManager DataMgr { get; set; }

    public ScenceManager ScenceMgr { get; set; }
    //测试脚本挂载
    public TestManager TestMgr { get; set; }
    #endregion

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(_instance);

        //提前初始化Addressables 否则第一次调用WaitForCompletion会卡死
        Addressables.InitializeAsync();

        AssetMgr = gameObject.GetComponentInChildren<AssetManager>();
        DataMgr = gameObject.GetComponentInChildren<DataManager>();
        NetMgr = gameObject.GetComponentInChildren<NetManager>();
        UIMgr = gameObject.GetComponentInChildren<UIManager>();
        ScenceMgr = gameObject.GetComponentInChildren<ScenceManager>();
        TestMgr = gameObject.GetComponentInChildren<TestManager>();
    }

    private void Start()
    {
        DataMgr.OnManagerInit();
        NetMgr.OnManagerInit();
        AssetMgr.OnManagerInit();
        UIMgr.OnManagerInit();
        ScenceMgr.OnManagerInit();
        TestMgr.OnManagerInit();
    }

    private void Update()
    {
        float deltTime = Time.deltaTime;

        DataMgr.OnManagerUpdate(deltTime);
        NetMgr.OnManagerUpdate(deltTime);
        AssetMgr.OnManagerUpdate(deltTime);
        UIMgr.OnManagerUpdate(deltTime);
        ScenceMgr.OnManagerUpdate(deltTime);
        TestMgr.OnManagerUpdate(deltTime);
    }

    private void OnDestroy()
    {
        DataMgr.OnManagerDestroy();
        NetMgr.OnManagerDestroy();
        AssetMgr.OnManagerDestroy();
        UIMgr.OnManagerDestroy();
        ScenceMgr.OnManagerDestroy();
        TestMgr.OnManagerDestroy();
    }
}
