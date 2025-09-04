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
    float _remainCool = 0;
    float _maxCool = 10.0f;

    public override void Init()
    {
        _maxSkillLevel = 3;

        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        GetText((int)Texts.CooldownTimerText).text = "";
        ActivateLevelUp(DoYouActivate:false);

        SetSkillLevel(_skillLevel);
    }

    void Update()
    {
        //temp
        //쿨다운타이머가 활성화 되어 있으면 셋 쿨다운을 호출해서 쿨타임을 지속적으로 갱신
        if (_skillLevel == 0)
            return;

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


    }

    void SetSkillLevel(int level)
    {
        //TODO 레벨 체크 이렇게 해야되나
        if (level < 0 || level > _maxSkillLevel)
            return;
        if (level == 1)
            GetObject(_cooldownTimer).gameObject.SetActive(false);

        _skillLevel = level;

        GetText((int)Texts.LevelText).text = _skillLevel.ToString();
    }


    public override void SkillLevelUp()
    {
        SetSkillLevel(_skillLevel + 1);
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
    public override void SetMaxCool(float value)
    {
        _maxCool = value;
    }
}
