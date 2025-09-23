using Data;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_TSkill : UI_SkillBase
{

    enum Texts
    {
        CooldownTimerText
    }

    enum Images
    {
        // 추가 할거면 밑으로만 위에 무엇을 추가하지 말 것. 이미지 잘 못 바뀜.
        Level_1,
        Level_2,
        Level_3,
        SkillImg,
        CooldownFill
    }

    enum GameObjects
    {
        CooldownTimer,
        LevelUp
    }

    enum ColorEnum
    {
        Yellow,
        Gray
    }


    //Temp
    float _remainCool = 0;
    float _maxCool = 10.0f;

    public override void Init()
    {
        _maxSkillLevel = 3;

        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        BindEvent(gameObject, OnMouseOverEvent, Define.UIEvent.PointerEnter);
        BindEvent(gameObject, OnMouseExitEvent, Define.UIEvent.PointerExit);

        GetText((int)Texts.CooldownTimerText).text = "";
        ActivateLevelUp(false);

        _skillLevel = 1;
        SetSkillLevel(_skillLevel);
    }

    void Update()
    {

        if (GetObject((int)GameObjects.CooldownTimer).activeSelf && _remainCool > 0.0f)
        {
            _remainCool = Math.Max(0.0f, _remainCool - Time.deltaTime);

            if (_remainCool > 0.0f)
            {

                GetImage((int)Images.CooldownFill).fillAmount = _remainCool / _maxCool;
                SetCoolDown(_remainCool);
            }
            else
            {
                GetObject((int)GameObjects.CooldownTimer).SetActive(false);
            }
        }
    }


    void SetSkillLevel(int level)
    {
        //TODO 레벨 체크 이렇게 해야되나
        if (level < 0 || level > _maxSkillLevel)
            return;

        if (level == 1)
            GetObject((int)GameObjects.CooldownTimer).gameObject.SetActive(false);

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
        GetObject((int)GameObjects.CooldownTimer).SetActive(true);
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
            case 1:
                {
                    if (playerLevel >= 5)
                    {
                        GetObject((int)GameObjects.LevelUp).SetActive(activate);
                        return;
                    }

                }
                break;
            case 2:
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

    public override void SetImage(string path)
    {
        Sprite sprite = Managers.Resource.Load<Sprite>(path);
        if (sprite == null)
        {
            Debug.Log($"null : {path}");
            return;
        }
        GetImage((int)Images.SkillImg).sprite = sprite;
    }

    public override void SetMaxCool(float value)
    {
        _maxCool = value;
    }
}
