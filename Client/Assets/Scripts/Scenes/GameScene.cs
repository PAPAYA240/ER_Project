using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf.Protocol;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GameScene : BaseScene
{
    public Dictionary<int, GameObject> Team1 = new Dictionary<int, GameObject>();
    public Dictionary<int, GameObject> Team2 = new Dictionary<int, GameObject>();

    private HashSet<GameObject> _visibleObjects = new HashSet<GameObject>();


    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Game;

        GameObject go = Managers.Resource.Instantiate("Map/Map_Cobalt");
        go.name = "Map";

        Screen.SetResolution(960 , 540, false);
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
        Dictionary<int, GameObject> team;

        if (Managers.Object.MyPlayer.ObjInfo.Player.Team == 1)
        {
            team = Team1;
        }
        else
        {
            team = Team2;
        }

        _visibleObjects.Clear();

        foreach (GameObject obj in team.Values)
        {
            if(null == obj || _visibleObjects == null || Managers.Object == null) continue;

            Managers.Object.ResiterVisibleObjects(obj, _visibleObjects);
        }

        Managers.Object.SetVisibleObjects(_visibleObjects);
    }

    public void AddPlayer(GameObject go, PlayerController pc)
    {
        if(go == null)
        {
            Debug.Log("go null : AddPlayer in GameScene");
        }
        else
        {
            Debug.Log("Sucess AddPlayer");
        }

        if (pc.ObjInfo.Player.Team == 1)
        {
            Team1.Add(pc.Id, go);
        }
        else
        {
            Team2.Add(pc.Id, go);
        }
    }
}
