using UnityEngine;
using UnityEngine.UI;

public class UI_YukiStud : Monobehaviour
{
    enum Images { Gage_1, Gage_2, Gage_3, Gage_4 }

    const int _maxStudCount = 4;

    private Sprite _studOff;
    private Sprite _studOn;

    public override void Init()
    {
        Bind<Image>(typeof(Images));

        _studOff = Managers.Resource.Load<Sprite>("Sprite/Img_Yuki_Gage_01");
        _studOn = Managers.Resource.Load<Sprite>("Sprite/Img_Yuki_Gage_02");
    }

    public void Awake()
    {
        Init();
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void SetStud(int count)
    {
        for (int i = 0; i < count; i++)
        {
            GetImage((int)Images.Gage_1 + i).sprite = _studOn;
        }
        for(int i = count; i < _maxStudCount; i++)
        {
            GetImage((int)Images.Gage_1 + i).sprite = _studOff;
        }
    }
}
