using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public UICharacterCreat() : base("UICharacterCreat", EUILayerType.Normal, true) { }

    private void OnClickColorChange(int idx)
    {
        Debug.Log($"change to {idx}!");
    }

    protected override void OnInit()
    {
        ApplyBtn = uiGameObject.transform.Find("CreatWindow/ApplyBtn").GetComponent<Button>();
        RandomBtn = uiGameObject.transform.Find("CreatWindow/RandomBtn").GetComponent<Button>();
        NameInputField = uiGameObject.transform.Find("CreatWindow/NameBar/NameInputField").GetComponent<TMP_InputField>();

        ColorItemRed = uiGameObject.transform.Find("CreatWindow/ColorBar/ColorItemRed").GetComponent<Button>();
        ColorItemRed.onClick.RemoveAllListeners();
        ColorItemRed.onClick.AddListener(() =>  OnClickColorChange(1) );

        ColorItemBlue = uiGameObject.transform.Find("CreatWindow/ColorBar/ColorItemBlue").GetComponent<Button>();
        ColorItemBlue.onClick.RemoveAllListeners();
        ColorItemBlue.onClick.AddListener(() => OnClickColorChange(2));

        ColorItemYellow = uiGameObject.transform.Find("CreatWindow/ColorBar/ColorItemYellow").GetComponent<Button>();
        ColorItemYellow.onClick.RemoveAllListeners();
        ColorItemYellow.onClick.AddListener(() => OnClickColorChange(3));

        ColorItemBlack = uiGameObject.transform.Find("CreatWindow/ColorBar/ColorItemBlack").GetComponent<Button>();
        ColorItemBlack.onClick.RemoveAllListeners();
        ColorItemBlack.onClick.AddListener(() => OnClickColorChange(4));

        EyeItem1 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem1").GetComponent<Button>();
        EyeItem2 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem2").GetComponent<Button>();
        EyeItem3 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem3").GetComponent<Button>();
        EyeItem4 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem4").GetComponent<Button>();
        EyeItem5 = uiGameObject.transform.Find("CreatWindow/EyeBar/EyeItem5").GetComponent<Button>();
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