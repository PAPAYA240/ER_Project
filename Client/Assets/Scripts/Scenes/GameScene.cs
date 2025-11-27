using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.Protocol;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GameScene : BaseScene
{
    public Dictionary<int, GameObject> Team1 = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> Team2 = new Dictionary<int, GameObject>();

    private HashSet<GameObject> _visibleObjects = new HashSet<GameObject>();

    protected async override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Game;

        GameObject go = await Managers.Resource.InstantiateAsync("Map/Map_Cobalt");
        go.name = "Map";

        Screen.SetResolution(960 , 540, false);

        await Task.Yield();

        LoadingManager.Instance.OnSceneReady();

        GameObject loadingUI = GameObject.Find("LoadingUI");
        loadingUI.SetActive(false);
    }

    private void Update()
    {
        SetVisibleObjects();
    }

    public override void Clear()
    {
        Team1.Clear();
        Team2.Clear();
    }

    public void SetVisibleObjects()
    {
        if (Managers.Object.MyPlayer == null ||Managers.Object.MyPlayer.ObjInfo.Player == null || _visibleObjects == null)
            return;

        Dictionary<int, GameObject> team;

        if (Managers.Info.Team == 1)
        {
            team = Team1;
        }
        else
        {
            team = Team2;
        }

        _visibleObjects.Clear();

        // 팀을 돌면서 obj(팀원)의 시야로 보이는 오브젝트를 추림
        foreach (GameObject obj in team.Values)
        {
            if(null == obj) continue;

            Managers.Object.ResiterVisibleObjects(obj, _visibleObjects);
        }


        // 와드를 돌면서 obj(와드)의 시야로 보이는 오브젝트를 추림
        foreach(int wardId in Managers.Object.MyPlayer.View.WardIds)
        {
            GameObject go = Managers.Object.FindById(wardId);
            if (null == go) continue;

            Managers.Object.ResiterVisibleObjects(go, _visibleObjects, 13f);
        }

        Managers.Object.SetVisibleObjects(_visibleObjects);
    }

    public void AddPlayer(GameObject go, PlayerController pc)
    {
        if (Managers.Info.Team == 1)
        {
            Team1.Add(pc.Id, go);
        }
        else
        {
            Team2.Add(pc.Id, go);
        }
    }
}
