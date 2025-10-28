using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

public class Player_AttackState : IPlayerState
{
    // ===== 튜닝 파라미터(테이블화 가능) =====
    public const float DefaultAttackRange = 3.0f;   // MyPlayerController 기본값 매칭
    private const float WindupSeconds = 0.20f;      // 선딜(히트 타이밍까지)
    private const float BackswingSeconds = 0.30f;   // 후딜
    private const float ReattackGapSeconds = 0.10f; // 연속 스윙 사이 최소 텀
    private const float ComboResetSeconds = 2.00f;  // 콤보 리셋 타이머

    // 애니메이션(프로젝트 애니 자원명/ID에 맞춰 교체)
    private const string AnimAttackA = "ATTACK_1";
    private const string AnimAttackB = "ATTACK_2";
    private const string AnimRun = "RUN";

    // ===== 상태 필드 =====
    private readonly float _attackRange;
    private int _targetId;
    private bool _chaseAllowed;
    private int? _pendingTargetId;          // 스윙 중 들어온 타겟 변경은 스윙 종료 후 반영

    private bool _swingActive;
    private bool _damageApplied;
    private int _attackIndex;               // 0/1 → A/B 번갈이

    private DateTime _swingStartUtc;
    private DateTime _hitMomentUtc;
    private DateTime _swingEndUtc;
    private DateTime _nextAttackReadyUtc;
    private DateTime _comboResetDeadlineUtc;

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

    public void Enter(Player player)
    {
        player.State = CreatureState.Attack;
        player.SendStatePacket();
        player.SendStopPacket(StopReason.StopMoveOnly);
        _swingActive = false;
        _damageApplied = false;

        var now = DateTime.UtcNow;
        _nextAttackReadyUtc = now;              // 즉시 공격 가능
        _comboResetDeadlineUtc = default;

        StartSwing(player, now);
    }

    public void Execute(Player player)
    {
        if (player == null || player.Room == null || !player.CanAttack())
            return;

        GameObject target = player.FindTarget(_targetId);
        if (target == null || target.State == CreatureState.Dead)
        {
            // 스윙 중이 아니고 pending 타겟이 있으면 교체 후 재시도
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
        bool inRange = Vector3.Distance(pos, targetPos) <= _attackRange;

        var now = DateTime.UtcNow;

        // ===== 스윙 진행 중 =====
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

                // 스윙 종료 후에만 타겟 변경 반영
                if (_pendingTargetId.HasValue)
                {
                    _targetId = _pendingTargetId.Value;
                    _pendingTargetId = null;
                }
            }
            return; // 스윙 중에는 추가 개시 없음
        }

        // ===== 스윙 중이 아님 =====

        // 콤보 리셋
        if (_comboResetDeadlineUtc != default && now >= _comboResetDeadlineUtc)
        {
            _attackIndex = 0; // 다음 스윙은 첫타
            _comboResetDeadlineUtc = default;
        }

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
        if (now >= _nextAttackReadyUtc)
        {
            StartSwing(player, now);
        }
    }

    public void Exit(Player player)
    {
        _swingActive = false;
        _pendingTargetId = null;
    }

    // 외부에서 타겟 변경을 요청할 때 호출(스윙 진행 중이면 종료 후에 반영)
    public void RequestTargetChange(Player p, int newTargetId)
    {
        if (_swingActive)
            _pendingTargetId = newTargetId;
        else
            _targetId = newTargetId;
    }

    // ===== 내부 유틸 =====
    private void StartSwing(Player p, DateTime now)
    {
        _swingActive = true;
        _damageApplied = false;

        _swingStartUtc = now;
        _hitMomentUtc = now.AddSeconds(WindupSeconds);
        _swingEndUtc = _hitMomentUtc.AddSeconds(BackswingSeconds);

        // A/B 번갈이
        string animName = (_attackIndex == 0) ? AnimAttackA : AnimAttackB;
        _attackIndex = 1 - _attackIndex;

        // 애니 송출(서버 권한)
        p.SendAnimPacket(animName, 0.05f);

        // 필요 시 바라보기 보정
        //p.FaceToTarget(_targetId);
    }

    // 데미지 적용 훅(프로젝트 룰에 맞게 연결)
    private void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead)
            return;

        // TODO: 실제 데미지 계산/적용 로직에 연결
        // 예) target.OnDamaged(p, 10f);
    }
}

