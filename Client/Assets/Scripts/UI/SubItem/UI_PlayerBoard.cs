using Data;
using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerBoard : UI_Base
{
    enum Images
    { 
        Bg,
        CharIcon
    }

    enum Texts
    {
        PB_LevelText,
        PB_PlayerName,
        Kill,
        Death,
        Asist
    }

    public enum GameObjects
    {
        Weapon,
        Armor,
        Head,
        Arm,
        Leg
    }

    // 무엇을 손보냐
    // 캐릭터 이미지
    // 레벨
    // 이름
    // 킬뎃
    // 아이템
    // 배경 색

    public int UID {  get; set; }

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

    public void SetCharImage(string charName)
    {
        Image image = GetImage((int)Images.CharIcon);

        image.sprite = Managers.Resource.Load<Sprite>($"Sprite/CharScore_{charName}_S000");
        image.rectTransform.sizeDelta = new Vector2(image.sprite.rect.width, image.sprite.rect.height);
    }

    public void SetBgColor(int team)
    {
        // Allies
        if(team == Managers.Object.MyPlayer.ObjInfo.Player.Team)
        {
            GetImage((int)Images.Bg).color = new Color(0.5f, 0.5f, 1f);
        }
        // Enemies
        else
        {
            GetImage((int)Images.Bg).color = new Color(1f, 0.5f, 0.5f);
        }
    }

    public void SetLevelText(int level)
    {
        GetText((int)Texts.PB_LevelText).text = $"Lv.{level.ToString()}";
    }

    public void SetNameText(string name)
    {
        GetText((int)Texts.PB_PlayerName).text = name;
    }

    public void SetKill(int kill)
    {
        GetText((int)Texts.Kill).text = kill.ToString();
    }

    public void SetDeath(int death)
    {
        GetText((int)Texts.Death).text = death.ToString();
    }

    public void SetAsist(int asist)
    {
        GetText((int)Texts.Asist).text = asist.ToString();
    }

    public void SetKDA(int kill, int death, int asist)
    {
        SetKill(kill);
        SetDeath(death);
        SetAsist(asist);
    }

    public void SetEquipItem(GameObjects go, int itemId)
    {
        GetObject((int)go).GetComponent<UI_EquipItemSlot>().SetItem(DataManager.ItemDict[itemId] as EquipItemInfo);
    }
}
