using Google.Protobuf.Protocol;
using TMPro;
using UnityEditor;
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

    Color _victoryColor;
    Color _failColor;

    int _allyCount = 0;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));

        ColorUtility.TryParseHtmlString("#334872", out _victoryColor);
        ColorUtility.TryParseHtmlString("#80070A", out _failColor);

        GetObject((int)GameObjects.Ally_1).SetActive(false);
        GetObject((int)GameObjects.Ally_2).SetActive(false);
        GetObject((int)GameObjects.Ally_3).SetActive(false);
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

        Image myChar = GetImage((int)Images.MyChar);
        Image myCharShadow = GetImage((int)Images.MyCharShadow);

        myChar.sprite = sprite;
        myCharShadow.sprite = sprite;

        // 크기 조절.
        Vector2 size = myChar.sprite.rect.size;
        RectTransform rtChar = myChar.gameObject.GetComponent<RectTransform>();
        RectTransform rtCharShadow = myCharShadow.gameObject.GetComponent<RectTransform>();
        rtChar.sizeDelta = size;
        rtCharShadow.sizeDelta = size;

        float ratio = 420f / size.y;
        rtChar.localScale = new Vector3(ratio, ratio, ratio);
        rtCharShadow.localScale = new Vector3(ratio, ratio, ratio);
    }

    public void SetGameResultText(bool isVictory)
    {
        TextMeshProUGUI resultText = GetText((int)Texts.GameResultText);

        if (isVictory)
        {
            resultText.text = "VICTORY";
            resultText.color = _victoryColor;
        }
        else
        {
            resultText.text = "FAIL";
            resultText.color = _failColor;
        }
    }

    public void SetUserName(string name)
    {
        GetText((int)Texts.UserNameText).text = name;
    }

    public void SetCharName(CharacterType type)
    {
        string name = type.ToString();
        GetText((int)Texts.CharNameText).text = name.ToUpper();
    }

    public void SetKillAmount(int value)
    {
        GetText((int)Texts.MyPlayerKillText).text = value.ToString();
    }

    public void SetDeathAmount(int value)
    {
        GetText((int)Texts.MyPlayerDeathText).text = value.ToString();
    }

    public void SetAsistAmount(int value)
    {
        GetText((int)Texts.MyPlayerAsistText).text = value.ToString();
    }

    public void SetMyPlayer()
    {
        MyPlayerController myPlayer = Managers.Object.MyPlayer;

        SetKillAmount(myPlayer.KillAmount);
        SetDeathAmount(myPlayer.DeathAmount);
        SetAsistAmount(myPlayer.AsistAmount);

        SetUserName(myPlayer.NickName);
        SetCharName(myPlayer.ObjInfo.Player.CharType);

        SetCharImage(myPlayer.ObjInfo.Player.CharType);
    }

    public void AddAlly(PlayerController pc)
    {
        if (_allyCount > 2)
            return;

        Debug.Log($"AddAlly, _allyCount : {_allyCount}");


        GameObject go = GetObject((int)GameObjects.Ally_1 + _allyCount);
        if (go == null) 
            return; 

        go.SetActive(true);

        UI_GameResultAlly ui = GetObject((int)GameObjects.Ally_1 + _allyCount).GetComponent<UI_GameResultAlly>();
        if (ui == null) 
            return;

        _allyCount++;

        ui.SetCharImage(pc.ObjInfo.Player.CharType);

        ui.SetUserName(pc.NickName);

        ui.SetKill(pc.KillAmount);
        ui.SetDeath(pc.DeathAmount);
        ui.SetAsist(pc.AsistAmount);
        Debug.Log($"AddAlly, Success");
    }
}
