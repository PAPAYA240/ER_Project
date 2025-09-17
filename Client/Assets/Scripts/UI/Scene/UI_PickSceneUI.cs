using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.UI;
using static UI_SelectedCharacterImage;

public class UI_PickSceneUI : UI_Scene
{
    enum GameObjects
    {
        SceneBg,
        CharFullSize,
        PickScrollView,
        SelectedCharacterImage_0,
        SelectedCharacterImage_1,
        SelectedCharacterImage_2,
        SelectedCharacterImage_3,
        SelectedCharacterImage_4,
        SelectedCharacterImage_5,
        SelectedCharacterImage_6,
        SelectedCharacterImage_7,
    }

    public Action<string> OnClickedPickButton = null;

    public override void Init()
    {
        base.Init();
        Bind<GameObject>(typeof(GameObjects));

        GetObject((int)GameObjects.PickScrollView).GetComponent<UI_PickScrollView>().OnButtonClicked += ClickedCharPickButton;
    }

    private void Awake()
    {
        Init();
    }


    void Start()
    {

    }

    void Update()
    {
        UpdateScale();
    }

    public void ClickedCharPickButton(string charName)
    {
        OnClickedPickButton?.Invoke(charName);
    }

    public void ChangePickImage(CharacterType charType, int idx)
    {
        GameObject go = GetObject((int)GameObjects.SelectedCharacterImage_0 + idx);
        if (go == null)
            return;

        if (charType == CharacterType.CharacterNone)
            return;

        go.GetComponent<UI_SelectedCharacterImage>().SetCharImage(charType.ToString());
    }

    public void ChangeFullSizeImage(CharacterType charType)
    {
        GameObject go = GetObject((int)GameObjects.CharFullSize);
        if (go == null)
            return;

        if (charType == CharacterType.CharacterNone)
            return;

        string path = $"Sprite/CharFull_{charType.ToString()}_S000";

        go.GetComponent<UI_CharFullSize>().SetImage(path);
    }

public void ChangeNickname(string nickname, int idx)
    {
        GameObject go = GetObject((int)GameObjects.SelectedCharacterImage_0 + idx);
        if (go == null)
            return;

        go.GetComponent<UI_SelectedCharacterImage>().SetName(nickname);
    }

    public void ChangeBar(BarType barType, int idx)
    {
        GameObject go = GetObject((int)GameObjects.SelectedCharacterImage_0 + idx);
        if (go == null)
            return;

        go.GetComponent<UI_SelectedCharacterImage>().SetBar(barType);
    }

    public void ChangeTraitImage(TraitType traitType, int idx)
    {
        GameObject go = GetObject((int)GameObjects.SelectedCharacterImage_0 + idx);
        if (go == null)
            return;

        go.GetComponent<UI_SelectedCharacterImage>().SetTraitSkill(TraitTypeToCode(traitType));
    }

    public void ChangeWeaponImage(Weapon weaponType, int idx)
    {
        GameObject go = GetObject((int)GameObjects.SelectedCharacterImage_0 + idx);
        if (go == null)
            return;

        if (weaponType == Weapon.None)
            return;

        go.GetComponent<UI_SelectedCharacterImage>().SetWeaponSkill(weaponType.ToString());
    }

    protected override void UpdateScale()
    {
        base.UpdateScale();

        RectTransform rc = GetObject((int)GameObjects.SceneBg).GetComponent<RectTransform>();
        rc.sizeDelta = new Vector2(Screen.width, Screen.height) / _scaler.scaleFactor;
    }

    string TraitTypeToCode(TraitType traitType)
    {
        string result = "";

        switch (traitType)
        {
            case TraitType.FrailtyInfliction:
                result = "7000200";
                break;
            case TraitType.VampiricBloodline:
                result = "7000400";
                break;
            case TraitType.Adrenaline:
                result = "7000600";
                break;
            case TraitType.Accelerater:
                result = "7000700";
                break;
            case TraitType.StellarCharge:
                result = "7300100";
                break;
            case TraitType.GhostLight:
                result = "7300200";
                break;
            case TraitType.RedSprite:
                result = "7000500";
                break;
            case TraitType.SiphonMaelstorm:
                result = "7300300";
                break;
            case TraitType.DiamondShard:
                result = "7100100";
                break;
            case TraitType.Ironclad:
                result = "7100200";
                break;
            case TraitType.HeavyKneepads:
                result = "7100400";
                break;
            case TraitType.BitterRetribution:
                result = "7100500";
                break;
            case TraitType.HealingFactor:
                result = "7200100";
                break;
            case TraitType.AmplificationDrone:
                result = "7200200";
                break;
            case TraitType.HealingDrone:
                result = "7200300";
                break;
            case TraitType.Sentinel:
                result = "7200500";
                break;
        }

        return result;
    }
}
