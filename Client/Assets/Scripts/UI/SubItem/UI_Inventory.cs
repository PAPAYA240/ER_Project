using Data;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Inventory : Monobehaviour
{
    enum GameObjects
    {
        ItemSlot_1, 
        ItemSlot_2, 
        ItemSlot_3, 
        ItemSlot_4, 
        ItemSlot_5, 
        ItemSlot_6, 
        ItemSlot_7, 
        ItemSlot_8, 
        ItemSlot_9, 
        ItemSlot_0
    }

    public override void Init()
    {
        Bind<GameObject>(typeof(GameObjects));
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

    public void SetItem(ItemInfoBase item, int index)
    {
        GetObject((int)GameObjects.ItemSlot_1 + index).GetComponent<UI_ItemSlot>().SetItem(item);
    }

    // 아이템 사용 및 착용
}
