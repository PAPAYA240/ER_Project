using Data;
using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static PlayerSkillController;
using static UI_SkillBase;
using static UI_PlayerInterface;

public class PlayerUIController : MonoBehaviour
{
    private MyPlayerController _player;
    private UI_PlayerInterface _UI;
    private PlayerSkillController _skill;

    public UI_PlayerHUD PlayerHUD;
    public UI_PlayerInterface PlayerInterface { get; protected set; }

    List<ItemInfoBase> _inventory = new List<ItemInfoBase>();
    const int _maxInventorySlot = 10;

    private Dictionary<KeyCode, SkillBase> _skillDict = new Dictionary<KeyCode, SkillBase>();
    private Dictionary<KeyCode, CoolTime> _coolDownDict = new Dictionary<KeyCode, CoolTime>();

    private void Awake()
    {
        _player = GetComponentInChildren<MyPlayerController>();
        _skill = GetComponentInChildren<PlayerSkillController>();
    }

    public void Init()
    {
        _skillDict = _skill.SkillDict;
        _coolDownDict = _skill.CoolDownDict;

        //UI
        GameObject go = Managers.Resource.Instantiate("UI/Scene/PlayerHUD");
        go.transform.SetParent(gameObject.transform);
        PlayerHUD = go.GetComponent<UI_PlayerHUD>();
        PlayerHUD.Init();

        PlayerInterface = go.GetComponentInChildren<UI_PlayerInterface>();
        PlayerInterface.CharacterCode = CharTypeToCharCode(_player.ObjInfo.Player.CharType);
        PlayerInterface.CharacterName = Enum.GetName(typeof(CharacterType), _player.ObjInfo.Player.CharType);
        PlayerInterface.WeaponCode = CharTypeToWeaponCode(_player.ObjInfo.Player.CharType);
        PlayerInterface.Init();
        PlayerInterface.OnCharSkillLevelUpAction += OnCharSkillLevelUp;

        UI_Minimap minimap = _player.GetComponentInChildren<UI_Minimap>();
        minimap.ActivatePlayerIcon(UI_MinimapCharIcon.IconType.MyPlayer, _player);

        //쿨타임 설정
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, FindSkill(KeyCode.Q).MaxCooldown);

        //업데이트 함수들 호출
        _player.Stat = _player.Stat;

        _player.NameTag.GetComponentInChildren<UI_PlayerNameTag>().SetHPColor();

