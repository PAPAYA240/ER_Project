using Google.Protobuf;
using UnityEngine;

public class UI_BattleBoard : UI_Base
{
    enum GameObjects
    {
        Allies,
        Enemies
    }

    private int _alliesCount;
    private int _enemiesCount;


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
