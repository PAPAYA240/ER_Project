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
        //��ٿ�Ÿ�̸Ӱ� Ȱ��ȭ �Ǿ� ������ �� ��ٿ��� ȣ���ؼ� ��Ÿ���� ���������� ����
        if (_skillLevel == 0)
            return;

        if (GetObject(_cooldownTimer).activeSelf && _remainCool > 0.0f)
        {
            _remainCool = Math.Max(0.0f, _remainCool - Time.deltaTime);

            if (_remainCool > 0.0f)
            {
                // ��Ÿ���� ����������
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
        //TODO ���� üũ �̷��� �ؾߵǳ�
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

    public override void UseSkill()
    {
        //��ų�� ����ϸ� Ÿ�̸Ӹ� Ȱ��ȭ
        //Ȱ��ȭ �Ǹ� ��ų�� ��ο����� ��Ÿ���� ǥ�õ�.
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
