using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class UI_CharSkillInfoPopup : UI_Popup
{
    enum Images { SkillImage }

    enum Texts
    {
        SkillName,
        SkillKeyCode,
        SkillStaminaCool,
        SkillDescription
    }

    enum GameObjects { Root, Panel, LevelUpValues }

    public int CurSkillLevel { get { return _curSkillLevel; } 
        set 
        { 
            _curSkillLevel = value;

            SetName(GenerateStrName());
            SetStaminaCool(GenerateStrStaminaCool());
            SetDescription(GenerateDescription());
            UpdateSkillPopupLevelUpValues();
        } 
    }
    int _curSkillLevel = 0; // 0 - 5까지 실제 스킬 레벨
    int _maxSkillLevel = 0;

    int _skillAcc = 0;
    public int SkillAcc { get { return _skillAcc; } set { _skillAcc = value; SetStaminaCool(GenerateStrStaminaCool()); } } 
    SkillData _skilldata;
    List<UI_SkillPopupLevelUpValue> _skillPopupLevelUpValues = new List<UI_SkillPopupLevelUpValue>();

    public override void Init()
    {
        base.Init();

        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));
    }

    private void Awake()
    {
        Init();
    }

    void Start()
    {
        
    }

    void Update()
    {
        UpdateScale();
    }

    public void SetSkill(CharacterType charType, KeyCode code)
    {
        if (DataManager.SkillDict != null &&
            DataManager.SkillDict.ContainsKey(charType) &&
            DataManager.SkillDict[charType].ContainsKey(code))
        {
            _skilldata = DataManager.SkillDict[charType][code];
            _maxSkillLevel = _skilldata.maxLevel;
            SetName(GenerateStrName());
            SetCode(code.ToString());
            SetStaminaCool(GenerateStrStaminaCool());
            SetDescription(GenerateDescription());

            GetImage((int)Images.SkillImage).sprite = Managers.Resource.Load<Sprite>($"Sprite/SkillIcon_1{CharTypeToCode(charType)}{KeyCodeToSkillCode(code)}");

            InitSkillPopupLevelUpValues();
        }
    }

    string CharTypeToCode(CharacterType type)
    {
        string result = "001";
        switch (type)
        {
            case CharacterType.Abigail:
                result = "067";
                break;
            case CharacterType.Yuki:
                result = "011";
                break;
            case CharacterType.Rozzi:
                result = "021";
                break;
            case CharacterType.Hyunwoo:
                result = "007";
                break;
            case CharacterType.Theodore:
                result = "062";
                break;
        }

        return result;
    }

    string KeyCodeToSkillCode(KeyCode key)
    {
        string result = "100";
        switch (key)
        {
            case KeyCode.T:
                result = "100";
                break;
            case KeyCode.Q:
                result = "200";
                break;
            case KeyCode.W:
                result = "300";
                break;
            case KeyCode.E:
                result = "400";
                break;
            case KeyCode.R:
                result = "500";
                break;

        }
        return result;
    }

    public void SetName(string str)
    {
        GetText((int)Texts.SkillName).text = str;
    }

    public void SetCode(string str)
    {
        GetText((int)Texts.SkillKeyCode).text = str;
    }

    public void SetStaminaCool(string str)
    {
        GetText((int)Texts.SkillStaminaCool).text = str;
    }

    public void SetDescription(string str)
    {
        GetText((int)Texts.SkillDescription).text = str;
    }

    public void SetY(float y)
    {
        Vector3 pos = GetObject((int)GameObjects.Root).transform.position;

        Vector2 panelSize = GetObject((int)GameObjects.Panel).GetComponent<RectTransform>().sizeDelta;
        Vector2 levelUpSize = GetObject((int)GameObjects.LevelUpValues).GetComponent<RectTransform>().sizeDelta;

        pos.y = y + panelSize.y + levelUpSize.y;
        GetObject((int)GameObjects.Root).transform.position = pos;
    }

    private string GenerateStrName()
    {
        //스킬 안찍었을 때도 1레벨의 정보를 나타내기 위해 Max함수 사용
        int level = Mathf.Max(CurSkillLevel, 1);
        string result = $"{_skilldata.name} (레벨 {level})";
        return result;
    }
    private string GenerateStrStaminaCool()
    {
        //스킬 안찍었을 때도 1레벨의 정보를 나타내기 위해 Max함수 사용
        float cool = _skilldata.levels[Mathf.Max(CurSkillLevel, 1)].cooldown * (100f / (100f + SkillAcc));

        string result = $"스테미나 {_skilldata.levels[Mathf.Max(CurSkillLevel, 1)].staminaCost}\n" +
            $"쿨다운 {cool.ToString("F1")} 초";
        return result;
    }
    private string GenerateDescription()
    {
        //일단 동적으로 바꾸지 않고 공식을 표시함
        string result = _skilldata.description;

        if (_skilldata.descriptionInfo == null)
            return result;

        foreach(var pair in _skilldata.descriptionInfo)
        {
            //value가 리스트로 들어있어서 0부터 시작, 최대 스킬 레벨을 넘어가서 이상한데 참조하지 않게.
            result = result.Replace($"{pair.Key}", pair.Value[Mathf.Min(Mathf.Max(CurSkillLevel - 1, 0), _maxSkillLevel)]);
        }

        return result;
    }

    //스킬이 레벨업하면 오르는 정보가 표기되는 오브젝트의 초기화 함수
    private void InitSkillPopupLevelUpValues()
    {
        int count = _skilldata.popupInfo.Count;

        for(int i = 0; i < count; ++i)
        {
            GameObject go = Managers.Resource.Instantiate("UI/SubItem/SkillPopupLevelUpValue");
            go.transform.SetParent(GetObject((int)GameObjects.LevelUpValues).transform);
            _skillPopupLevelUpValues.Add(go.GetComponent<UI_SkillPopupLevelUpValue>());
        }

        int index = 0;
        foreach(var pair in _skilldata.popupInfo)
        {
            _skillPopupLevelUpValues[index++].SetKeyText(pair.Key);
        }

        UpdateSkillPopupLevelUpValues();
    }

    private void UpdateSkillPopupLevelUpValues()
    {
        List<string> strings = new List<string>();

        //리스트라 0부터시작
        int index = Mathf.Max(CurSkillLevel - 1, 0);

        foreach (var v in _skilldata.popupInfo )
        {
            string result = "[ ";
            for(int i = 0; i < v.Value.Count; ++i)
            {
                if( i == index)
                    result += $"<color=#FFFFFF>{v.Value[i].ToString()} </color>"; //해당 레벨 색을 바꿔서 강조
                else
                    result += v.Value[i].ToString() + " ";
            }
            result += "]";

            strings.Add(result);
        }

        for(int i = 0; i < strings.Count; ++i)
        {
            _skillPopupLevelUpValues[i].SetValuesText(strings[i]);
        }
    }
}
