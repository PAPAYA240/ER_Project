using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
        // ï¿½ß°ï¿½ ï¿½Ò°Å¸ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½Î¸ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ ï¿½ß°ï¿½ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ ï¿½ï¿½. ï¿½Ì¹ï¿½ï¿½ï¿½ ï¿½ï¿½ ï¿½ï¿½ ï¿½Ù²ï¿½.
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
        //Äð´Ù¿îÅ¸ÀÌ¸Ó°¡ È°¼ºÈ­ µÇ¾î ÀÖÀ¸¸é ¼Â Äð´Ù¿îÀ» È£ÃâÇØ¼­ ÄðÅ¸ÀÓÀ» Áö¼ÓÀûÀ¸·Î °»½Å
        //TODO ÄðÅ¸ÀÓ ÀÌ¹ÌÁö µ¹¾Æ°¡´Â°Å ÇØ¾ßµÊ.
        if (GetObject(_cooldownTimer).activeSelf && _remainCool > 0.0f)
        {
            _remainCool = Math.Max(0.0f, _remainCool - Time.deltaTime);

            if( _remainCool > 0.0f )
            {
                // ÄðÅ¸ÀÓÀÌ ³²¾ÆÀÖÀ»¶§
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
            if (IsEnoughStamina(ui_PlayerInterface.GetStamina())) //½ºÅ×¹Ì³Ê°¡ ÃæºÐÇÏ¸é
                ActivateStamina(false);
            else //½ºÅ×¹Ì³Ê°¡ ºÎÁ·ÇÏ¸é
                ActivateStamina(true);
        }
    }

    void SetSkillLevel(int level)
    {
        //TODO ï¿½ï¿½ï¿½ï¿½ Ã¼Å© ï¿½Ì·ï¿½ï¿½ï¿½ ï¿½Ø¾ßµÇ³ï¿½
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

        SetSkillLevel(_skillLevel + 1);
        OnLevelUp?.Invoke();
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
        //½ºÅ³À» »ç¿ëÇÏ¸é Å¸ÀÌ¸Ó¸¦ È°¼ºÈ­
        //È°¼ºÈ­ µÇ¸é ½ºÅ³ÀÌ ¾îµÎ¿öÁö°í ÄðÅ¸ÀÓÀÌ Ç¥½ÃµÊ.
        GetObject(_cooldownTimer).SetActive(true);
        //temp
        _remainCool = _maxCool;
        //SetCoolDown(0.4f);
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
        GetObject((int)GameObjects.LevelUp).SetActive(activate);
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

    public override bool IsEnoughStamina(float curStamina)
    {
        return curStamina > _staminaCost ? true : false;
    }
}
