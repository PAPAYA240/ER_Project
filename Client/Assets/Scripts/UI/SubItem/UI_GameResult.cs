using Google.Protobuf.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameResult : UI_Base
{
    enum Images { MyChar, MyCharShadow }
    enum Texts 
    { 
        CharNameText, 
        UserNameText, 
        MyPlayerKillText, 
        MyPlayerDeathText, 
        MyPlayerAsistText, 
        GameResultText 
    }
    enum GameObjects { Ally_1, Ally_2, Ally_3 }

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

    public void SetCharImage(CharacterType type)
    {
        Sprite sprite = Managers.Resource.Load<Sprite>($"Sprite/CharFull_{type.ToString()}_S000");
        GetImage((int)Images.MyChar).sprite = sprite;
        GetImage((int)Images.MyCharShadow).sprite = sprite;

        // 크기 조절.
    }
}
