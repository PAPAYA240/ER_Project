using TMPro;
using UnityEngine;

public class UI_Stat : UI_Base
{
    public enum Texts
    {
        AttackText,
        AttackAmpText,
        AttackSpeedText,
        CriticalRatioText,
        SkillAmpText,
        DefenseText,
        SkillAccText,
        SpeedText,
        HpText,
        StaminaText,
        VisionText,
        AttackRangeText,
        CCResistanceText,
        PenetrationText,
        LifeStealText
    }

    enum GameObjects
    { ExtraStat }



    public override void Init()
    {
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));

        ActivateExtra(false);
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
        if(Input.GetKeyDown(KeyCode.C))
        {
            ActivateExtra(true);
        }
        
        if(Input.GetKeyUp(KeyCode.C))
        {
            ActivateExtra(false);
        }
    }

    public void SetText(Texts texts,  string str)
    {
        GetText((int)texts).text = str;
    }

    private void ActivateExtra(bool isActivate)
    {
        GetObject((int)GameObjects.ExtraStat).SetActive(isActivate);
    }
}
