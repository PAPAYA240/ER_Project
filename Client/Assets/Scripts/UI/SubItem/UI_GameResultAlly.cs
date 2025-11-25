using Google.Protobuf.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameResultAlly : UI_Base
{
    enum Images { CharImage }
    enum Texts { UserName, Kill, Death, Asist }


    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
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
        GetImage((int)Images.CharImage).sprite = Managers.Resource.Load<Sprite>($"Sprite/CharResult_{type.ToString()}_S000");
    }

    public void SetUserName(string name)
    {
        GetText((int)Texts.UserName).text = name;
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
}
