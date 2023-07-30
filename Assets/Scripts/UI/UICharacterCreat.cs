using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UnityEngine.ResourceManagement.AsyncOperations;

struct Rebotconfig
{
    public int Color;
    public int EyeColor;
    public string playername;
    static public readonly Rebotconfig Default = new Rebotconfig{ Color = 1,EyeColor = 1,playername = "玩家1" };
}


public class UICharacterCreat : UIBase
{
    public Button ApplyBtn;
    public Button RandomBtn;
    public TMP_InputField NameInputField;

    public Button ColorItemRed;
    public Button ColorItemBlue;
    public Button ColorItemYellow;
    public Button ColorItemBlack;

    public Button EyeItem1;
    public Button EyeItem2;
    public Button EyeItem3;
    public Button EyeItem4;
    public Button EyeItem5;

    public Material Rebot_Material = GameManager.Instance.ScenceMgr.RobotMaterial;
    public Material RebotEye_Material = GameManager.Instance.ScenceMgr.RobotEyesMaterial;

    private Rebotconfig _rebotconfig = Rebotconfig.Default;
    
    private AsyncOperationHandle<Texture> handle;
    

    public UICharacterCreat() : base("UICharacterCreat", EUILayerType.Normal, true) { }

    private void OnClickColorChange(int idx)
    {
        int index;
        Texture tex;
        
        if (idx / 10 == 0)
        {
            index = idx% 10;
            _rebotconfig.Color = index;
            switch (index)
            {
                case 1:
                    handle = Addressables.LoadAssetAsync<Texture>("mixbot_low_mixamo_edit1_AlbedoTransparency");
                    tex = handle.WaitForCompletion();
                    if (handle.Status == AsyncOperationStatus.Failed)
                    {
                        break;
                    }
                    Rebot_Material.mainTexture = tex;
                    Addressables.Release(handle);
                    break;
                case 2:
                    handle = Addressables.LoadAssetAsync<Texture>("t_jammo_blue");
                    tex = handle.WaitForCompletion();
                    if (handle.Status == AsyncOperationStatus.Failed)
                    {
                        break;
                    }
                    Rebot_Material.mainTexture = tex;
                    Addressables.Release(handle);
                    break;
                case 3:
                    handle = Addressables.LoadAssetAsync<Texture>("t_jammo_yellow");
                    tex = handle.WaitForCompletion();
                    if (handle.Status == AsyncOperationStatus.Failed)
                    {
                        break;
                    }
                    Rebot_Material.mainTexture = tex;
                    Addressables.Release(handle);
                    break;
                case 4:
                    handle = Addressables.LoadAssetAsync<Texture>("t_jammo_black");
                    tex = handle.WaitForCompletion();
                    if (handle.Status == AsyncOperationStatus.Failed)
                    {
                        break;
                    }
                    Rebot_Material.mainTexture = tex;
                    Addressables.Release(handle);
                    break;
                default:
                    Debug.Log($"error index {idx}");
                    break;
            }
        }
        else
        {
            index = idx% 10;
            _rebotconfig.EyeColor = index;
            switch (index)
            {
                case 1:
                    RebotEye_Material.mainTextureOffset=new Vector2(0,0);
                    break;
                case 2:
                    RebotEye_Material.mainTextureOffset=new Vector2(0.34f,0);
                    break;
                case 3:
                    RebotEye_Material.mainTextureOffset=new Vector2(0.66f,0);
                    break;
                case 4:
                    RebotEye_Material.mainTextureOffset=new Vector2(0.33f,0.66f);
                    break;
                case 5:
                    RebotEye_Material.mainTextureOffset=new Vector2(0,0.66f);
                    break;
                default:
                    Debug.Log($"error index {idx}");
                    break;
            }
        }
        Debug.Log($"change to {idx}!");
    }

    private void OnClickApply()
    {
        _rebotconfig.playername = NameInputField.text;
    }

    private void OnClickRandom()
    {
        int rand = Random.Range(1, 5);
        OnClickColorChange(rand);
        rand = Random.Range(1, 6);
        OnClickColorChange(10+rand);
    }
    protected override void OnInit()
    {
        ApplyBtn = uiGameObject.transform.Find("CreatWindow/ApplyBtn").GetComponent<Button>();
        ApplyBtn.onClick.RemoveAllListeners();
        ApplyBtn.onClick.AddListener(OnClickApply);
        RandomBtn = uiGameObject.transform.Find("CreatWindow/RandomBtn").GetComponent<Button>();
        RandomBtn.onClick.RemoveAllListeners();
        RandomBtn.onClick.AddListener(OnClickRandom);
        NameInputField = uiGameObject.transform.Find("CreatWindow/NameBar/NameInputField").GetComponent<TMP_InputField>();

        ColorItemRed = uiGameObject.transform.Find("CreatWindow/ColorBar/ColorItemRed/Button").GetComponent<Button>();
        ColorItemRed.onClick.RemoveAllListeners();
        ColorItemRed.onClick.AddListener(() =>  OnClickColorChange(1) );

        ColorItemBlue = uiGameObject.transform.Find("CreatWindow/ColorBar/ColorItemBlue/Button").GetComponent<Button>();
        ColorItemBlue.onClick.RemoveAllListeners();
        ColorItemBlue.onClick.AddListener(() => OnClickColorChange(2));

        ColorItemYellow = uiGameObject.transform.Find("CreatWindow/ColorBar/ColorItemYellow/Button").GetComponent<Button>();
        ColorItemYellow.onClick.RemoveAllListeners();
        ColorItemYellow.onClick.AddListener(() => OnClickColorChange(3));

        ColorItemBlack = uiGameObject.transform.Find("CreatWindow/ColorBar/ColorItemBlack/Button").GetComponent<Button>();
        ColorItemBlack.onClick.RemoveAllListeners();
        ColorItemBlack.onClick.AddListener(() => OnClickColorChange(4));

        EyeItem1 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem1/Button").GetComponent<Button>();
        EyeItem1.onClick.RemoveAllListeners();
        EyeItem1.onClick.AddListener(() => OnClickColorChange(11))
            ;
        EyeItem2 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem2/Button").GetComponent<Button>();
        EyeItem2.onClick.RemoveAllListeners();
        EyeItem2.onClick.AddListener(() => OnClickColorChange(12));
        
        EyeItem3 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem3/Button").GetComponent<Button>();
        EyeItem3.onClick.RemoveAllListeners();
        EyeItem3.onClick.AddListener(() => OnClickColorChange(13));
        
        EyeItem4 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem4/Button").GetComponent<Button>();
        EyeItem4.onClick.RemoveAllListeners();
        EyeItem4.onClick.AddListener(() => OnClickColorChange(14));
        
        EyeItem5 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem5/Button").GetComponent<Button>();
        EyeItem5.onClick.RemoveAllListeners();
        EyeItem5.onClick.AddListener(() => OnClickColorChange(15));
        
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