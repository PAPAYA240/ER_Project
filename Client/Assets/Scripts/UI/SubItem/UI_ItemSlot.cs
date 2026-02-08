using Data;
using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ItemSlot : Monobehaviour
{
    enum Texts
    {
        CountText
    }

    enum Buttons
    {
        Button
    }

    enum Images
    {
        ItemImage
    }

    enum GameObjects
    {
        EmptyBg,
        Button,
        ItemImage,
        Count
    }

    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Button>(typeof(Buttons));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        SetActive(GameObjects.Button, false);
        SetActive(GameObjects.Count, false);
        SetActive(GameObjects.ItemImage, false);
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

    public void SetItem(ItemInfoBase item)
    {
        if(item == null || item.Id == 0)
        {
            // 빈칸 처리
            SetActive(GameObjects.EmptyBg, true);
            SetActive(GameObjects.Button, false);
            SetActive(GameObjects.Count, false);
            SetActive(GameObjects.ItemImage, false);
            return;
        }

        SetActive(GameObjects.EmptyBg, false);
        SetActive(GameObjects.Button, true);
        SetActive(GameObjects.ItemImage, true);

        switch (item)
        {
            case ConsumableItemInfo consumableItem:
                {
                    SetActive(GameObjects.Count, true);
                    SetCount(consumableItem.Count);
                    SetImage(consumableItem.Id);
                    SetGradeBg(consumableItem.Grade);
                }
                break;
            case EquipItemInfo equipItem:
                {
                    SetActive(GameObjects.Count, false);
                    SetImage(equipItem.Id);
                    SetGradeBg(equipItem.Grade);
                }
                break;
        }
    }

    public void SetImage(int itemId)
    {
        Image image = GetImage((int)Images.ItemImage);

        image.sprite = Managers.Resource.Load<Sprite>($"Sprite/ItemIcon_{itemId}");
        image.GetComponent<RectTransform>().sizeDelta = new Vector2(image.sprite.rect.width, image.sprite.rect.height);
    }

    public void SetGradeBg(ItemGrade grade)
    {
        int num = (int)grade + 1;
        GetButton((int)Buttons.Button).image = Managers.Resource.Load<Image>($"Sprite/Ico_ItemGradebg_0{num}");
    }

    private void SetActive(GameObjects go, bool activate)
    {
        GetObject((int)go).SetActive(activate);
    }

    public void SetCount(int count)
    {
        GetText((int)Texts.CountText).text = count.ToString();
    }

    //버튼 클릭했을 때 or 마우스 커서 올렸을 때 이벤트 함수
}
