using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Device;
using static UI_Minimap;
using static UnityEngine.GraphicsBuffer;

public class UI_MonsterHpBar : UI_Base
{
    enum Texts 
    { 
        LevelText, 
        HpText 
    }

    enum GameObjects 
    {
        HpBar,
        Patience,
        TextBg
    }

    public float NameTagHeight { get; set; } = 2.5f;

    GameObject _target;

    public override void Init()
    {
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

    private void LateUpdate()
    {
        if (_target != null)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        Vector3 worldPos = _target.transform.position + new Vector3(0, NameTagHeight, 0);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        gameObject.transform.position = screenPos;
    }

    public void SetLevelText(int level)
    {
        GetText((int)Texts.LevelText).text = level.ToString();
    }

    public void SetHpText(string str)
    {
        GetText((int)Texts.HpText).text = str;
    }

    public void SetTarget(GameObject target)
    {
        _target = target;
    }

    public void SetHp(float newHp)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_BarTick>().SetValue(newHp);
    }
    public void SetStamina(float newStamina)
    {
        GetObject((int)GameObjects.Patience).GetComponent<UI_BarNonText>().SetValue(newStamina);
    }
}
