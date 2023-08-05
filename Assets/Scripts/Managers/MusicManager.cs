using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


/// <summary>
/// 音乐管理器
/// </summary>
public class MusicManager : MonoBehaviour, IManager
{
    public static MusicManager Instance => GameManager.Instance.MusicMgr;
    
    //总 音量
    [Range(0,1)]
    public float AllVolume;
    //BGM 音量
    [Range(0,1)]
    public float BGMVolume;

    private AudioSource BGMSource;
    
    private AsyncOperationHandle<AudioClip> handle;
    public void OnManagerInit()
    {
        Debug.Log(DataManager.musicTable.BGM[0].MusicID);
        Debug.Log(DataManager.musicTable.BGM[0].MusicPath);

        //BGMSource = transform.Find("Audio_Source").GetComponent<AudioSource>();
        BGMSource = this.transform.GetComponentInChildren<AudioSource>();
        if(BGMSource == null) Debug.Log("没有播放器22222");
    }
    
    public void OnManagerUpdate(float deltTime){}
    public void OnManagerDestroy(){}

    public void ChangBGM(string BGMpath)
    {
        AudioClip audio;
        handle = Addressables.LoadAssetAsync<AudioClip>(BGMpath);
        audio = handle.WaitForCompletion();
        if (handle.Status == AsyncOperationStatus.Failed)
        {
            return;
        }
        if(BGMSource == null) Debug.Log("没有播放器");
        BGMSource.clip = audio;
        BGMSource.Play();
        Addressables.Release(handle);
    }
}
