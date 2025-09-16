using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectedCharacterImage : UI_Base
{
    enum Images { CharImage, TraitSkillIcon, WeaponImage, Bar }
    enum Texts { NameText }

    static Color _red = new Color(1, 0.125f, 0.125f); 
    static Color _green = new Color(0.125f, 1, 0.125f); 
    static Color _blue = new Color(0.125f, 0.125f, 1);

    public enum BarType { None, My, Team, Enemy }

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));

        Image img = GetImage((int)Images.CharImage);
        img.sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_Question");
        Vector2 size = img.sprite.rect.size;
        float Ratio = 56 / size.y;
        img.gameObject.GetComponent<RectTransform>().sizeDelta = size * Ratio;

        img = GetImage((int)Images.WeaponImage);
        img.sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_Question");
        size = img.sprite.rect.size;
        Ratio = 55 / size.y;
        img.gameObject.GetComponent<RectTransform>().sizeDelta = size * Ratio;

        SetName("Empty");
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
        
    }

    public void SetName(string name)
    {
        GetText((int)Texts.NameText).text = name;
    }
    public void SetCharImage(string charName)
    {
        Image img = GetImage((int)Images.CharImage);
        img.sprite = Managers.Resource.Load<Sprite>($"Sprite/CharScore_{charName}_S000");
        Vector2 size = img.sprite.rect.size;
        img.gameObject.GetComponent<RectTransform>().sizeDelta = size;
    }
    public void SetTraitSkill(string skillCode)
    {
        GetImage((int)Images.TraitSkillIcon).sprite = Managers.Resource.Load<Sprite>($"TraitSkillIcon_${skillCode}");
    }
    public void SetWeaponSkill(string weaponName)
    {
        Image img = GetImage((int)Images.TraitSkillIcon);
        img.sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_Ability_{weaponName}");
        Vector2 size = img.sprite.rect.size;
        img.gameObject.GetComponent<RectTransform>().sizeDelta = size;
    }

    public void SetBar(BarType type)
    {
        switch (type)
        {
            case BarType.My:
                GetImage((int)Images.Bar).color = _green;
                break;
            case BarType.Team:
                GetImage((int)Images.Bar).color = _blue;
                break;
            case BarType.Enemy:
                GetImage((int)Images.Bar).color = _red;
                break;
        }

    }
}
