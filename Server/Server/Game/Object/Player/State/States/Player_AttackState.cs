using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public class Player_AttackState : IPlayerState, IReceivesAttackCommand
{
    // ===== 튜닝 파라미터(테이블화 가능) =====
    protected const float WindupSeconds = 0.20f;      // 선딜(히트 타이밍까지)
    protected const float BackswingSeconds = 0.30f;   // 후딜
    protected const float ReattackGapSeconds = 0.10f; // 연속 스윙 사이 최소 텀
    protected const float ComboResetSeconds = 2.00f;  // 콤보 리셋 타이머

    // 애니메이션(프로젝트 애니 자원명/ID에 맞춰 교체)
    protected const string AnimAttackA = "ATTACK_1";
    protected const string AnimAttackB = "ATTACK_2";

    // ===== 상태 필드 =====
    //public int TargetId => TargetId;
    public int _targetId;
    protected bool _chaseAllowed;
    protected int? _pendingTargetId;          // 스윙 중 들어온 타겟 변경은 스윙 종료 후 반영

    protected bool _swingActive;
    protected bool _damageApplied;
    protected int _attackIndex;               // 0/1 → A/B 번갈이

    // 회전
    protected bool _isRotate = false;

    protected DateTime _swingStartUtc;
    protected DateTime _hitMomentUtc;
    protected DateTime _swingEndUtc;
    protected DateTime _nextAttackReadyUtc;
    protected DateTime _comboResetDeadlineUtc;
    protected DateTime _rotateStartUtc;

    // 데미지

    public Player_AttackState(int targetId, bool chaseAllowed = true)
    {
        _targetId = targetId;

        _chaseAllowed = chaseAllowed;
    }

    // 외부에서 추격 허용 변경(H키)
    public void SetChaseAllowed(bool allowed) => _chaseAllowed = allowed;

    // 공격 중 타겟 교체(사거리 내 우클릭)
    public void ChangeTarget(int newTargetId) => _targetId = newTargetId;

    public bool IsLookingAtTargetYawOnly(Vector3 myPos, Quaternion myRot, Vector3 targetPos, float toleranceDeg = 5f)
    {
        // forward 계산
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, myRot);
        forward.Y = 0;

        if (forward.LengthSquared() < 0.0001f)
            return false;

        forward = Vector3.Normalize(forward);

        // target 방향
        Vector3 toTarget = targetPos - myPos;
        toTarget.Y = 0;

        if (toTarget.LengthSquared() < 0.0001f)
            return false;

        toTarget = Vector3.Normalize(toTarget);

        float dot = Vector3.Dot(forward, toTarget);

        if (float.IsNaN(dot))
            return false;

        float cos = MathF.Cos(toleranceDeg * MathF.PI / 180f);
        return dot >= cos;
    }

    public virtual void Enter(Player player)
    {
        player.SendStopPacket(StopReason.StopMoveOnly);
        _swingActive = false;
        _damageApplied = false;

        // 최초 타겟 변경 시 회전 패킷
        S_TargetChange pkt = new S_TargetChange();

        pkt.TargetId = _targetId;
        player.SendTargetChangePacket(pkt);
        _isRotate = true;

        var now = DateTime.UtcNow;
        _nextAttackReadyUtc = now;              // 즉시 공격 가능
        _comboResetDeadlineUtc = default;
    }


    public virtual void Execute(Player player)
    {
        if (player == null || player.Room == null || !player.CanAttack())
            return;

        // Target resolve
        GameObject target = TryResolveTarget(player);
        if (target == null)
        {
            player.ChangeState(new Player_IdleState());
            return;
        }

        Vector3 pos = player.Position;

        Vector3 targetPos = target.Position;
        float dist = Vector3.Distance(pos, targetPos);
        bool inRange = dist <= player.AttackRange;

        var now = DateTime.UtcNow;

        // 공격 중이면 스윙 처리만
        if (_swingActive)
        {
            UpdateSwing(player, now, target);
            return;
        }

        // 회전 중 공격은 막지 않음
        if (_isRotate)
        {
            UpdateRotation(player, inRange, targetPos);
            // 회전 중이라도 공격 가능하므로 여기서 return 해도 공격 개시 로직은 아래에서 처리됨
        }

        // 공격 및 회전이 끝났거나 회전 중이지만 공격 가능
        // 사거리 밖
        if (!inRange)
        {
            HandleOutOfRange(player, targetPos);
            return;
        }

        // 사거리 안이고 공격 가능
        if (now >= _nextAttackReadyUtc)
        {
            StartSwing(player, now);
            return;
        }
    }

    private GameObject TryResolveTarget(Player player)
    {
        GameObject target = player.FindTarget(_targetId);

        if (target == null || target.State == CreatureState.Dead || target.IsUntargetable() || SameMonsterTeam(target, player))
        {
            if (!_swingActive && _pendingTargetId.HasValue)
            {
                _targetId = _pendingTargetId.Value;
                _pendingTargetId = null;

                target = player.FindTarget(_targetId);
                if (target == null || target.State == CreatureState.Dead)
                    return null;

                _isRotate = true;
                _rotateStartUtc = DateTime.UtcNow;
                return target;
            }

            return null;
        }

        return target;
    }

    private void UpdateRotation(Player player, bool inRange, Vector3 targetPos)
    {
        // 사거리 밖 → 회전 종료 & 이동
        if (!inRange)
        {
            _isRotate = false;
            HandleOutOfRange(player, targetPos);
            return;
        }

        // 사거리 안이면 회전 유지 또는 완료
        Quaternion rot = new Quaternion(player.RotInfo.Qx, player.RotInfo.Qy, player.RotInfo.Qz, player.RotInfo.Qw);
        if (IsLookingAtTargetYawOnly(player.Position, rot, targetPos))
            _isRotate = false;
    }

    private void HandleOutOfRange(Player player, Vector3 targetPos)
    {
        if (!_chaseAllowed)
        {
            player.ChangeState(new Player_IdleState());
            return;
        }

        var move = new C_Move
        {
            IsTargetOn = true,
            TargetId = _targetId,
            TargetPosition = new PositionInfo
            {
                PosX = targetPos.X,
                PosY = targetPos.Y,
                PosZ = targetPos.Z
            }
        };

        player.ChangeState(new Player_MovingState(move));
    }

    private void UpdateSwing(Player player, DateTime now, GameObject target)
    {
        if (!_damageApplied && now >= _hitMomentUtc)
        {
            //float distNow = Vector3.Distance(player.Position, target.Position);
            //if (distNow <= player.AttackRange)
            ApplyHit(player, target);

            _damageApplied = true;
        }

        if (now >= _swingEndUtc)
        {
            _swingActive = false;
            _damageApplied = false;

            _nextAttackReadyUtc = now.AddSeconds(ReattackGapSeconds);
            _comboResetDeadlineUtc = now.AddSeconds(ComboResetSeconds);

            if (_pendingTargetId.HasValue)
            {
                _targetId = _pendingTargetId.Value; // 여기
                _pendingTargetId = null;

                S_TargetChange pkt = new S_TargetChange { TargetId = _targetId };
                player.SendTargetChangePacket(pkt);

                _isRotate = true;
                _rotateStartUtc = DateTime.UtcNow;
            }
        }
    }

    public virtual void Exit(Player player)
    {
        _swingActive = false;
        _pendingTargetId = null;
    }

    // 외부에서 타겟 변경을 요청할 때 호출(스윙 진행 중이면 종료 후에 반영)
    public void RequestTargetChange(int newTargetId)
    {
        if (_swingActive)
            _pendingTargetId = newTargetId;
        else
            _targetId = newTargetId;
    }

    // ===== 내부 유틸 =====
    protected virtual void StartSwing(Player p, DateTime now)
    {
        _swingActive = true;
        _damageApplied = false;

        _swingStartUtc = now;
        _hitMomentUtc = now.AddSeconds(WindupSeconds / p.AttackSpeed);
        _swingEndUtc = _hitMomentUtc.AddSeconds(BackswingSeconds / p.AttackSpeed);

        // A/B 번갈이
        string animName = (_attackIndex == 0) ? AnimAttackA : AnimAttackB;
        _attackIndex = 1 - _attackIndex;

        // 전투 상태 평타 칠 때마다 갱신
        // 전투 모드
        {
            p.CombatState = CombatState.Combat;
            S_CombatMode combatModePkt = new S_CombatMode();
            combatModePkt.CombatMode = p.CombatState;
            p.Room.Push(p.Session.Send, combatModePkt);
            p.CombatTime = 0f;
        }

        // 애니 송출(서버 권한)
        p.SendAnimPacket(animName, 0.05f, p.AttackSpeed, true);

        //p.FaceToTarget(_targetId);
    }

    // 데미지 적용 훅(프로젝트 룰에 맞게 연결)
    protected virtual void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead)
            return;

        target.OnDamaged(p, p.Attack, false, true);
    }

    public bool IsSwingActive() { return _swingActive; }

    public static Player_AttackState CreateAttackState(Player p, int targetId, bool chaseAllowed = true)
    {
        if (p.Info.Player.CharType == CharacterType.Abigail)
            return new Abigail_AttackState(targetId, chaseAllowed);
        else if(p.Info.Player.CharType == CharacterType.Theodore)
            return new Theodore_AttackState(targetId, chaseAllowed);
        else if (p.Info.Player.CharType == CharacterType.Yuki)
            return new Yuki_AttackState(targetId, chaseAllowed);
        else if (p.Info.Player.CharType == CharacterType.Hyunwoo)
            return new Hyunwoo_AttackState(targetId, chaseAllowed);
        else if (p.Info.Player.CharType == CharacterType.Rozzi)
            return new Rozzi_AttackState(targetId, chaseAllowed);

        return new Player_AttackState(targetId, chaseAllowed);
    }
    private bool SameMonsterTeam(GameObject target, Player player)
    {
        if (target is Monster monster)
        {
            if (monster.MonsterTeam == player.Team)
                return true;
        }
        return false;
    }
}

