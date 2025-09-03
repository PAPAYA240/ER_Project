using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerInterface : UI_Base
{
    enum Images 
    {
        ProfileImage
    }

    public enum GameObjects
    {
        QSkill,
        WSkill,
        ESkill,
        RSkill,
        TSkill,
        DSkill,
        FSkill,
        HpBar,
        StaminaBar,
        LevelAndExp,
        Credit
    }

    //TODO 디파인으로 각 요소들을 관리?
    string _characterName = "Hyunwoo_S000";
    string _characterCode = "007";
    string _weaponCode = "081";
    string _tacticalCode = "4000000";

    int _remainSkillPoint = 1;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        LoadProfile(_characterName);
        LoadQWERTImage(_characterCode);
        LoadWeaponSkillImage(_weaponCode);
        LoadTacticalSkillImage(_tacticalCode);
    }

    void Update()
    {
        
    }


    #region ImageSetting
    public void LoadProfile(string characterName)
    {
        string path = "Sprite/CharProfile_" + characterName;
        Sprite sprite = Managers.Resource.Load<Sprite>(path);
        if (sprite == null)
            return;

        GetImage((int)Images.ProfileImage).sprite = sprite;
    }

    public void LoadQWERTImage(string characterCode)
    {
        string path = "";
        path = "Sprite/SkillIcon_1" + characterCode;

        SetSkillImgSet(GameObjects.QSkill, path + "100");
        SetSkillImgSet(GameObjects.WSkill, path + "200");
        SetSkillImgSet(GameObjects.ESkill, path + "300");
        SetSkillImgSet(GameObjects.RSkill, path + "400");
        SetSkillImgSet(GameObjects.TSkill, path + "500");
    }
    public void LoadWeaponSkillImage(string weaponCode)
    {
        SetSkillImgSet(GameObjects.DSkill, "Sprite/WSkillIcon_" + weaponCode);
    }
    public void LoadTacticalSkillImage(string tacticalCode)
    {
        SetSkillImgSet(GameObjects.FSkill, "Sprite/VSkillIcon_" + tacticalCode);
    }

    public void SetSkillImgSet(GameObjects skill, string path)
    {
        switch (skill)
        {
            case GameObjects.QSkill:
            case GameObjects.WSkill:
            case GameObjects.ESkill:
            case GameObjects.RSkill:
            case GameObjects.TSkill:
            case GameObjects.DSkill:
            case GameObjects.FSkill:
                {
                    GameObject go = GetObject((int)skill);
                    if (go == null)
                        return;

                    UI_SkillBase ui_skill = go.GetComponent<UI_SkillBase>();
                    if(ui_skill != null)
                        ui_skill.SetImage(path);
                }
                break;
        }
    }

    #endregion

    #region Skill
    public void UseSkill(GameObjects skill)
    {
        switch (skill)
        {
            case GameObjects.QSkill:
            case GameObjects.WSkill:
            case GameObjects.ESkill:
            case GameObjects.RSkill:
            case GameObjects.DSkill:
            case GameObjects.FSkill:
                {
                    GameObject go = GetObject((int)skill);
                    if (go == null)
                        return;

                    UI_SkillBase ui_skill = go.GetComponent<UI_SkillBase>();
                    if (ui_skill != null)
                        ui_skill.UseSkill();
                }
                break;
        }
    }
    #endregion

    #region Level
    public void LevelUp()
    {
        _remainSkillPoint += 1;
    }

    #endregion

    #region Credit
    public void PlusCredit(int credit)
    {
        UI_Credit uI_Credit = GetObject((int)GameObjects.Credit).GetComponent<UI_Credit>();
        if (null != uI_Credit)
            uI_Credit.PlusCredit(credit);
    }

    public void MinusCredit(int credit)
    {
        UI_Credit uI_Credit = GetObject((int)GameObjects.Credit).GetComponent<UI_Credit>();
        if (null != uI_Credit)
            uI_Credit.MinusCredit(credit);
    }

    public void UseCredit(int credit)
    {
        UI_Credit uI_Credit = GetObject((int)GameObjects.Credit).GetComponent<UI_Credit>();
        if (null != uI_Credit)
            uI_Credit.UseCredit(credit);
    }
    #endregion
}
