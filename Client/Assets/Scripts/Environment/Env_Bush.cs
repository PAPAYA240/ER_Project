using Google.Protobuf.Protocol;
using System;
using System.Collections;
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
    Dictionary<int, Coroutine> _delayedVisibleCoroutines = new Dictionary<int, Coroutine>(); 

    [SerializeField] public BoxCollider _bushCollider;
    protected override void Init() => base.Init();

    void FixedUpdate()
    {
        CheckBushStatus();
    }
    private void CheckBushStatus()
    {
        Vector3 center = transform.TransformPoint(_bushCollider.center);
        Vector3 halfExtents = Vector3.Scale(_bushCollider.size / 2f, transform.lossyScale);
        Quaternion rotation = transform.rotation;

        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, rotation, ~0);
        List<int> currentInsidePlayersId = new List<int>();
        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent<PlayerController>(out PlayerController pc))
            {
                currentInsidePlayersId.Add(pc.Id);
            }
        }

        // 나간 플레이어 처리
        for (int i = _insidePlayersId.Count - 1; i >= 0; i--)
        {
            int oldId = _insidePlayersId[i];
            if (!currentInsidePlayersId.Contains(oldId))
            {
                GameObject exGo = Managers.Object.FindById(oldId);
                if (exGo != null && exGo.TryGetComponent<PlayerController>(out PlayerController exPc))
                {
                    BushExitRender(exPc);
                }
            }
        }

        // 새로 들어온 플레이어 처리
        foreach (int newId in currentInsidePlayersId)
        {
            if (!_insidePlayersId.Contains(newId))
            {
                GameObject newGo = Managers.Object.FindById(newId);
                if (newGo != null && newGo.TryGetComponent<PlayerController>(out PlayerController newPc))
                {

                    BushEnterRender(newPc);
                }
            }
        }

        _insidePlayersId = currentInsidePlayersId; 
    }
    private void BushExitRender(PlayerController pc)
    {
        if (pc.ObjInfo.Player.CharType == CharacterType.Theodore)
        {
            bool isEnemyTeam = pc.ObjInfo.Player.Team != Managers.Object.MyPlayer.ObjInfo.Player.Team;
            if (isEnemyTeam)
            {
                pc.BushRenderType((int)BushState.Hidden);

                if (_delayedVisibleCoroutines.ContainsKey(pc.Id))
                    StopCoroutine(_delayedVisibleCoroutines[pc.Id]);
                _delayedVisibleCoroutines[pc.Id] = StartCoroutine(DelayedVisible(pc, 2.5f));
            }
            else
            {
                GameObject effect = Managers.FX.Effect.FindCurrentPlayEffect(pc.Id, "FX_PassiveShideld");
                if (effect != null)
                    Managers.FX.Effect.RemoveEffect(pc.Id, effect);

                pc.PlaySkillEffect(KeyCode.F1, default(Vector3), default(Vector3));
                pc.BushRenderType((int)BushState.Translucent);

                if (_delayedVisibleCoroutines.ContainsKey(pc.Id))
                    StopCoroutine(_delayedVisibleCoroutines[pc.Id]);
                _delayedVisibleCoroutines[pc.Id] = StartCoroutine(DelayedVisible(pc, 2.5f));
            }
        }
        else
            pc.BushRenderType((int)BushState.Visible);

        UpdateRemainingPlayersRender();
    }

    private IEnumerator DelayedVisible(PlayerController pc, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_delayedVisibleCoroutines.ContainsKey(pc.Id))
            _delayedVisibleCoroutines.Remove(pc.Id);

        pc.BushRenderType((int)BushState.Visible);
    }

    private void UpdateRemainingPlayersRender()
    {
        foreach (int inPlayerId in _insidePlayersId)
        {
            GameObject inGo = Managers.Object.FindById(inPlayerId);
            if (inGo == null) continue;

            PlayerController inPc = inGo.GetComponent<PlayerController>();
            if (inPc.ObjInfo.Player.Team == Managers.Object.MyPlayer.ObjInfo.Player.Team)
                inPc.BushRenderType((int)BushState.Translucent);
            else
                inPc.BushRenderType((int)BushState.Hidden);
        }
    }

    #region Interaction
    private void BushEnterRender(PlayerController target)
    {
        if (_delayedVisibleCoroutines.ContainsKey(target.Id))
        {
            StopCoroutine(_delayedVisibleCoroutines[target.Id]);
            _delayedVisibleCoroutines.Remove(target.Id);

            GameObject effect = Managers.FX.Effect.FindCurrentPlayEffect(target.Id, "FX_PassiveShideld");
            if (effect != null)
                Managers.FX.Effect.RemoveEffect(target.Id, effect);
        }

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
