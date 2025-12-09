using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_UltimateSkill : UI_SkillBase
{
    enum Buttons
    {
        SkillButton
    }

    enum Texts
    {
        StaminaCost,
        CooldownTimerText,
        MapingKey
    }

    enum Images
    {
        // �߰� �ҰŸ� �����θ� ���� ������ �߰����� �� ��. �̹��� �� �� �ٲ�.
        Level_1,
        Level_2,
        Level_3,
        CooldownFill
    }

    enum GameObjects
    {
        Stamina,
        CooldownTimer,
        LevelUp
    }

    enum ColorEnum
    {
        Yellow,
        Gray
    }

    const int _cooldownTimer = (int)GameObjects.CooldownTimer;
    const int _stamina = (int)GameObjects.Stamina;

    int _staminaCost = 0;

    UI_PlayerInterface ui_PlayerInterface = null;

    //Temp
    float _maxCool = 10.0f;

    public override void Init()
    {
        _maxSkillLevel = 3;

        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        BindEvent(gameObject, OnMouseOverEvent, Define.UIEvent.PointerEnter);
        BindEvent(gameObject, OnMouseExitEvent, Define.UIEvent.PointerExit);

        ui_PlayerInterface = GetComponentInParent<UI_PlayerInterface>();
        if (ui_PlayerInterface == null)
        {
            //Debug.Log("null  == ui_PlayerInterface");
        }

        GetObject(_stamina).gameObject.SetActive(false);
        //GetObject(_cooldownTimer).gameObject.SetActive(false);

        //temp
        //SetStaminaCost(100);

        GetText((int)Texts.CooldownTimerText).text = "";
        ActivateLevelUp(false);

        SetSkillLevel(_skillLevel);
    }

    void Update()
    {
        if (_skillLevel == 0)
            return;

        if(null != ui_PlayerInterface)
        {
            if(IsEnoughStamina(ui_PlayerInterface.GetStamina())) //스테미너가 충분
                ActivateStamina(false);
            else //스테미너가 부족
                ActivateStamina(true);
        }
    }

    void SetSkillLevel(int level)
    {
        //TODO 이렇게 체크해야하나?
        if (level < 0 || level > _maxSkillLevel)
            return;
        if (level == 1)
            GetObject(_cooldownTimer).gameObject.SetActive(false);

        _skillLevel = level;

        for (int i = 1; i <= _skillLevel; ++i)
            ChangeColor(i, ColorEnum.Yellow);

        for (int i = _skillLevel + 1; i <= _maxSkillLevel; ++i)
            ChangeColor(i, ColorEnum.Gray);
    }


    public override void SkillLevelUp()
    {
        if (_skillLevel == _maxSkillLevel)
            return;

        int newLevel = _skillLevel + 1;

        SetSkillLevel(newLevel);
        _popupUi.CurSkillLevel = newLevel;

        OnLevelUp?.Invoke(SkillKeyCode);
    }

    public override void LevelUpButtonClicked()
    {
        if (ui_PlayerInterface != null)
        {
            ui_PlayerInterface.TrySkillLevelUp(SkillEnumToKeyCode(SkillKeyCode));
        }
    }

    void ChangeColor(int level, ColorEnum color)
    {
        Color destColor = Color.black;

        if (color == ColorEnum.Yellow)
        {
            if (!ColorUtility.TryParseHtmlString(_yellow, out destColor))
            {
                //Debug.Log($"Failed to TryParseHtmlString : {_yellow}");
                return;
            }
        }
        else if (color == ColorEnum.Gray)
        {
            if (!ColorUtility.TryParseHtmlString(_gray, out destColor))
            {
                //Debug.Log($"Failed to TryParseHtmlString : {_gray}");
                return;
            }
        }

        GetImage(level - 1).color = destColor;
    }

    public override void SetCool(float value)
    {
        if (_skillLevel == 0)
            return;

        if (value > 0)
        {
            GetObject(_cooldownTimer).SetActive(true);
            GetImage((int)Images.CooldownFill).fillAmount = value / _maxCool;
            SetCoolDown(value);
        }
        else
        {
            GetObject(_cooldownTimer).SetActive(false);
        }
    }

    void SetCoolDown(float remainCooldown)
    {
        string text = "";

        if (remainCooldown > 1.0f)
        {
            text = remainCooldown.ToString("F0");
        }
        else
        {
            text = remainCooldown.ToString("F1");
        }

        TextMeshProUGUI textObject = GetText((int)Texts.CooldownTimerText);
        if (textObject != null)
            textObject.text = text;
    }

    //스킬의 레벨업 버튼을 활성화/비활성화 시키는 함수
    public override void ActivateLevelUp(bool activate)
    {
        if (activate == false)
        {
            GetObject((int)GameObjects.LevelUp).SetActive(activate);
            return;
        }

        MyPlayerController mpc = Managers.Object.MyPlayer;

        if (mpc == null)
            return;

        int playerLevel = mpc.ObjInfo.StatInfo.Level;

        switch (_skillLevel)
        {
            case 0:
                {
                    if (playerLevel >= 6)
                    {
                        GetObject((int)GameObjects.LevelUp).SetActive(activate);
                        return;
                    }
                }
                break;
            case 1:
                {
                    if (playerLevel >= 11)
                    {
                        GetObject((int)GameObjects.LevelUp).SetActive(activate);
                        return;
                    }
                }
                break;
            case 2:
                {
                    if (playerLevel >= 16)
                    {
                        GetObject((int)GameObjects.LevelUp).SetActive(activate);
                        return;
                    }
                }
                break;
        }
        GetObject((int)GameObjects.LevelUp).SetActive(false);
    }
    public void ActivateStamina(bool activate)
    {
        GetObject(_stamina).SetActive(activate);
    }

    public override void SetImage(string path)
    {
        Sprite sprite = Managers.Resource.Load<Sprite>(path);
        if (sprite == null)
        {
            //Debug.Log($"null : {path}");
            return;
        }
        GetButton((int)Buttons.SkillButton).image.sprite = sprite;
    }
    public override void SetStaminaCost(int value)
    {
        _staminaCost = value;
        GetText((int)Texts.StaminaCost).text = _staminaCost.ToString();
    }

    public override void SetMaxCool(float value)
    {
        _maxCool = value;
    }

    public override bool IsEnoughStamina(float curStamina)
    {
        return curStamina > _staminaCost ? true : false;
    }
}

