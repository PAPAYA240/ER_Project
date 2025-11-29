using Google.Protobuf.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Analytics.IAnalytic;

public class Env_Bush : EnvController
{
    protected override void Init()
    {
        base.Init();
    }

    protected override void OnTriggerEnter(Collider other)
    {
        PlayerController pc = other.gameObject.GetComponent<PlayerController>();
        if (pc == null)
            return ;

        RequestCollect(other.gameObject.GetComponent<PlayerController>());
    }
    protected override void OnTriggerExit(Collider other)
    {
        PlayerController pc = other.gameObject.GetComponent<PlayerController>();
        if (pc == null)
            return;

        // 테오도르 패시브
        if(pc.ObjInfo.Player.CharType == CharacterType.Theodore)
        {
            if (pc.Id == Managers.Object.MyPlayer.Id)
            {
                // 호출 중이라면 삭제 후 재호출
                GameObject effect = Managers.FX.Effect.FindEffect(pc.Id, "FX_PassiveShideld");
                if (effect != null)
                    Managers.FX.Effect.RemoveEffect(pc.Id, effect);

                pc.PlaySkillEffect(KeyCode.F1, default(Vector3), default(Vector3));
            }

            StartCoroutine(pc.MakeVisible(2.5f)); 
        }
        else
            StartCoroutine(pc.MakeVisible());
    }
    #region Interaction
    protected override void TryHandleInteraction(PlayerController target)
    {
        if (Managers.Object.MyPlayer.Id == target.Id)
        {
            target.ChangeBushRenderer();
        }
        else
        {
            target.MakeInvisible();
        }
    }
    #endregion
}
