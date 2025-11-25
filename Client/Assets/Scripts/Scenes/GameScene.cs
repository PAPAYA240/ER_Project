using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf.Protocol;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class GameScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Game;

        GameObject go = Managers.Resource.Instantiate("Map/Map_Cobalt");
        go.name = "Map";

        Screen.SetResolution(960 , 540, false);
    }

    public override void Clear()
    {
        
    }
}
