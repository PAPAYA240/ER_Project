using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_TSkill : UI_SkillBase
{

    enum Images
    {
        // 추가 할거면 밑으로만 위에 무엇을 추가하지 말 것. 이미지 잘 못 바뀜.
        Level_1,
        Level_2,
        Level_3,
        SkillImg
    }

    enum GameObjects
    {
        Mask,
        LevelUp
    }

    enum ColorEnum
    {
        Yellow,
        Gray
    }

    public override void Init()
    {
        _maxSkillLevel = 3;

        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        ActivateLevelUp(false);

        SetSkillLevel(_skillLevel);
    }

    void Update()
    {
        
    }

    void SetSkillLevel(int level)
    {
        //TODO 레벨 체크 이렇게 해야되나
        if (level < 0 || level > _maxSkillLevel)
            return;

        if (level == 1)
            GetObject((int)GameObjects.Mask).gameObject.SetActive(false);

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

        SetSkillLevel(_skillLevel + 1);
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

    public override void UseSkill()
    {
    }
}
