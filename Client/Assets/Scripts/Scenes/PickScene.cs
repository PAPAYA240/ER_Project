using Google.Protobuf.Protocol;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickScene : BaseScene
{
    protected override void Init()
    {
        base.Init();

        SceneType = Define.Scene.Login;
    }

    private void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Managers.Scene.LoadScene(Define.Scene.Game);
        }
    }

    public override void Clear()
    {
        Debug.Log("PickScene Clear");
    }
}
