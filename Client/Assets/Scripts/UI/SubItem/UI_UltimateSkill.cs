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
        // 추가 할거면 밑으로만 위에 무엇을 추가하지 말 것. 이미지 잘 못 바뀜.
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
    float _remainCool = 0;
    float _maxCool = 10.0f;

    public override void Init()
    {
        _maxSkillLevel = 3;

        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        ui_PlayerInterface = GetComponentInParent<UI_PlayerInterface>();
        if (ui_PlayerInterface == null)
            Debug.Log("null  == ui_PlayerInterface");

        GetObject(_stamina).gameObject.SetActive(false);
        //GetObject(_cooldownTimer).gameObject.SetActive(false);

        //temp
        SetStaminaCost(100);

        GetText((int)Texts.CooldownTimerText).text = "";
        ActivateLevelUp(DoYouActivate:false);

        SetSkillLevel(_skillLevel);
    }

    void Update()
    {
        if (_skillLevel == 0)
            return;
        //temp
        //쿨다운타이머가 활성화 되어 있으면 셋 쿨다운을 호출해서 쿨타임을 지속적으로 갱신
        if (GetObject(_cooldownTimer).activeSelf && _remainCool > 0.0f)
        {
            _remainCool = Math.Max(0.0f, _remainCool - Time.deltaTime);

            if (_remainCool > 0.0f)
            {
                // 쿨타임이 남아있을때
                GetImage((int)Images.CooldownFill).fillAmount = _remainCool / _maxCool;
                SetCoolDown(_remainCool);
            }
            else
            {
                GetObject(_cooldownTimer).SetActive(false);
            }
        }

        if(null != ui_PlayerInterface)
        {
            if(IsEnoughStamina(ui_PlayerInterface.GetStamina())) //스테미너가 충분하면
                ActivateStamina(false);
            else //스테미너가 부족하면
                ActivateStamina(true);
        }
        
    }

    void SetSkillLevel(int level)
    {
        //TODO 레벨 체크 이렇게 해야되나
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
        SetSkillLevel(_skillLevel + 1);
    }

    void ChangeColor(int level, ColorEnum color)
    {
        Color destColor = Color.black;

        if (color == ColorEnum.Yellow)
        {
            if (!ColorUtility.TryParseHtmlString(_yellow, out destColor))
            {
                Debug.Log($"Failed to TryParseHtmlString : {_yellow}");
                return;
            }
        }
        else if (color == ColorEnum.Gray)
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
        //스킬을 사용하면 타이머를 활성화
        //활성화 되면 스킬이 어두워지고 쿨타임이 표시됨.
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

    public override void ActivateLevelUp(bool DoYouActivate)
    {
        GetObject((int)GameObjects.LevelUp).SetActive(DoYouActivate);
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

