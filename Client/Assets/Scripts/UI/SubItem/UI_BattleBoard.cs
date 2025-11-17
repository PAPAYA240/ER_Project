using Google.Protobuf;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class UI_BattleBoard : UI_Base
{
    enum GameObjects
    {
        Allies,
        Enemies
    }

    Dictionary<int, GameObject> _allies = new Dictionary<int, GameObject>();
    Dictionary<int, GameObject> _enemies = new Dictionary<int, GameObject>();


    public override void Init()
    {
        
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

    public void AddPlayer(PlayerController pc)
    {
        GameObject go = Managers.Resource.Instantiate("UI/SubItem/PlayerBoard");

        // Allies
        if (Managers.Object.MyPlayer.ObjInfo.Player.Team == pc.ObjInfo.Player.Team)
        {
            go.transform.SetParent(GetObject((int)GameObjects.Allies).transform);
            _alliesCount++;
        }
        // Enemies
        else
        {
            go.transform.SetParent(GetObject((int)GameObjects.Enemies).transform);
            _enemiesCount++;
        }
    }
}
