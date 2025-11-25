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
        EnemyScore,
        TeamScore,
        TurbineLeft, 
        TurbineCenter, 
        TurbineRight,
        Minimap,
        KDA,
        KillNoti,
        BattleBoard,
        GameResult
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
        GetObject((int)GameObjects.BattleBoard).SetActive(false);
        GetObject((int)GameObjects.GameResult).SetActive(false);
    }

    private void Start()
    {
        
    }

    void Update()
    {
        UpdateScale();

        if(Input.GetKeyDown(KeyCode.Tab))
        {
            GetObject((int)GameObjects.BattleBoard).SetActive(true);
        }
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            GetObject((int)GameObjects.BattleBoard).SetActive(false);
        }

        //TODO ���� ������ �� ȣ��Ǿߵ�.
        if(Input.GetKeyDown(KeyCode.M))
        {
            SetGameResult(true);
        }
    }

    #region Beacon

    public void CaptureTurbine(Beacon beacon ,int team)
    {
        bool isAlly = false;
        if (Managers.Object.MyPlayer.ObjInfo.Player.Team == team)
            isAlly = true;

        GameObjects go = GameObjects.TurbineLeft;
        switch (beacon)
        {
            case Beacon.Left:
                go = GameObjects.TurbineLeft;
                break;
            case Beacon.Center:
                go = GameObjects.TurbineCenter;
                break;
            case Beacon.Right:
                go = GameObjects.TurbineRight;
                break;
        }

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

    public void SetBeaconTimer(Beacon beacon, float Time)
    {
        GameObjects go = GameObjects.TurbineLeft;
        switch (beacon)
        {
            case Beacon.Left:
                go = GameObjects.TurbineLeft;
                break;
            case Beacon.Center:
                go = GameObjects.TurbineCenter;
                break;
            case Beacon.Right:
                go = GameObjects.TurbineRight;
                break;
        }

        GetObject((int)go).GetComponent<UI_Turbine>().SetTimer(Time);
    }

    #endregion

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

    public void SetScore(int team, int score)
    {
        bool isAlly = false;

        if (Managers.Object.MyPlayer.ObjInfo.Player.Team == team)
            isAlly = true;

        if (isAlly)
            GetObject((int)GameObjects.TeamScore).GetComponent<UI_ScoreBar>().CurrentScore = score;
        else 
            GetObject((int)GameObjects.EnemyScore).GetComponent<UI_ScoreBar>().CurrentScore = score;
    }

    public void AddPlayerBoardToBattleBoard(PlayerController pc)
    {
        GetObject((int)GameObjects.BattleBoard).GetComponent<UI_BattleBoard>().AddPlayer(pc);
    }

    public void UpdateBattleBoard(int id)
    {
        GetObject((int)GameObjects.BattleBoard).GetComponent<UI_BattleBoard>().UpdatePlayerBoard(id);
    }

    public void SetGameResult(bool isVictory)
    {
        GameObject gameResultGo = GetObject((int)GameObjects.GameResult);
        if (gameResultGo == null)
            return;

        gameResultGo.SetActive(true);

        UI_GameResult ui_GameResult = gameResultGo.GetComponent<UI_GameResult>();
        if(ui_GameResult == null) 
            return;

        ui_GameResult.SetMyPlayer();
        ui_GameResult.SetGameResultText(isVictory);
        
        Dictionary<int, GameObject> allies = GetObject((int)GameObjects.BattleBoard).GetComponent<UI_BattleBoard>().Allies;
        foreach(GameObject gameObject in allies.Values)
        {
            PlayerController pc = gameObject.GetComponent<UI_PlayerBoard>().TargetPc;
            if (pc.Id == Managers.Object.MyPlayer.Id)
                continue;

            ui_GameResult.AddAlly(pc);
        }
    }

    public void SetMinimapHealPackImg(int id, bool isActivate)
    {
        GameObject minimap = GetObject((int)GameObjects.Minimap);
        if(minimap == null) 
            return;

        switch (id)
        {
            case 0:
                GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().ChangeHealPackImage(UI_Minimap.Images.HealPackLL, isActivate);
                break;
            case 1:
                GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().ChangeHealPackImage(UI_Minimap.Images.HealPackLR, isActivate);
                break;
            case 2:
                GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().ChangeHealPackImage(UI_Minimap.Images.HealPackRL, isActivate);
                break;
            case 3:
                GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().ChangeHealPackImage(UI_Minimap.Images.HealPackRR, isActivate);
                break;
            case 4:
                GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().ChangeHealPackImage(UI_Minimap.Images.HealPackCL, isActivate);
                break;
            case 5:
                GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().ChangeHealPackImage(UI_Minimap.Images.HealPackCR, isActivate);
                break;
            case 6:
                GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().ChangeHealPackImage(UI_Minimap.Images.HealPackRC, isActivate);
                break;
            case 7:
                GetObject((int)GameObjects.Minimap).GetComponent<UI_Minimap>().ChangeHealPackImage(UI_Minimap.Images.HealPackLC, isActivate);
                break;
        }
    }
}
