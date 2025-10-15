using Data;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UI_PlayerInterface;
using static UI_SkillBase;
using static UnityEngine.GraphicsBuffer;

public class MyPlayerController : PlayerController
{
    private PlayerInputController _input;
    private PlayerViewController _view;

    private void Awake()
    {
        _input = gameObject.GetOrAddComponent<PlayerInputController>();      
        _view = gameObject.GetOrAddComponent<PlayerViewController>();
    }

    protected override void Init()
    {
        base.Init();
        ObjectType = Define.Object.MyPlayer;
        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);
    }

    private void Update()
    {
        // 1) 정지(S/H)
        var stopCmd = _input.GetStopCommand();
        if (stopCmd != null)
            Managers.Network.Send(stopCmd);

        // 2) 우클릭 "타겟 공격" 의도
        var atkCmd = _input.GetAttackCommand();
        if (atkCmd != null)
            Managers.Network.Send(atkCmd);

        // 3) 우클릭 유지: 타겟 이동 or 땅 이동
        var setMove = _input.GetSetMoveTarget();
        if (setMove != null)
        {
            _view.ApplyLocalSetMoveTarget(setMove);
            Managers.Network.Send(setMove);
        }

        var skillCmd = _input.GetSkillCommand();
        if (skillCmd != null)
            Managers.Network.Send(skillCmd);

        //var restCmd = _input.GetRestCommand();
        //if (moveCmd != null)
        //    Managers.Network.Send(restCmd);

        CheckUpdatedFlag();
    }

    // 서버 응답 전달
    //public void OnServerUpdate(S_Idle packet) => _view.OnIdle(packet);
    public void OnServerUpdate(S_Move packet) => _view.OnMove(packet);
    public void OnServerUpdate(S_SkillMotion packet) => _view.OnSkill(packet);
    public void OnServerUpdate(S_Anim packet) => _view.OnAnim(packet);
    public void OnServerUpdate(S_ChangeHp packet) => _view.OnHpChanged(packet);
    public void OnServerUpdate(S_Die packet) => _view.OnDead(packet);
    public void OnServerUpdate(S_Respawn packet) => _view.OnRespawn(packet);
    public void OnServerUpdate(S_SetMoveTarget packet)
    {
        // 서버가 내려준 의도 그대로 로컬 네비 실행
        _view.ApplyLocalSetMoveTarget(new C_SetMoveTarget
        {
            IsGround = packet.IsGround,
            TargetId = packet.TargetId,
            TargetPos = packet.TargetPos != null ? new PositionInfo(packet.TargetPos) : null
        });
    }
    public void OnServerUpdate(S_Stop packet) => _view.OnStop(packet);

    public void UpdateTransform(bool isWarp = false)
    {
        CellPos = transform.position;
        RotInfo = transform.rotation;
        _updated = true;
    }

    protected override void CheckUpdatedFlag()
    {
        if (_updated)
        {
            C_MoveSync syncPacket = new C_MoveSync();
            syncPacket.PosInfo = PosInfo;
            syncPacket.RotInfo = RotInfo;
            Managers.Network.Send(syncPacket);
            _updated = false;
        }
    }

    public void SendPacket(IMessage packet)
    {
        Managers.Network.Send(packet);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    protected KeyCode _keyCode = KeyCode.None;
    protected bool _isUseSkill = false;
    protected float _attackRange = 3.0f; // Temp
    protected virtual void UpdateSkillKeyInput() { }
    protected GameObject TryGetAttackableObject(float radius = 0.1f) { return null; }
    protected int SkillTargetId { get; set; }
    protected void SetSkillInput(KeyCode keyCode) { }
    protected void SetMovementState() { }
    protected void SendFXPacket(KeyCode key) { }
    protected virtual void ResetCharacterState() { }
    public virtual void OnSkillConfirmed(S_Skill skillPacket) { }
    protected virtual void GetMouseInput(int mouseButton) { }
    protected void ResetTarget() { }
    protected void ResetCoroutine(Coroutine coroutine) { }
    public HashSet<int> VisibleObjectIds { get; set; } = new HashSet<int>();
    protected void LookAtTarget(Vector3 targetPos, bool snapToTarget = false, float speed = 20.0f) { }
    protected void LookAtMouse() { }
    protected Vector3 GetTargetPos(float range, bool isMaxDistance = true) { return Vector3.zero; }
    protected Vector3 GetReachablePosition(Vector3 startPos, Vector3 targetPos, out NavMeshHit navHit) { navHit = new NavMeshHit();  return Vector3.zero;  }
    protected Vector3 GetCursorPos() { return Vector3.zero; }
    protected float GetCurrentAnimClipLength() { return 0f; }
    public void UpdateLevel() { }
    public UI_PlayerInterface PlayerInterface { get; protected set; }
}
