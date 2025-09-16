using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SelectedCharacterImage : UI_Base
{
    enum Images { CharImage, TraitSkillIcon, WeaponImage, Bar }
    enum Texts { NameText }


    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
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
        img.sprite = Managers.Resource.Load<Sprite>($"CharScore_{charName}_S000");
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
        img.sprite = Managers.Resource.Load<Sprite>($"Ico_Ability_{weaponName}");
        Vector2 size = img.sprite.rect.size;
        img.gameObject.GetComponent<RectTransform>().sizeDelta = size;
    }

    public void SetBar()
    {
        //TODO 내 플레이어인지 아군 플레이어인지 적 플레이어인지 받아와서 바의 색을 채운다.
    }
}
