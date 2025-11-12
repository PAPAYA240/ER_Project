using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class Player_AttackState : IPlayerState, IReceivesAttackCommand
{
    // ===== 튜닝 파라미터(테이블화 가능) =====
    public const float DefaultAttackRange = 3.0f;   // MyPlayerController 기본값 매칭
    protected const float WindupSeconds = 0.20f;      // 선딜(히트 타이밍까지)
    protected const float BackswingSeconds = 0.30f;   // 후딜
    private const float ReattackGapSeconds = 0.10f; // 연속 스윙 사이 최소 텀
    private const float ComboResetSeconds = 2.00f;  // 콤보 리셋 타이머

    // 애니메이션(프로젝트 애니 자원명/ID에 맞춰 교체)
    protected const string AnimAttackA = "ATTACK_1";
    protected const string AnimAttackB = "ATTACK_2";

    // ===== 상태 필드 =====
    protected readonly float _attackRange;
    protected int _targetId;
    protected bool _chaseAllowed;
    protected int? _pendingTargetId;          // 스윙 중 들어온 타겟 변경은 스윙 종료 후 반영

    protected bool _swingActive;
    protected bool _damageApplied;
    protected int _attackIndex;               // 0/1 → A/B 번갈이

    // 회전
    private bool _isRotate = false;

    protected DateTime _swingStartUtc;
    protected DateTime _hitMomentUtc;
    protected DateTime _swingEndUtc;
    private DateTime _nextAttackReadyUtc;
    private DateTime _comboResetDeadlineUtc;

    // 데미지

    public Player_AttackState(int targetId, bool chaseAllowed = true, float attackRange = DefaultAttackRange)
    {
        _targetId = targetId;
        _attackRange = MathF.Max(0.1f, attackRange);

        _chaseAllowed = chaseAllowed;
    }

    // 외부에서 추격 허용 변경(H키)
    public void SetChaseAllowed(bool allowed) => _chaseAllowed = allowed;

    // 공격 중 타겟 교체(사거리 내 우클릭)
    public void ChangeTarget(int newTargetId) => _targetId = newTargetId;

    public bool IsLookingAtTargetYawOnly(Vector3 myPos, Quaternion myRot, Vector3 targetPos, float toleranceDeg = 5f)
    {
        // 쿼터니언에서 forward 꺼내기
        Vector3 forward = Vector3.Transform(Vector3.UnitZ, myRot); // Z를 forward로 쓴다고 가정
        forward.Y = 0;
        forward = Vector3.Normalize(forward);

        Vector3 toTarget = targetPos - myPos;
        toTarget.Y = 0;
        toTarget = Vector3.Normalize(toTarget);

        float dot = Vector3.Dot(forward, toTarget);
        float cos = MathF.Cos(toleranceDeg * MathF.PI / 180f);
        return dot >= cos;
    }

    public void Enter(Player player)
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

        //StartSwing(player, now);
    }

    public void Execute(Player player)
    {
        if (player == null || player.Room == null || !player.CanAttack())
            return;

        GameObject target = player.FindTarget(_targetId);
        if (target == null || target.State == CreatureState.Dead)
        {
            // 공격 중이 아니고 pending 타겟이 있으면 교체 후 재시도
            if (!_swingActive && _pendingTargetId.HasValue)
            {
                _targetId = _pendingTargetId.Value;
                _pendingTargetId = null;
                target = player.FindTarget(_targetId);
                if (target == null || target.State == CreatureState.Dead)
                {
                    player.ChangeState(new Player_IdleState());
                    return;
                }
            }
            else
            {
                player.ChangeState(new Player_IdleState());
                return;
            }
        }

        // 거리 판정
        Vector3 pos = new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ);
        Vector3 targetPos = new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ);

        if (_isRotate == true)
        {
            if (IsLookingAtTargetYawOnly(player.Position, new Quaternion(player.RotInfo.Qx, player.RotInfo.Qy, player.RotInfo.Qz, player.RotInfo.Qw), targetPos))
            {
                _isRotate = false;
            }
        }
        else
        {
            bool inRange = Vector3.Distance(pos, targetPos) <= _attackRange;

            var now = DateTime.UtcNow;

            // ===== 공격 진행 중 =====
            if (_swingActive)
            {
                if (!_damageApplied && now >= _hitMomentUtc)
                {
                    // 히트 타이밍: 서버 거리 검증(위에서 inRange는 프레임 타임이라 다시 체크해도 됨)
                    float distNow = Vector3.Distance(
                        new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ),
                        new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ));
                    if (distNow <= _attackRange /* + player.HitTolerance 가능 */)
                        ApplyHit(player, target);

                    _damageApplied = true;
                }

                if (now >= _swingEndUtc)
                {
                    _swingActive = false;
                    _damageApplied = false;
                    _nextAttackReadyUtc = now.AddSeconds(ReattackGapSeconds);
                    _comboResetDeadlineUtc = now.AddSeconds(ComboResetSeconds);

                    // 공격 종료 후에만 타겟 변경 반영
                    if (_pendingTargetId.HasValue)
                    {
                        _targetId = _pendingTargetId.Value;
                        _pendingTargetId = null;

                        // 타겟 변경 시 회전 패킷
                        S_TargetChange pkt = new S_TargetChange();
                        pkt.TargetId = _targetId;
                        player.SendTargetChangePacket(pkt);
                        _isRotate = true;
                    }
                }
                return; // 스윙 중에는 추가 개시 없음
            }

            // ===== 공격 중이 아님 =====
            //// 콤보 리셋
            //if (_comboResetDeadlineUtc != default && now >= _comboResetDeadlineUtc)
            //{
            //    _attackIndex = 0; // 다음 스윙은 첫타
            //    _comboResetDeadlineUtc = default;
            //}

            // 사거리 밖
            if (!inRange)
            {
                // CHANGED: H키 이후 추격 금지 모드면, 자리 지키기 → 범위 밖이면 종료
                if (!_chaseAllowed)
                {
                    player.ChangeState(new Player_IdleState());
                    return;
                }

                // 기존 이동 상태 재사용(타겟 추격)
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
                return;
            }

            // 사거리 내 + 다음 타 가능 → 스윙 개시
            if (now >= _nextAttackReadyUtc && _isRotate == false)
            {
                StartSwing(player, now);
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
        _hitMomentUtc = now.AddSeconds(WindupSeconds);
        _swingEndUtc = _hitMomentUtc.AddSeconds(BackswingSeconds);

        // A/B 번갈이
        string animName = (_attackIndex == 0) ? AnimAttackA : AnimAttackB;
        _attackIndex = 1 - _attackIndex;

        // 전투 상태 평타 칠 때마다 갱신
        p.CombatState = CombatState.Combat;
        p.CombatTime = 0f;

        // 유키 단추
        if (p.Info.Player.CharType == CharacterType.Yuki)
        {
            if (p.YukiStud > 0)
                p.YukiStud--;
        }

        // 애니 송출(서버 권한)
        p.SendAnimPacket(animName, 0.05f);
 
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

    public static Player_AttackState CreateAttackState(Player p, int targetId, bool chaseAllowed = true, float attackRange = DefaultAttackRange)
    {
        if (p.Info.Player.CharType == CharacterType.Abigail)
            return new Abigail_AttackState(targetId, chaseAllowed, attackRange);
        else if (p.Info.Player.CharType == CharacterType.Theodore)
            return new Theodore_AttackState(targetId, chaseAllowed, attackRange);
        else if (p.Info.Player.CharType == CharacterType.Yuki)
            return new Yuki_AttackState(targetId, chaseAllowed, attackRange);
        else if (p.Info.Player.CharType == CharacterType.Hyunwoo)
            return new Hyunwoo_AttackState(targetId, chaseAllowed, 1.66f);
  

        return new Player_AttackState(targetId, chaseAllowed, attackRange);
    }
}

