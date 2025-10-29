using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GameScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Game;

        Managers.Map.LoadMap("Cobalt");

        Screen.SetResolution(960 , 540, false);

        // 서버로 패킷 보내기
        C_EnterGame EnterGamePacket = new C_EnterGame();
        //EnterGamePacket.DummyPlayers.Add(CharacterType.Rozzi);
        //EnterGamePacket.DummyPlayers.Add(CharacterType.Yuki);
        //EnterGamePacket.DummyPlayers.Add(CharacterType.Hyunwoo);
        //EnterGamePacket.DummyPlayers.Add(CharacterType.Abigail);
        //EnterGamePacket.DummyPlayers.Add(CharacterType.Theodore);
        Managers.Network.Send(EnterGamePacket);


        //GameObject player = Managers.Resource.Instantiate("Creature/Player");
        //player.name = "Player";
        //Managers.Object.Add(player);


        //Managers.UI.ShowSceneUI<UI_Inven>();
        //Dictionary<int, Data.Stat> dict = Managers.Data.StatDict;
        //gameObject.GetOrAddComponent<CursorController>();

        //GameObject player = Managers.Game.Spawn(Define.WorldObject.Player, "UnityChan");
        //Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(player);

        ////Managers.Game.Spawn(Define.WorldObject.Monster, "Knight");
        //GameObject go = new GameObject { name = "SpawningPool" };
        //SpawningPool pool = go.GetOrAddComponent<SpawningPool>();
        //pool.SetKeepMonsterCount(2);
    }

    public override void Clear()
    {
        
    }
}
