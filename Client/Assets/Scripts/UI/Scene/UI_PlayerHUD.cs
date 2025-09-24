using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_PlayerHUD : UI_Scene
{
    public enum GameObjects
    { 
        TurbineLeft, 
        TurbineCenter, 
        TurbineRight,
        Minimap
    }


    public Sprite TurbineAlly;
    public Sprite TurbineEnemy;
    public Sprite TurbineNeutral;
    public Sprite TurbineOff;

    public override void Init()
    {
        base.Init();

        TurbineAlly = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_AmpliTurbine_Ally");
        TurbineEnemy = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_AmpliTurbine_Enemy");
        TurbineNeutral = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_AmpliTurbine_Neutral");
        TurbineOff = Managers.Resource.Load<Sprite>("Sprite/Ico_Map_AmpliTurbine_Off");

        Bind<GameObject>(typeof(GameObjects));
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
}
