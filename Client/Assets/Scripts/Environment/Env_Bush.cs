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
    enum BushState
    {
        Visible,
        Hidden,
        Translucent
    }
    List<int> _insidePlayersId = new List<int>();


    protected override void Init() => base.Init();

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

        RemoveInsidePlayer(pc.Id);
        if (pc.ObjInfo.Player.CharType == CharacterType.Theodore)   // 테오도르 패시브
        {
            if (pc.ObjInfo.Player.Team != Managers.Object.MyPlayer.ObjInfo.Player.Team)
                pc.BushRenderType((int)BushState.Hidden);
            else
            {
                GameObject effect = Managers.FX.Effect.FindCurrentPlayEffect(pc.Id, "FX_PassiveShideld");
                if (effect != null)
                    Managers.FX.Effect.RemoveEffect(pc.Id, effect);
                pc.PlaySkillEffect(KeyCode.F1, default(Vector3), default(Vector3));
            }
            pc.BushRenderType((int)BushState.Visible, 2.5f);
        }
        else
            pc.BushRenderType((int)BushState.Visible);
    }

    private void RemoveInsidePlayer(int id)
    {
        if (_insidePlayersId.Contains(id))
            _insidePlayersId.Remove(id);

        foreach (int inPlayerId in _insidePlayersId)
        {
            GameObject inGo = Managers.Object.FindById(inPlayerId);
            if (inGo == null)
                continue;
            PlayerController inPc = inGo.GetComponent<PlayerController>();

            if (inPc.ObjInfo.Player.Team == Managers.Object.MyPlayer.ObjInfo.Player.Team)
                inPc.BushRenderType((int)BushState.Translucent);
            else
                inPc.BushRenderType((int)BushState.Hidden);
        }
    }

    #region Interaction
    protected override void TryHandleInteraction(PlayerController target)
    {
        if (!_insidePlayersId.Contains(target.Id))
            _insidePlayersId.Add(target.Id);

        bool isSameTeam = Managers.Object.MyPlayer.ObjInfo.Player.Team == target.ObjInfo.Player.Team;
        bool amIInsideBush = _insidePlayersId.Contains(Managers.Object.MyPlayer.Id);

        BushState targetState;

        if (isSameTeam)
            targetState = BushState.Translucent;

        else if (amIInsideBush)
            targetState = BushState.Translucent;
        else
            targetState = BushState.Hidden;

        target.BushRenderType((int)targetState);

        foreach (int id in _insidePlayersId)
        {
            if (id == target.Id) continue; 

            GameObject inGo = Managers.Object.FindById(id);
            if (inGo == null) continue;

            PlayerController inPc = inGo.GetComponent<PlayerController>();
            if (inPc == null) continue;

            if (Managers.Object.MyPlayer.Id == target.Id)
                inPc.BushRenderType((int)BushState.Translucent);
        }
    }
    #endregion
}
