using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerNameTag : UI_Base
{
    enum Images { Hp }

    enum Texts 
    { 
        LevelText, 
        NameText 
    }

    enum GameObjects
    {
        HpBar,
        StaminaBar,
    }

    const float _nameTagHeight = 2.5f;

    GameObject _target;

    static Color _red;
    static Color _green;
    static Color _blue;

    public override void Init()
    {
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));

        ColorUtility.TryParseHtmlString("#D5163A", out _red);
        ColorUtility.TryParseHtmlString("#76CC22", out _green);
        ColorUtility.TryParseHtmlString("#028FEE", out _blue);

        Camera.main.gameObject.GetOrAddComponent<CameraController>().LateUpdateAction += UpdatePosition;
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
    //private void LateUpdate()
    //{
    //    if (_target != null)
    //    {
    //        UpdatePosition();
    //    }
    //}

    private void UpdatePosition()
    {
        if (_target == null)
            return;

        Vector3 worldPos = _target.transform.position + new Vector3(0, _nameTagHeight, 0);
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        gameObject.transform.position = screenPos;
    }

    public void SetLevelText(int level)
    {
        GetText((int)Texts.LevelText).text = level.ToString();
    }

    public void SetNameText(string name)
    {
        GetText((int)Texts.LevelText).text = name;
    }

    public void SetTarget(GameObject target)
    {
        _target = target;
    }

    public void SetHPColor()
    {
        if (_target == null)
            return;

        PlayerController targetPc = _target.GetComponent<PlayerController>();
        if (null == targetPc)
            return;

        if (Managers.Object.MyPlayer.gameObject.transform == _target.transform)
            GetImage((int)Images.Hp).color = _green;
        else if(Managers.Object.MyPlayer.ObjInfo.Player.Team == targetPc.ObjInfo.Player.Team)
            GetImage((int)Images.Hp).color = _blue;
        else if(Managers.Object.MyPlayer.ObjInfo.Player.Team != targetPc.ObjInfo.Player.Team)
            GetImage((int)Images.Hp).color = _red;
    }

    public void SetHp(float newHp)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBarTick>().SetHp(newHp);
    }
    public void SetMaxHp(float newMaxHp)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBarTick>().SetMaxHp(newMaxHp);
    }
    public void SetBarrier(float newBarrier)
    {
        GetObject((int)GameObjects.HpBar).GetComponent<UI_HpBarTick>().SetBarrier(newBarrier);
    }
    public void SetStamina(float newStamina)
    {
        GetObject((int)GameObjects.StaminaBar).GetComponent<UI_BarNonText>().SetValue(newStamina);
    }
    public void SetMaxStamina(float newMaxStamina)
    {
        GetObject((int)GameObjects.StaminaBar).GetComponent<UI_BarNonText>().SetMaxValue(newMaxStamina);
    }
}
