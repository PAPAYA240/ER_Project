using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UI_SkillBase;

public class UI_PlayerInterface : Monobehaviour
{

    enum Texts { DeathTimerText }

    enum Images 
    {
        ProfileImage
    }

    public enum GameObjects
    {
        QSkill,
        WSkill,
        ESkill,
        RSkill,
        TSkill,
        DSkill,
        FSkill,
        HpBar,
        StaminaBar,
        Death,
        LevelAndExp,
        Credit,
        Equipment,
        Inventory,
        Stat,
        ChargingBar
    }

    //TODO 디파인으로 각 요소들을 관리?
    public string CharacterName { get; set; } = "Hyunwoo";
    public int SkinNumber { get; set; } = 0;
    public string CharacterCode { get; set; } = "007";
    public string WeaponCode { get; set; } = "081";
    public string TacticalCode { get; set; } = "4000000";

    public Dictionary<KeyCode, bool> IsActiveKey { get; set; } = new Dictionary<KeyCode, bool>();
    public Action<SkillEnum> OnCharSkillLevelUpAction = null;

    int _remainSkillPoint = 1; //이건 QWERT에만 적용되야함.
    
    bool _isDead = false;
    float _respawnCool = 0.0f;

    public UI_Death DeathUI;
    public event Action<int> OnSecondsChanged;     // 초가 바뀔 때 UI에게 알려주는 이벤트
    private int _lastNotifiedSeconds = -1;

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        LoadProfile(CharacterName, SkinNumber);
        LoadQWERTImage(CharacterCode);
        LoadWeaponSkillImage(WeaponCode);
        LoadTacticalSkillImage(TacticalCode);

        GetObject((int)GameObjects.QSkill).GetComponent<UI_SkillBase>().SkillKeyCode = UI_SkillBase.SkillEnum.Q;
        GetObject((int)GameObjects.WSkill).GetComponent<UI_SkillBase>().SkillKeyCode = UI_SkillBase.SkillEnum.W;
        GetObject((int)GameObjects.ESkill).GetComponent<UI_SkillBase>().SkillKeyCode = UI_SkillBase.SkillEnum.E;
        GetObject((int)GameObjects.RSkill).GetComponent<UI_SkillBase>().SkillKeyCode = UI_SkillBase.SkillEnum.R;
        GetObject((int)GameObjects.TSkill).GetComponent<UI_SkillBase>().SkillKeyCode = UI_SkillBase.SkillEnum.T;
        GetObject((int)GameObjects.DSkill).GetComponent<UI_SkillBase>().SkillKeyCode = UI_SkillBase.SkillEnum.D;
        GetObject((int)GameObjects.FSkill).GetComponent<UI_SkillBase>().SkillKeyCode = UI_SkillBase.SkillEnum.F;

        GetObject((int)GameObjects.QSkill).GetComponent<UI_SkillBase>().InitPopupUI();
        GetObject((int)GameObjects.WSkill).GetComponent<UI_SkillBase>().InitPopupUI();
        GetObject((int)GameObjects.ESkill).GetComponent<UI_SkillBase>().InitPopupUI();
        GetObject((int)GameObjects.RSkill).GetComponent<UI_SkillBase>().InitPopupUI();
        GetObject((int)GameObjects.TSkill).GetComponent<UI_SkillBase>().InitPopupUI();
        GetObject((int)GameObjects.DSkill).GetComponent<UI_SkillBase>().InitPopupUI();
        GetObject((int)GameObjects.FSkill).GetComponent<UI_SkillBase>().InitPopupUI();

        GetObject((int)GameObjects.Death).SetActive(false);
        //GetObject((int)GameObjects.LevelAndExp).GetComponent<UI_Level>().OnLevelUp += OnLevelUp;
        GetObject((int)GameObjects.QSkill).GetComponent<UI_SkillBase>().OnLevelUp += OnCharSkillLevelUp;
        GetObject((int)GameObjects.WSkill).GetComponent<UI_SkillBase>().OnLevelUp += OnCharSkillLevelUp;
        GetObject((int)GameObjects.ESkill).GetComponent<UI_SkillBase>().OnLevelUp += OnCharSkillLevelUp;
        GetObject((int)GameObjects.RSkill).GetComponent<UI_SkillBase>().OnLevelUp += OnCharSkillLevelUp;
        GetObject((int)GameObjects.TSkill).GetComponent<UI_SkillBase>().OnLevelUp += OnCharSkillLevelUp;
        GetObject((int)GameObjects.FSkill).GetComponent<UI_SkillBase>().OnLevelUp += OnTacticalSkillLevelUp;
        
