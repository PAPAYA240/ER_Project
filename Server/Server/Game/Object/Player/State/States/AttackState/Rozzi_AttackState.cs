using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public class Rozzi_AttackState : Player_AttackState
{
    protected const string AnimPassiveAttack = "ATTACK_3";
    private bool _isPassiveAttack = false;

    private int _damageAppliedTimes = 0;
    private int _maxDamageTimes = 1;

    private DateTime _hitMomentUtc2;

    private float[] _attackBonus = [0, 0.5f, 0.6f, 0.7f];

    public Rozzi_AttackState(int targetId, bool chaseAllowed = true) : base(targetId, chaseAllowed) { }

    public override void Execute(Player player)
    {
        if (player == null || player.Room == null || !player.CanAttack())
            return;

        GameObject target = player.FindTarget(_targetId);
        if (target == null || target.State == CreatureState.Dead || target.IsUntargetable())
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
            bool inRange = Vector3.Distance(pos, targetPos) <= player.AttackRange;

            var now = DateTime.UtcNow;

            // ===== 공격 진행 중 =====
            if (_swingActive)
            {
                if ((_damageAppliedTimes < _maxDamageTimes) && now >= _hitMomentUtc)
                {
                    if((_damageAppliedTimes == 0) || (_damageAppliedTimes == 1 && now >= _hitMomentUtc2))
                    {
                        // 히트 타이밍: 서버 거리 검증(위에서 inRange는 프레임 타임이라 다시 체크해도 됨)
                        float distNow = Vector3.Distance(
                            new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ),
                            new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ));
                        if (distNow <= player.AttackRange /* + player.HitTolerance 가능 */)
                            ApplyHit(player, target);

                        _damageAppliedTimes++;
                    }                 
                }

                if (now >= _swingEndUtc)
                {
                    _swingActive = false;
                    _damageAppliedTimes = 0;
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

    protected override void StartSwing(Player p, DateTime now)
    {
        _swingActive = true;
        _damageAppliedTimes = 0;

        _swingStartUtc = now;
        _hitMomentUtc = now.AddSeconds(WindupSeconds / p.AttackSpeed);
        _swingEndUtc = _hitMomentUtc.AddSeconds(BackswingSeconds / p.AttackSpeed);

        string animName = default;
        string nextCombo = (_attackIndex == 0) ? AnimAttackA : AnimAttackB;
        _isPassiveAttack = false;
        _maxDamageTimes = 1;

        // 우선순위: OnAttackPerformed → 토큰 공격 → 기본 A/B
        if (p.OnAttackPerformed())
            animName = nextCombo;
        else if (p.TryHandleAttackWithTokens() != null)
        { 
            animName = AnimPassiveAttack;
            _isPassiveAttack = true;
            _maxDamageTimes = 2;
            _hitMomentUtc2 = _hitMomentUtc.AddSeconds(0.2);
            //_swingEndUtc = _hitMomentUtc2.AddSeconds(0.1);
        }
        else
            animName = nextCombo;

        if (animName == AnimAttackA || animName == AnimAttackB)
            _attackIndex = 1 - _attackIndex;

        // 전투 상태 평타 칠 때마다 갱신
        // 전투 모드
        {
            p.CombatState = CombatState.Combat;
            S_CombatMode combatModePkt = new S_CombatMode();
            combatModePkt.ObjectId = p.Id;
            combatModePkt.CombatMode = p.CombatState;
            p.Room.Broadcast(combatModePkt);
            p.CombatTime = 0f;
        }

        // 애니 송출(서버 권한)
        p.SendAnimPacket(animName, 0.05f/*, p.AttackSpeed, _isPassiveAttack*/);
    }

    protected override void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead)
            return;

        float damage = p.Attack;
        if (_isPassiveAttack)
            damage = p.Attack * (0.6f + _attackBonus[p.GetSkillLevel(KeyCode.T)]);
     
        target.OnDamaged(p, damage, false, true);

        Projectile_Rozzi_R pj = p.Room.FindProjectile(p, ProjectileType.ProjectileRozziR) as Projectile_Rozzi_R;
        if (pj != null && pj.Target != null && pj.Target == target)
            pj.RegisterOwnerHit(isSkillHit: false);
    }
}
