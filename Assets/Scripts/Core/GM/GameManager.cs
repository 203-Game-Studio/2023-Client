using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance = null;
    public static GameManager Instance { get { return _instance; } }

    //挂载节点
    public Transform SceneRoot { get { return gameObject.transform; } }

    #region 游戏管理器类实例

    public GM_Net gmNet { get; set; }
    #endregion

    private void Awake()
    {
        _instance = this;
        DontDestroyOnLoad(_instance);

        gmNet = gameObject.GetComponentInChildren<GM_Net>();
    }

    private void Start()
    {
        gmNet.OnManagerInit();
    }

    private void Update()
    {
        float deltTime = Time.deltaTime;

        gmNet.OnManagerUpdate(deltTime);
    }

    private void OnDestroy()
    {
        gmNet.OnManagerDestroy();
    }
}
