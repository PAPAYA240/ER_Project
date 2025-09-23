using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_CommonSkill : UI_SkillBase
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
        Level_1,
        Level_2,
        Level_3,
        Level_4,
        Level_5,
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
    float _remainCool = 0;
    float _maxCool = 10.0f;

    public override void Init()
    {

        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        BindEvent(gameObject, OnMouseOverEvent, Define.UIEvent.PointerEnter);
        BindEvent(gameObject, OnMouseExitEvent, Define.UIEvent.PointerExit);

        ui_PlayerInterface = GetComponentInParent<UI_PlayerInterface>();
        if (ui_PlayerInterface == null)
            Debug.Log("null  == ui_PlayerInterface");

        //temp
        SetStaminaCost(50);
        GetObject(_stamina).gameObject.SetActive(false);

        GetText((int)Texts.CooldownTimerText).text = "";
        ActivateLevelUp(activate: false);

        SetSkillLevel(_skillLevel);
    }

    void Update()
    {
        if (_skillLevel == 0)
            return;
        //temp

        if (GetObject(_cooldownTimer).activeSelf && _remainCool > 0.0f)
        {
            _remainCool = Math.Max(0.0f, _remainCool - Time.deltaTime);

            if( _remainCool > 0.0f )
            {

                GetImage((int)Images.CooldownFill).fillAmount = _remainCool / _maxCool;
                SetCoolDown(_remainCool);
            }
            else
            {
                GetObject(_cooldownTimer).SetActive(false);
            }
        }

        if (null != ui_PlayerInterface)
        {
            if (IsEnoughStamina(ui_PlayerInterface.GetStamina())) 
                ActivateStamina(false);
            else 
                ActivateStamina(true);
        }
    }

    void SetSkillLevel(int level)
    {
        //TODO 
        if (level < 0 || level > _maxSkillLevel)
            return;
        if (level == 1)
            GetObject(_cooldownTimer).gameObject.SetActive(false);

        _skillLevel = level;

        for(int i = 1; i <= _skillLevel; ++i) 
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

    void ChangeColor(int level, ColorEnum color)
    {
        Color destColor = Color.black;

        if(color == ColorEnum.Yellow)
        {
            if (!ColorUtility.TryParseHtmlString(_yellow, out destColor))
            {
                Debug.Log($"Failed to TryParseHtmlString : {_yellow}");
                return;
            }
        }
        else if(color == ColorEnum.Gray)
        {
            if (!ColorUtility.TryParseHtmlString(_gray, out destColor))
            {
                Debug.Log($"Failed to TryParseHtmlString : {_gray}");
                return;
            }
        }

        GetImage(level - 1).color = destColor;
    }

    public override void UseSkill()
    {
        GetObject(_cooldownTimer).SetActive(true);
        //temp
        _remainCool = _maxCool;
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
    public override void ActivateLevelUp(bool activate)
    {
        if(activate == false)
        {
            GetObject((int)GameObjects.LevelUp).SetActive(activate);
            return;
        }

        MyPlayerController mpc = Managers.Object.MyPlayer;

        if (mpc == null)
            return;

        int playerLevel = mpc.ObjInfo.StatInfo.Level;

        //플레이어 레벨 별 스킬 레벨 제한 
        switch (_skillLevel)
        {
            case 0:
                {
                    if(playerLevel >= 1)
                    {
                        GetObject((int)GameObjects.LevelUp).SetActive(activate);
                        return;
                    }
                }
                break;
            case 1:
                {
                    if (playerLevel >= 3)
                    {
                        GetObject((int)GameObjects.LevelUp).SetActive(activate);
                        return;    
                    }
                }
                break;
            case 2:
                {
                    if (playerLevel >= 5)
                    {
                        GetObject((int)GameObjects.LevelUp).SetActive(activate);
                        return;    
                    }
                }
                break;
            case 3:
                {
                    if (playerLevel >= 7)
                    {
                        GetObject((int)GameObjects.LevelUp).SetActive(activate);
                        return;
                    }
                }
                break;
            case 4:
                {
                    if (playerLevel >= 9)
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
        if(sprite == null)
        {
            Debug.Log($"null : {path}");
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