        //ChargingBar
        GetObject((int)GameObjects.ChargingBar).SetActive(false);

        //Stat
        UpdateStat();

        //temp
        OnLevelUp(1);
        SpecificSkillLevelUp(GameObjects.DSkill);
        SpecificSkillLevelUp(GameObjects.FSkill);

        //equip
        //Equip(DataManager.ItemDict[116405] as EquipItemInfo);
        //Equip(DataManager.ItemDict[201414] as EquipItemInfo);
        //Equip(DataManager.ItemDict[202418] as EquipItemInfo);
        //Equip(DataManager.ItemDict[203405] as EquipItemInfo);
        //Equip(DataManager.ItemDict[204418] as EquipItemInfo);

    }
    private void Start()
    {
        
    }

    void Update()
    {
        //EarnExp(1);

        #region Dead
        if (_isDead)
        {
            _respawnCool = Mathf.Max(0, _respawnCool - Time.deltaTime);
            //GetText((int)Texts.DeathTimerText).text = _respawnCool.ToString("F0");
            //
            //// 초 단위가 바뀔 때만 이벤트 호출
            //int seconds = Mathf.CeilToInt(_respawnCool);

            int seconds = Mathf.CeilToInt(_respawnCool);

            // 2) 텍스트도 이 값으로 표시
            GetText((int)Texts.DeathTimerText).text = seconds.ToString();
            if (seconds != _lastNotifiedSeconds) 
            {
                _lastNotifiedSeconds = seconds;
                OnSecondsChanged?.Invoke(seconds);
            }

            if (_respawnCool <= Mathf.Epsilon)
            {
                //TODO 부활
                GetObject((int)GameObjects.Death).SetActive(false);
                DeathUI.Hide();
            }
        }
        #endregion
    }


    #region ImageSetting
    public void LoadProfile(string characterName, int skinNumber)
    {
        string path = "Sprite/CharProfile_" + characterName + "_S" + skinNumber.ToString("D3");
        Sprite sprite = Managers.Resource.Load<Sprite>(path);
        if (sprite == null)
            return;

        GetImage((int)Images.ProfileImage).sprite = sprite;
    }

    public void LoadQWERTImage(string characterCode)
    {
        string path = "";
        path = "Sprite/SkillIcon_1" + characterCode;

        SetSkillImgSet(GameObjects.QSkill, path + "200");
        SetSkillImgSet(GameObjects.WSkill, path + "300");
        SetSkillImgSet(GameObjects.ESkill, path + "400");
        SetSkillImgSet(GameObjects.RSkill, path + "500");
        SetSkillImgSet(GameObjects.TSkill, path + "100");
    }
    public void LoadWeaponSkillImage(string weaponCode)
    {
        SetSkillImgSet(GameObjects.DSkill, "Sprite/WSkillIcon_" + weaponCode);
    }
    public void LoadTacticalSkillImage(string tacticalCode)
    {
        SetSkillImgSet(GameObjects.FSkill, "Sprite/VSkillIcon_" + tacticalCode);
    }

    public void SetSkillImgSet(GameObjects skill, string path)
    {
        switch (skill)
        {
            case GameObjects.QSkill:
            case GameObjects.WSkill:
            case GameObjects.ESkill:
            case GameObjects.RSkill:
            case GameObjects.TSkill:
            case GameObjects.DSkill:
            case GameObjects.FSkill:
                {
                    GameObject go = GetObject((int)skill);
                    if (go == null)
                        return;

                    UI_SkillBase ui_skill = go.GetComponent<UI_SkillBase>();
                    if(ui_skill != null)
                        ui_skill.SetImage(path);
                }
                break;
        }
    }

    #endregion

    #region Skill

    //QWERT 스킬 포인트를 사용해서 스킬 레벨올리는 함수
    public void OnCharSkillLevelUp(SkillEnum skillEnum)
    {
        --_remainSkillPoint;
        OnCharSkillLevelUpAction?.Invoke(skillEnum);

        if (_remainSkillPoint == 0)
        {
            ActivateSkillLevelUpButton(GameObjects.QSkill, false);
            ActivateSkillLevelUpButton(GameObjects.WSkill, false);
            ActivateSkillLevelUpButton(GameObjects.ESkill, false);
            ActivateSkillLevelUpButton(GameObjects.RSkill, false);
            ActivateSkillLevelUpButton(GameObjects.TSkill, false);
        }
        else
        {
            ActivateSkillLevelUpButton(GameObjects.QSkill, true);
            ActivateSkillLevelUpButton(GameObjects.WSkill, true);
            ActivateSkillLevelUpButton(GameObjects.ESkill, true);
            ActivateSkillLevelUpButton(GameObjects.RSkill, true);
            ActivateSkillLevelUpButton(GameObjects.TSkill, true);
        }
    }

    //전술 스킬 올리는 함수. 이건 전술 강화모듈 같은 특정 조건이 있으면 호출할 예정.
    void OnTacticalSkillLevelUp(SkillEnum skillEnum)
    {
        ActivateSkillLevelUpButton(GameObjects.FSkill, false);
    }

    //스킬포인트를 사용하지 않고 스킬 레벨 올리는 함수. 무기 스킬 레벨 올릴 때 이걸로 올려야할듯
    public void SpecificSkillLevelUp(GameObjects objEnum)
    {
        switch (objEnum)
        {
            case GameObjects.QSkill:
            case GameObjects.WSkill:
            case GameObjects.ESkill:
            case GameObjects.RSkill:
            case GameObjects.TSkill:
            case GameObjects.DSkill:
            case GameObjects.FSkill:
                GetObject((int)objEnum).GetComponent<UI_SkillBase>().SkillLevelUp();
                break;
        }
    }

    public void SpecificSkillLevelUp(KeyCode key)
    {
        GameObjects objEnum = GameObjects.Credit;//음 초기화가 어렵네

        switch (key)
        {
            case KeyCode.Q:
                objEnum = GameObjects.QSkill;
                break;
            case KeyCode.W:
                objEnum = GameObjects.WSkill;
                break;
            case KeyCode.E:
                objEnum = GameObjects.ESkill;
                break;
            case KeyCode.R:
                objEnum = GameObjects.RSkill;
                break;
            case KeyCode.T:
                objEnum = GameObjects.TSkill;
                break;
            case KeyCode.D:
                objEnum = GameObjects.DSkill;
                break;
            case KeyCode.F:
                objEnum = GameObjects.FSkill;
                break;
        }

        //잘 못 들어온 정보
        if (objEnum == GameObjects.Credit)
            return;

        switch (objEnum)
        {
            case GameObjects.QSkill:
            case GameObjects.WSkill:
            case GameObjects.ESkill:
            case GameObjects.RSkill:
            case GameObjects.TSkill:
            case GameObjects.DSkill:
            case GameObjects.FSkill:
                GetObject((int)objEnum).GetComponent<UI_SkillBase>().SkillLevelUp();
                break;
        }
    }

    void ActivateSkillLevelUpButton(GameObjects objEnum, bool activate)
    {
        GetObject((int)objEnum).GetComponent<UI_SkillBase>().ActivateLevelUp(activate);
    }

    public void SetSkillStaminaCost(GameObjects objEnum, int value)
    {
        GetObject((int)objEnum).GetComponent<UI_SkillBase>().SetStaminaCost(value);
    }
    public void SetSkillMaxCool(GameObjects objEnum, float value)
    {
        GetObject((int)objEnum).GetComponent<UI_SkillBase>().SetMaxCool(value);
    }

    public void SetSkillCool(GameObjects objEnum, float value)
    {
        GetObject((int)objEnum).GetComponent<UI_SkillBase>().SetCool(value);
    }

    //버튼을 누르거나 컨트롤 스킬 누르면 호출되는 함수?
    public void TrySkillLevelUp(KeyCode key)
    {
        if (CanLevelUp() == false)
            return;

        if (!IsActiveKey.ContainsKey(key))
            IsActiveKey.Add(key, true);

        C_SkillLevelUp packet = new C_SkillLevelUp();
        packet.KeyCode = (int)key;

        Managers.Network.Send(packet);
    }

    public void UpdateSkillAccForPopup(int skillAcc)
    {
        GetObject((int)GameObjects.QSkill).GetComponent<UI_SkillBase>().UpdateSkillAcc(skillAcc);
        GetObject((int)GameObjects.WSkill).GetComponent<UI_SkillBase>().UpdateSkillAcc(skillAcc);
        GetObject((int)GameObjects.ESkill).GetComponent<UI_SkillBase>().UpdateSkillAcc(skillAcc);
        GetObject((int)GameObjects.RSkill).GetComponent<UI_SkillBase>().UpdateSkillAcc(skillAcc);
        GetObject((int)GameObjects.TSkill).GetComponent<UI_SkillBase>().UpdateSkillAcc(skillAcc);
    }

    #endregion

    #region HpBar
    public void SetHp(float hp)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBar>().SetHp(hp);
    }
    public float GetHp()
    {
        return GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBar>().GetHp();
    }
    public void SetBarrier(float barrier)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBar>().SetBarrier(barrier);
    }

    public void SetMaxHp(float maxHp)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBar>().SetMaxHp(maxHp);
    }

    public void PlusHp(float value)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBar>().PlusHp(value);
    }
    public void PlusBarrier(float value)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBar>().PlusBarrier(value);
    }
    public void MinusHp(float value)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBar>().MinusHp(value);
    }

    #endregion

    #region StaminaBar
    public void SetStamina(float value)
    {
        GetObject((int)GameObjects.StaminaBar).GetComponent<UI_Bar>().SetValue(value);
    }

    public float GetStamina()
    {
        return GetObject((int)GameObjects.StaminaBar).GetComponent<UI_Bar>().GetValue();
    }

    public void SetMaxStamina(float value)
    {
        GetObject((int)GameObjects.StaminaBar).GetComponent<UI_Bar>().SetMaxValue(value);
    }
    public void PlusStamina(float value)
    {
        GetObject((int)GameObjects.StaminaBar).GetComponent<UI_Bar>().PlusValue(value);
    }
    public void MinusStamina(float value)
    {
        GetObject((int)GameObjects.StaminaBar).GetComponent<UI_Bar>().MinusValue(value);
    }

    #endregion

    #region Death

    public void OnDead(float respawnTime)
    {
        _isDead = true;
        _respawnCool = respawnTime;
        _lastNotifiedSeconds = -1;      
        GetObject((int)GameObjects.Death).SetActive(true);

        DeathUI.Bind(this);
        DeathUI.Show();
    }

    #endregion

    #region Level
    public bool CanLevelUp()
    {
        //return _remainSkillPoint > 0 ? true : false;
        return _remainSkillPoint > 0;
    }

    public void ActivateCombatImg(bool activate)
    {
        //전투중 이미지 표시
        GameObject go = GetObject((int)GameObjects.LevelAndExp);
        if (go == null)
            return;

        UI_Level ui_Level = go.GetComponent<UI_Level>();
        if (ui_Level != null)
            ui_Level.ActivateCombatImg(activate);
    }

    public void OnLevelUp(int levelUpCnt)
    {
        _remainSkillPoint += levelUpCnt;

        //TODO 레벨업 버튼 활성화 조건 : 일반 스킬 / 궁극기 / 패시브 나뉘어야함.
        ///CanSkillLevelUp
        ActivateSkillLevelUpButton(GameObjects.QSkill, true);
        ActivateSkillLevelUpButton(GameObjects.WSkill, true);
        ActivateSkillLevelUpButton(GameObjects.ESkill, true);
        ActivateSkillLevelUpButton(GameObjects.RSkill, true);
        ActivateSkillLevelUpButton(GameObjects.TSkill, true);
    }
    public void SetLevel(int level)
    {
        GetObject((int)GameObjects.LevelAndExp).GetComponent<UI_Level>().CurrentLevel = level;
    }

    public void SetExp(int value)
    {
        GetObject((int)GameObjects.LevelAndExp).GetComponent<UI_Level>().CurrentExp = value;
    }

    public void SetMaxExp(int value)
    {
        GetObject((int)GameObjects.LevelAndExp).GetComponent<UI_Level>().MaxExp = value;
    }
    #endregion

    #region Credit
    public void PlusCredit(int credit)
    {
        GameObject go = GetObject((int)GameObjects.Credit);
        if (go == null)
            return;

        UI_Credit uI_Credit = go.GetComponent<UI_Credit>();
        if (null != uI_Credit)
            uI_Credit.PlusCredit(credit);
    }

    public void MinusCredit(int credit)
    {
        GameObject go = GetObject((int)GameObjects.Credit);
        if (go == null)
            return;

        UI_Credit uI_Credit = go.GetComponent<UI_Credit>();
        if (null != uI_Credit)
            uI_Credit.MinusCredit(credit);
    }

    public void UseCredit(int credit)
    {
        GameObject go = GetObject((int)GameObjects.Credit);
        if (go == null)
            return;

        UI_Credit uI_Credit = go.GetComponent<UI_Credit>();
        if (null != uI_Credit)
            uI_Credit.UseCredit(credit);
    }
    #endregion

    public void Equip(EquipItemInfo item)
    {
        GetObject((int)GameObjects.Equipment).GetComponent<UI_Equipment>().Equip(item);
    }

    public void SetInventoryItem(ItemInfoBase item, int idx)
    {
        GetObject((int)GameObjects.Inventory).GetComponent<UI_Inventory>().SetItem(item, idx);
    }

    #region Stat

    public void UpdateStat()
    {
        UI_Stat stat = GetObject((int)GameObjects.Stat).GetComponent<UI_Stat>();
        if (null == stat) return;

        MyPlayerController mpc = Managers.Object.MyPlayer;

        //Basic
        stat.SetText(UI_Stat.Texts.AttackText, mpc.Attack.ToString("F0"));
        stat.SetText(UI_Stat.Texts.AttackAmpText, "0"); // TODO 무기에서 가져와야할듯?
        stat.SetText(UI_Stat.Texts.AttackSpeedText, mpc.AttackSpeed.ToString("F2"));
        stat.SetText(UI_Stat.Texts.CriticalRatioText, (mpc.CriticalRatio * 100f).ToString("F0") + "%");
        stat.SetText(UI_Stat.Texts.SkillAmpText, mpc.SkillAmplification.ToString("F0"));
        stat.SetText(UI_Stat.Texts.DefenseText, mpc.Defense.ToString("F0"));
        stat.SetText(UI_Stat.Texts.SkillAccText, mpc.ItemStat.SkillAcceleration.ToString("F0"));
        stat.SetText(UI_Stat.Texts.SpeedText, mpc.Speed.ToString("F2"));
        //Extra
        stat.SetText(UI_Stat.Texts.HpText, (mpc.ItemStat.MaxHp + mpc.ItemStat.MaxHpPerLevel * mpc.Stat.Level).ToString("F0"));
        stat.SetText(UI_Stat.Texts.StaminaText, mpc.ItemStat.MaxStamina.ToString("F0"));
        stat.SetText(UI_Stat.Texts.VisionText, mpc.ItemStat.Vision.ToString("F2")); // TODO
        stat.SetText(UI_Stat.Texts.AttackRangeText, mpc.AttackRange.ToString("F2")); //TODO
        stat.SetText(UI_Stat.Texts.CCResistanceText, (mpc.ItemStat.CCResistance * 100).ToString("F0") + "%");
        stat.SetText(UI_Stat.Texts.PenetrationText, $"{mpc.FixedDefensePenetration.ToString("F0")} | {(mpc.PercentageDefensePenetration * 100).ToString("F0")}%");
        stat.SetText(UI_Stat.Texts.LifeStealText,$"{mpc.ItemStat.LifeSteal.ToString("F0")}% | {mpc.ItemStat.Omnivamp.ToString("F0")}%");
    }

    #endregion

    #region ChargingBar

    private Coroutine _coChargingBar = null;

    public void SetChargingBar(string skillName, float fullChargingTime, float maxChargingTime)
    {
        GameObject chargingBar = GetObject((int)GameObjects.ChargingBar);
        if (chargingBar != null)
        {
            chargingBar.SetActive(true);

            UI_ChargingBar ui = chargingBar.GetComponent<UI_ChargingBar>();
            if (ui != null)
            {
                ui.SetChargingBar(skillName, fullChargingTime, maxChargingTime);
            }

            if (_coChargingBar != null)
            {
                StopCoroutine(_coChargingBar);
                _coChargingBar = null;
            }

            _coChargingBar = StartCoroutine(CoChargingBar(maxChargingTime));
        }
    }
    public void SetMaintainChargingBar(string skillName, float fullChargingTime, float maxChargingTime)
    {
        GameObject chargingBar = GetObject((int)GameObjects.ChargingBar);
        if (chargingBar != null)
        {
            chargingBar.SetActive(true);

            UI_ChargingBar ui = chargingBar.GetComponent<UI_ChargingBar>();
            if (ui != null)
            {
                ui.SetChargingBar(skillName, fullChargingTime, maxChargingTime);
            }
        }
    }
    public void StopChargingBar()
    {
        GameObject chargingBar = GetObject((int)GameObjects.ChargingBar);
        if (chargingBar != null)
        {

            UI_ChargingBar ui = chargingBar.GetComponent<UI_ChargingBar>();
            if (ui != null)
            {
                ui.Stop();
            }

            if (_coChargingBar != null)
            {
                StopCoroutine(_coChargingBar);
                _coChargingBar = null;
            }

            chargingBar.SetActive(false);
        }
    }

    private IEnumerator CoChargingBar(float maxChargingTime)
    {
        yield return new WaitForSeconds(maxChargingTime);
        StopChargingBar();
    }

    #endregion

}
