using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.Protobuf.Protocol;
using Data;

public class UI_Equipment : Monobehaviour
{
    enum GameObjects
    {
        Weapon,
        Head,
        Body,
        Arm,
        Leg
    }

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));

        GetObject((int)GameObjects.Weapon).GetComponent<UI_EquipItemSlot>().Type = EquipItemType.Weapon;
        GetObject((int)GameObjects.Head).GetComponent<UI_EquipItemSlot>().Type = EquipItemType.Head;
        GetObject((int)GameObjects.Body).GetComponent<UI_EquipItemSlot>().Type = EquipItemType.Body;
        GetObject((int)GameObjects.Arm).GetComponent<UI_EquipItemSlot>().Type = EquipItemType.Arm;
        GetObject((int)GameObjects.Leg).GetComponent<UI_EquipItemSlot>().Type = EquipItemType.Leg;
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

    public void Equip(EquipItemInfo item)
    {
        switch (item.Type)
        {
            case EquipItemType.Weapon:
                GetObject((int)GameObjects.Weapon).GetComponent<UI_EquipItemSlot>().SetItem(item);
                break;
            case EquipItemType.Head:
                GetObject((int)GameObjects.Head).GetComponent<UI_EquipItemSlot>().SetItem(item);
                break;
            case EquipItemType.Body:
                GetObject((int)GameObjects.Body).GetComponent<UI_EquipItemSlot>().SetItem(item);
                break;
            case EquipItemType.Arm:
                GetObject((int)GameObjects.Arm).GetComponent<UI_EquipItemSlot>().SetItem(item);
                break;
            case EquipItemType.Leg:
                GetObject((int)GameObjects.Leg).GetComponent<UI_EquipItemSlot>().SetItem(item);
                break;
        }
    }
}
