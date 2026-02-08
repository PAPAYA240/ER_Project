using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class UI_SkillBase : Monobehaviour
{
    public enum SkillEnum { Q,W,E,R,T,D,F,NONE }

    public override void Init()
    {
        
    }

    private void Awake()
    {
        Init();
    }

    private void Start()
    {
        
    }


    public Action<SkillEnum> OnLevelUp = null;

    public SkillEnum SkillKeyCode { get; set; } = SkillEnum.NONE;
    protected int _skillLevel = 0;
    protected int _maxSkillLevel = 5;

    protected const float _height = 170f; //스킬팝업 띄울 높이
    protected const string _yellow = "#B89249";
    protected const string _gray = "#505050";

    protected GameObject _popupGameObject;
    protected UI_CharSkillInfoPopup _popupUi;

    public abstract void SkillLevelUp();
    public abstract void ActivateLevelUp(bool activate);
    public abstract void SetImage(string path);
    public virtual void SetStaminaCost(int value) { }
    public virtual void SetCool(float value) { }
    public virtual void SetMaxCool(float value) { }
    public virtual bool IsEnoughStamina(float curStamina) { return false; }
    public virtual void LevelUpButtonClicked() { }

    public KeyCode SkillEnumToKeyCode(SkillEnum skill)
    {
        KeyCode result = KeyCode.None;

        switch (skill)
        {
            case SkillEnum.Q:
                result = KeyCode.Q;
                break;
            case SkillEnum.W:
                result = KeyCode.W;
                break;
            case SkillEnum.E:
                result = KeyCode.E;
                break;
            case SkillEnum.R:
                result = KeyCode.R;
                break;
            case SkillEnum.T:
                result = KeyCode.T;
                break;
            case SkillEnum.D:
                result = KeyCode.D;
                break;
            case SkillEnum.F:
                result = KeyCode.F;
                break;
        }

        return result;
    }

    public void InitPopupUI()
    {
        _popupGameObject = Managers.Resource.Instantiate("UI/Popup/SkillInfoPopup");
        _popupUi = _popupGameObject.GetComponent<UI_CharSkillInfoPopup>();
        _popupUi.SetSkill(Managers.Object.MyPlayer.ObjInfo.Player.CharType, SkillEnumToKeyCode(SkillKeyCode));
        PopupActivate(false);
    }

    public void UpdateSkillAcc(int skillAcc)
    {
        _popupUi.SkillAcc = skillAcc;
    }

    protected void PopupActivate(bool activate)
    {
        if (null != _popupGameObject)
        {
            _popupGameObject.SetActive(activate);
            //위치 조정
            _popupUi.SetY(_height);
        }
    }

    protected void OnMouseOverEvent(PointerEventData data)
    {
        PopupActivate(true);
    }

    protected void OnMouseExitEvent(PointerEventData data)
    {
        PopupActivate(false);
    }
}
