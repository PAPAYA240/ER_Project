using Google.Protobuf;
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

    public void AddPlayer(PlayerController pc)
    {
        GameObject go = Managers.Resource.Instantiate("UI/SubItem/PlayerBoard");
        UI_PlayerBoard ui = go.GetComponent<UI_PlayerBoard>();
        
        ui.SetTarget(pc);
        ui.SetNameText(pc.NickName);
        ui.UpdatePlayerBoard();

        

        // Allies
        if (Managers.Object.MyPlayer.ObjInfo.Player.Team == pc.ObjInfo.Player.Team)
        {
            GameObject ally = GetObject((int)GameObjects.Allies);

            go.transform.SetParent(ally.transform);
            _allies.Add(pc.Id, go);
        }
        // Enemies
        else
        {
            go.transform.SetParent(GetObject((int)GameObjects.Enemies).transform);
            _enemies.Add(pc.Id, go);
        }
    }

    public void UpdatePlayerBoard(int id)
    {

        if(_allies.TryGetValue(id, out GameObject ally))
        {
            ally.GetComponent<UI_PlayerBoard>().UpdatePlayerBoard();
        }
        else if(_enemies.TryGetValue(id, out GameObject enemy))
        {
            enemy.GetComponent<UI_PlayerBoard>().UpdatePlayerBoard();
        }
    }

    public void Clear()
    {
        _allies.Clear();
        _enemies.Clear();
    }
}
