using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_KillNoti : UI_Base
{
    enum Images { KillNoti, KillCharImg, DeathCharImg, WeaponIcon }
    enum Texts { KillPlayerName, DeathPlayerName }
    enum GameObjects { WeaponIcon }


    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
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

    public void NotifyKill(PlayerController attPc, PlayerController diePc)
    {
        if (attPc == null || diePc == null) return; 

        //TODO 이름 바꾸기.

        GetImage((int)Images.KillCharImg).sprite = Managers.Resource.Load<Sprite>($"Sprite/CharResult_{attPc.ObjInfo.Player.CharType.ToString()}_S000");
        GetImage((int)Images.DeathCharImg).sprite = Managers.Resource.Load<Sprite>($"Sprite/CharResult_{diePc.ObjInfo.Player.CharType.ToString()}_S000");

        Image weaponImage = GetImage((int)Images.WeaponIcon);
        weaponImage.sprite = Managers.Resource.Load<Sprite>($"Sprite/Ico_KillNoti_{attPc.ObjInfo.Player.Weapon.ToString()}");

        RectTransform rectTransform = GetObject((int)GameObjects.WeaponIcon).transform as RectTransform;
        rectTransform.sizeDelta = new Vector2(weaponImage.sprite.rect.width, weaponImage.sprite.rect.height);

        if (Managers.Object.MyPlayer != null)
        {
            if(Managers.Object.MyPlayer.ObjInfo.Player.Team == attPc.ObjInfo.Player.Team)
            {
                ColorUtility.TryParseHtmlString("#0033FF", out Color blue);

                GetImage((int)Images.KillNoti).color = blue;
            }
            else
            {
                ColorUtility.TryParseHtmlString("#FF0200", out Color red);
                GetImage((int)Images.KillNoti).color = red;
            }
        }
    }
}
