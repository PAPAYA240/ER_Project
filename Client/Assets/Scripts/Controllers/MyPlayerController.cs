using Data;
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
        _input.SetPlayer(this);
        
        _view = gameObject.GetOrAddComponent<PlayerViewController>();
    }

    protected override void Init()
    {
        base.Init();

        Camera.main.gameObject.GetOrAddComponent<CameraController>().SetPlayer(gameObject);
        // NavMesh Agent
        _agent = GetComponent<NavMeshAgent>();
        _input.SetAgent(_agent);
    }

    PositionInfo _targetPos;
    private void Update()
    {
        var moveCmd = _input.GetMoveCommand();
        if(moveCmd != null)
            Managers.Network.Send(moveCmd);

        var skillCmd = _input.GetSkillCommand();
        if (skillCmd != null)
            Managers.Network.Send(skillCmd);

        //var attackCmd = _input.GetAttackCommand();
        //if (attackCmd != null)
        //    Managers.Network.Send(attackCmd);

        //var restCmd = _input.GetRestCommand();
        //if (moveCmd != null)
        //    Managers.Network.Send(restCmd);

        //CheckUpdatedFlag();
    }

    // 서버 응답 전달
    //public void OnServerUpdate(S_Idle packet) => _view.OnIdle(packet);
    public void OnServerUpdate(S_Move packet) => _view.OnMove(packet);
    public void OnServerUpdate(S_Skill packet) => _view.OnSkill(packet);
    public void OnServerUpdate(S_Anim packet) => _view.OnAnim(packet);
    public void OnServerUpdate(S_ChangeHp packet) => _view.OnHpChanged(packet);
    public void OnServerUpdate(S_Die packet) => _view.OnDead(packet);
    public void OnServerUpdate(S_Respawn packet) => _view.OnRespawn(packet);

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
            C_Move movePacket = new C_Move();
            movePacket.PosInfo = PosInfo;
            movePacket.RotInfo = RotInfo;
            Managers.Network.Send(movePacket);
            _updated = false;
        }
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
