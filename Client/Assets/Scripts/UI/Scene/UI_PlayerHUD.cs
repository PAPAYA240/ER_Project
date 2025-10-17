using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PlayerHUD : UI_Scene
{
    public enum GameObjects
    { 
        Timer,
        TurbineLeft, 
        TurbineCenter, 
        TurbineRight,
        Minimap,
        KDA,
        KillNoti
    }


    public Sprite TurbineAlly;
    public Sprite TurbineEnemy;
    public Sprite TurbineNeutral;
    public Sprite TurbineOff;

    private Coroutine _coNotifyKill = null;

    public override void Init()
    {
        base.Init();

        TurbineAlly = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_AmpliTurbine_Ally");
        TurbineEnemy = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_AmpliTurbine_Enemy");
        TurbineNeutral = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_AmpliTurbine_Neutral");
        TurbineOff = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_AmpliTurbine_Off");

        Bind<GameObject>(typeof(GameObjects));

        GetObject((int)GameObjects.KillNoti).SetActive(false);
    }

    private void Start()
    {
        
    }

    void Update()
    {
        UpdateScale();
    }

    public void CaptureTurbine(GameObjects go ,int team)
    {
        bool isAlly = false;
        if (Managers.Object.MyPlayer.ObjInfo.Player.Team == team)
            isAlly = true;

        switch (go)
        {
            case GameObjects.TurbineLeft:
            case GameObjects.TurbineCenter:
            case GameObjects.TurbineRight:
                {
                    GetObject((int)go).GetComponent<UI_Turbine>().CaptureTurbine(isAlly);

                    if (go == GameObjects.TurbineLeft)
                    {
                        if (isAlly)
                        {
                            GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().SetTurbineImage(UI_Minimap.Images.TurbineIconLeft, TurbineAlly);
                        }
                        else
                        {
                            GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().SetTurbineImage(UI_Minimap.Images.TurbineIconLeft, TurbineEnemy);
                        }
                    }
                    else if (go == GameObjects.TurbineCenter)
                    {
                        if (isAlly)
                        {
                            GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().SetTurbineImage(UI_Minimap.Images.TurbineIconCenter, TurbineAlly);
                        }
                        else
                        {
                            GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().SetTurbineImage(UI_Minimap.Images.TurbineIconCenter, TurbineEnemy);
                        }
                    }
                    else
                    {
                        if (isAlly)
                        {
                            GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().SetTurbineImage(UI_Minimap.Images.TurbineIconRight, TurbineAlly);
                        }
                        else
                        {
                            GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().SetTurbineImage(UI_Minimap.Images.TurbineIconRight, TurbineEnemy);
                        }
                    }
                }
                break;
        }
    }

    public void SetTimer(int phase, float clientLocalTargetRealtimeSinceStartupEnd)
    {
        GetObject((int)GameObjects.Timer).GetComponent<UI_Timer>().SetTimer(phase, clientLocalTargetRealtimeSinceStartupEnd);
    }

    public void SetKDA(int kill, int death, int asist)
    {
        GetObject((int)GameObjects.KDA).GetComponent<UI_KDA>().SetKDA(kill, death, asist);
    }

    public void NotifyKill(PlayerController attPc, PlayerController diePc)
    {
        if(_coNotifyKill != null)
        {
            StopCoroutine(_coNotifyKill);
            _coNotifyKill = null;
        }

        StartCoroutine(CoNotifyKill(attPc, diePc));
    }

    IEnumerator CoNotifyKill(PlayerController attPc, PlayerController diePc)
    {
        GetObject((int)GameObjects.KillNoti).SetActive(true);
        GetObject((int)GameObjects.KillNoti).GetComponent<UI_KillNoti>().NotifyKill(attPc, diePc);

        yield return new WaitForSeconds(3);

        GetObject((int)GameObjects.KillNoti).SetActive(false);
    }
}
