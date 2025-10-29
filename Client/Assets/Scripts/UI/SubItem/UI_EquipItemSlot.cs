using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_EquipItemSlot : UI_Base
{
    enum Images { GradeBg, ItemImage }

    public EquipItemType Type { get; set; }

    public override void Init()
    {
        Bind<Image>(typeof(Images));

        switch (Type)
        {
            case EquipItemType.Weapon:
                GetImage((int)Images.ItemImage).sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_Status_Weapon");
                break;
            case EquipItemType.Head:
                GetImage((int)Images.ItemImage).sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_Status_Head");
                break;
            case EquipItemType.Body:
                GetImage((int)Images.ItemImage).sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_Status_Armor");
                break;
            case EquipItemType.Arm:
                GetImage((int)Images.ItemImage).sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_Status_Arm");
                break;
            case EquipItemType.Leg:
                GetImage((int)Images.ItemImage).sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_Status_Leg");
                break;
        }

        SetGrade(ItemGrade.Common);
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

    public void SetItem(EquipItemInfo item)
    {
        SetItemImage(item.Id);
        SetGrade(item.Grade);
    }

    private void SetItemImage(int id)
    {
        Image image = GetImage((int)Images.ItemImage);

        image.sprite = Managers.Resource.Load<Sprite>($"Sprite/ItemIcon_{id}");
        image.GetComponent<RectTransform>().sizeDelta = new Vector2(image.sprite.rect.width, image.sprite.rect.height);
    }

    private void SetGrade(ItemGrade grade)
    {
        int num = (int)grade + 1;
        GetImage((int)Images.GradeBg).sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_ItemGradebg_0{num}");
    }
}
