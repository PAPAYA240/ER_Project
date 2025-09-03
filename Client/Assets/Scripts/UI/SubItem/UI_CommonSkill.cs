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
        // �߰� �ҰŸ� �����θ� ���� ������ �߰����� �� ��. �̹��� �� �� �ٲ�.
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

    //Temp
    float _remainCool = 0;
    float _maxCool = 10.0f;

    public override void Init()
    {

        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        GetObject(_stamina).gameObject.SetActive(false);

        GetText((int)Texts.CooldownTimerText).text = "";
        ActivateLevelUp(DoYouActivate: false);

        SetSkillLevel(_skillLevel);
    }

    void Update()
    {
        if (_skillLevel == 0)
            return;
        //temp
        //��ٿ�Ÿ�̸Ӱ� Ȱ��ȭ �Ǿ� ������ �� ��ٿ��� ȣ���ؼ� ��Ÿ���� ���������� ����
        //TODO ��Ÿ�� �̹��� ���ư��°� �ؾߵ�.
        if(GetObject(_cooldownTimer).activeSelf && _remainCool > 0.0f)
        {
            _remainCool = Math.Max(0.0f, _remainCool - Time.deltaTime);

            if( _remainCool > 0.0f )
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

        for(int i = 1; i <= _skillLevel; ++i) 
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
        //��ų�� ����ϸ� Ÿ�̸Ӹ� Ȱ��ȭ
        //Ȱ��ȭ �Ǹ� ��ų�� ��ο����� ��Ÿ���� ǥ�õ�.
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
    public override void ActivateLevelUp(bool DoYouActivate)
    {
        GetObject((int)GameObjects.LevelUp).SetActive(DoYouActivate);
    }

    public override void SetImage(string path)
    {
        Sprite sprite = Managers.Resource.Load<Sprite>(path);
        GetButton((int)Buttons.SkillButton).image.sprite = sprite;
    }
}