        MakeInventory();
    }

    public void Update()
    {
        UpdateCool();
    }

    private UI_PlayerInterface.GameObjects KeyToUIEnum(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Q:
                return UI_PlayerInterface.GameObjects.QSkill;
            case KeyCode.W:
                return UI_PlayerInterface.GameObjects.WSkill;
            case KeyCode.E:
                return UI_PlayerInterface.GameObjects.ESkill;
            case KeyCode.R:
                return UI_PlayerInterface.GameObjects.RSkill;
            case KeyCode.D:
                return UI_PlayerInterface.GameObjects.DSkill;
            case KeyCode.F:
                return UI_PlayerInterface.GameObjects.FSkill;
        }

        return UI_PlayerInterface.GameObjects.TSkill;
    }

    private string CharTypeToCharCode(CharacterType type)
    {
        string result = "";

        switch (type)
        {
            case CharacterType.Rozzi:
                result = "021";
                break;
            case CharacterType.Yuki:
                result = "011";
                break;
            case CharacterType.Hyunwoo:
                result = "007";
                break;
            case CharacterType.Abigail:
                result = "067";
                break;
            case CharacterType.Theodore:
                result = "062";
                break;
        }

        return result;
    }

    private string CharTypeToWeaponCode(CharacterType type)
    {
        string result = "";

        switch (type)
        {
            case CharacterType.Rozzi:
                result = "051";
                break;
            case CharacterType.Yuki:
                result = "021";
                break;
            case CharacterType.Hyunwoo:
                result = "081";
                break;
            case CharacterType.Abigail:
                result = "031";
                break;
            case CharacterType.Theodore:
                result = "071";
                break;
        }

        return result;
    }

    private void SetMaxCoolDownUI(UI_PlayerInterface.GameObjects skillEnum, float value)
    {
        PlayerInterface.SetSkillMaxCool(skillEnum, value);
    }

    private void UpdateSkillMaxCool()
    {
        // TODO 현재 스킬레벨에 따른 쿨타임과 아이템으로 인한 스킬 가속을 적용하여 UI에 반영
        // 일단 스킬 가속에 대한 계산이 어떻게 되는지 알아야하고, 스킬들이 레벨마다 어떤 쿨타임을 가질지 데이터(Json)를 만들어줘야함.

        //temp 나중에 스탯에서 가져오든가 해야될듯
        SkillBase QSkill = FindSkill(KeyCode.Q);
        SkillBase WSkill = FindSkill(KeyCode.W);
        SkillBase ESkill = FindSkill(KeyCode.E);
        SkillBase RSkill = FindSkill(KeyCode.R);

        float skillAcc = 0.0f;
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, CalculateMaxCool(QSkill.CurLevelCooldown, skillAcc));
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.WSkill, CalculateMaxCool(WSkill.CurLevelCooldown, skillAcc));
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.ESkill, CalculateMaxCool(ESkill.CurLevelCooldown, skillAcc));
        SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.RSkill, CalculateMaxCool(RSkill.CurLevelCooldown, skillAcc));
    }

    private float CalculateMaxCool(float cooldown, float skillAcc)
    {
        // 최종 쿨타임 = 기본 쿨타임 × (100 / (100 + 스킬가속))
        return cooldown * (100f / (100f + skillAcc));
    }

    protected void OnCharSkillLevelUp(SkillEnum skill)
    {
        //For QWERT
        KeyCode key = (KeyCode)System.Enum.Parse(typeof(KeyCode), skill.ToString());
        _skillDict[key].CurLevel += 1;

        float skillAcc = 0.0f;
        //float skillAcc = Stat.GetSkillAcc();

        switch (skill)
        {
            case SkillEnum.Q:
                SkillBase QSkill = FindSkill(KeyCode.Q);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.QSkill, CalculateMaxCool(QSkill.CurLevelCooldown, skillAcc));
                break;
            case SkillEnum.W:
                SkillBase WSkill = FindSkill(KeyCode.W);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.WSkill, CalculateMaxCool(WSkill.CurLevelCooldown, skillAcc));
                break;
            case SkillEnum.E:
                SkillBase ESkill = FindSkill(KeyCode.E);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.ESkill, CalculateMaxCool(ESkill.CurLevelCooldown, skillAcc));
                break;
            case SkillEnum.R:
                SkillBase RSkill = FindSkill(KeyCode.R);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.RSkill, CalculateMaxCool(RSkill.CurLevelCooldown, skillAcc));
                break;
            case SkillEnum.T:
                SkillBase TSkill = FindSkill(KeyCode.T);
                SetMaxCoolDownUI(UI_PlayerInterface.GameObjects.RSkill, CalculateMaxCool(TSkill.CurLevelCooldown, skillAcc));
                break;
        }
    }

    #region Update
    public void UpdateHp()
    {
        if (null == PlayerInterface)
            return;

        PlayerInterface.SetHp(_player.Hp);
    }

    public void UpdateMaxHp()
    {
        if (null == PlayerInterface)
            return;

        PlayerInterface.SetMaxHp(_player.MaxHp);
    }

    public void UpdateStamina()
    {
        if (null == PlayerInterface)
            return;

        PlayerInterface.SetStamina(_player.Stamina);
    }

    public void UpdateMaxStamina()
    {
        if (null == PlayerInterface)
            return;

        PlayerInterface.SetMaxStamina(_player.MaxStamina);
    }

    public void UpdateLevel()
    {
        if (null == PlayerInterface)
            return;

        PlayerInterface.SetLevel(_player.Stat.Level);
        _player.SetNameTagLevel();
    }

    public void UpdateCool()
    {
        if (null == PlayerInterface)
            return;

        PlayerInterface.SetSkillCool(GameObjects.QSkill, _coolDownDict[KeyCode.Q].coolTime);
        PlayerInterface.SetSkillCool(GameObjects.WSkill, _coolDownDict[KeyCode.W].coolTime);
        PlayerInterface.SetSkillCool(GameObjects.ESkill, _coolDownDict[KeyCode.E].coolTime);
        PlayerInterface.SetSkillCool(GameObjects.RSkill, _coolDownDict[KeyCode.R].coolTime);
        PlayerInterface.SetSkillCool(GameObjects.TSkill, _coolDownDict[KeyCode.T].coolTime);
        //PlayerInterface.SetSkillCool(GameObjects.DSkill, );
        //PlayerInterface.SetSkillCool(GameObjects.FSkill, );
    }
    #endregion

    public void SetTimer(int phase, float clientLocalTargetRealtimeSinceStartupEnd)
    {
        PlayerHUD.SetTimer(phase, clientLocalTargetRealtimeSinceStartupEnd);
    }

    public void NotifyKill(PlayerController attPc, PlayerController diePc)
    {
        PlayerHUD.NotifyKill(attPc, diePc);
    }

    #region Inventory
    private void MakeInventory()
    {
        for (int i = 0; i < _maxInventorySlot; ++i)
        {
            _inventory.Add(null); //비어 있는 인벤토리를 생성
        }
    }
    #endregion

    #region Utils
    protected SkillBase FindSkill(KeyCode keyCode)
    {
        SkillBase skillBase = null;

        if (!_skillDict.TryGetValue(keyCode, out skillBase))
        {
            Debug.Log($"Skill을 찾을 수 없음 : {keyCode}");
            return null;
        }

        return skillBase;
    }
    #endregion
}

