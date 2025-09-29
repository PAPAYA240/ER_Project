using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Text;

public class Player_AttackState : IPlayerState
{
    // === 튜닝 포인트(프로젝트 수치/테이블로 대체 가능) ===
    public const float DefaultAttackRange = 3.0f;   // 클라 _attackRange 기본값 매칭
    private const float WindupSeconds = 0.20f;  // 선딜
    private const float BackswingSeconds = 0.30f;  // 후딜
    private const float ComboResetSeconds = 2.00f;  // 클라 CoComboResetTimer와 동일
    private const float ReattackGap = 0.10f;  // 연속 스윙 사이 최소 간격

    private const int AnimAttackA = 1001; // 실제 프로젝트 애니 ID/이름으로 교체
    private const int AnimAttackB = 1002;

    private readonly float _attackRange;
    private int _currentTargetId;
    private int? _pendingTargetId;

    private bool _swingActive;
    private bool _damageApplied;
    private bool _comboToggle;           // A/B 번갈이
    private int _attackIndex;           // 0/1 (콤보 리셋 시 0으로)

    private DateTime _swingStartUtc;
    private DateTime _hitMomentUtc;
    private DateTime _swingEndUtc;
    private DateTime _nextAttackReadyUtc;
    private DateTime _comboResetDeadlineUtc;

    public Player_AttackState(int targetId = -99, float attackRange = DefaultAttackRange)
    {
        _currentTargetId = targetId;
        _attackRange = Math.Max(0.1f, attackRange);
    }

    public void Enter(Player player)
    {
        player.State = CreatureState.Attack;
        _swingActive = false;
        _damageApplied = false;
        _nextAttackReadyUtc = DateTime.UtcNow;       // 즉시 가능
        // 콤보 유지: 이전 인스턴스에서 이어가고 싶다면 p에 저장/복원 로직 추가 가능
    }

    public void Execute(Player player)
    {
        if (player == null || player.Room == null || !player.CanAttack())
        {
            player?.ChangeState(new Player_IdleState());
            return;
        }

        GameObject target = player.FindTarget(_currentTargetId);
        if(target == null || target.State == CreatureState.Dead)
        {
            if (_swingActive == false && _pendingTargetId.HasValue)
            {
                _currentTargetId = _pendingTargetId.Value;
                _pendingTargetId = null;
                target = player.FindTarget(_currentTargetId);
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

        float dist = player.PosInfo.Distance(target.PosInfo);
        bool inRange = dist <= _attackRange;
        var now = DateTime.UtcNow;

        // === 스윙 진행 중 ===
        if (_swingActive)
        {
            if (!_damageApplied && now >= _hitMomentUtc)
            {
                //ApplyHit(p, target);
                //_damageApplied = true;
            }

            if (now >= _swingEndUtc)
            {
                // 스윙 종료
                _swingActive = false;
                _damageApplied = false;
                _nextAttackReadyUtc = now.AddSeconds(ReattackGap);
                _comboResetDeadlineUtc = now.AddSeconds(ComboResetSeconds);

                // 스윙 끝났으니 pending 타겟 교체
                if (_pendingTargetId.HasValue)
                {
                    _currentTargetId = _pendingTargetId.Value;
                    _pendingTargetId = null;
                }
            }

            return; // 스윙 중에는 추가 개시 없음
        }
    }

    public void Exit(Player player)
    {

    }
}

