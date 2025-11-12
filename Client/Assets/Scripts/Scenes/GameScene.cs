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
        Managers.Network.Send(EnterGamePacket);
    }

    public override void Clear()
    {
        
    }
}
