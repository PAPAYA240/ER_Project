using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//D and F Skill
public class UI_DSkill : UI_SkillBase
{
    enum Buttons
    {
        SkillButton
    }

    enum Texts
    {
        CooldownTimerText,
        MapingKey,
        LevelText
    }

    enum Images
    {
        CooldownFill
    }

    enum GameObjects
    {
        CooldownTimer,
        LevelUp
    }

    const int _cooldownTimer = (int)GameObjects.CooldownTimer;

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

        GetText((int)Texts.CooldownTimerText).text = "";
        ActivateLevelUp(DoYouActivate:false);

        SetSkillLevel(_skillLevel);
    }

    void Update()
    {

    }

    void SetSkillLevel(int level)
    {
        //TODO 
        if (level < 0 || level > _maxSkillLevel)
            return;
        if (level == 1)
            GetObject(_cooldownTimer).gameObject.SetActive(false);

        _skillLevel = level;

        GetText((int)Texts.LevelText).text = _skillLevel.ToString();
    }


    public override void SkillLevelUp()
    {
        if (_skillLevel == _maxSkillLevel)
            return;

        SetSkillLevel(_skillLevel + 1);
        OnLevelUp?.Invoke(SkillKeyCode);
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

    public override void ActivateLevelUp(bool DoYouActivate)
    {
        GetObject((int)GameObjects.LevelUp).SetActive(DoYouActivate);
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
    public override void SetMaxCool(float value)
    {
        _maxCool = value;
    }
}
