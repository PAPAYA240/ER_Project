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
    List<int> _insidePlayersId = new List<int>();
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

        RemoveInsidePlayer(pc.Id);

        // 테오도르 패시브
        if (pc.ObjInfo.Player.CharType == CharacterType.Theodore)
        {
            if (pc.ObjInfo.Player.Team != Managers.Object.MyPlayer.ObjInfo.Player.Team)
                pc.ActiveRenderer(false);
            else
            {
                GameObject effect = Managers.FX.Effect.FindCurrentPlayEffect(pc.Id, "FX_PassiveShideld");
                if (effect != null)
                    Managers.FX.Effect.RemoveEffect(pc.Id, effect);
                pc.PlaySkillEffect(KeyCode.F1, default(Vector3), default(Vector3));
            }
            pc.ActiveRenderer(true, 2.5f);
        }
        else
            pc.ActiveRenderer(true); 
    }

    private void RemoveInsidePlayer(int id)
    {
        // 나간 플레이어는 부쉬 내 플레이어를 보지 못함(단, 자신의 팀은 투명화)
        if (_insidePlayersId.Contains(id))
            _insidePlayersId.Remove(id);

        foreach (int inPlayerId in _insidePlayersId)
        {
            GameObject inGo = Managers.Object.FindById(inPlayerId);
            if (inGo == null)
                continue;
            PlayerController inPc = inGo.GetComponent<PlayerController>();

            if (inPc.ObjInfo.Player.Team == Managers.Object.MyPlayer.ObjInfo.Player.Team)
                inPc.ChangeBushRenderer();
            else
                inPc.ActiveRenderer(false);
        }
    }

    #region Interaction
    protected override void TryHandleInteraction(PlayerController target)
    {
        if (!_insidePlayersId.Contains(target.Id))
        {
            _insidePlayersId.Add(target.Id);
        }

        //같은 팀은 투명화
        if (Managers.Object.MyPlayer.ObjInfo.Player.Team == target.ObjInfo.Player.Team)
            target.ChangeBushRenderer();
        // 같은 팀 아니면 시야에서 사라지게
        else
            target.ActiveRenderer(false);

        // 나는 부쉬 내 플레이어를 볼 수 있음 && 부쉬 내 플레이어들도 나를 볼 수 있음
        foreach (int id in _insidePlayersId)
        {
            GameObject inGo = Managers.Object.FindById(id);
            if (inGo == null)
                continue;
            PlayerController inPc = inGo.GetComponent<PlayerController>();
            if (inPc == null)
                continue;

            if (Managers.Object.MyPlayer.Id == target.Id)
                inPc.ChangeBushRenderer();

            else if(Managers.Object.MyPlayer.Id == inPc.Id)
                target.ChangeBushRenderer();
        }
    }
    #endregion
}
