using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public class Rozzi_AttackState : Player_AttackState
{
    protected const float RozziWindupSeconds = 0.05f;      // 선딜(히트 타이밍까지)
    protected const string AnimPassiveAttack = "ATTACK_3";

    private string _curAnimName = AnimAttackA;

    private DateTime _hitMomentUtc2;

    private bool _isPassiveAttack = false;
    private bool _shot1Fired = false;
    private bool _shot2Fired = false;

    private float[] _attackBonus = [0, 0.5f, 0.6f, 0.7f];

    private KeyCode _keyCode = KeyCode.F3;
    private float _projectileSpeed = 10f;

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
                //if ((_damageAppliedTimes < _maxDamageTimes) && now >= _hitMomentUtc)
                //{
                //    if((_damageAppliedTimes == 0) || (_damageAppliedTimes == 1 && now >= _hitMomentUtc2))
                //    {
                //        // 히트 타이밍
                //        float distNow = Vector3.Distance( new Vector3(player.PosInfo.PosX, player.PosInfo.PosY, player.PosInfo.PosZ),
                //                                          new Vector3(target.PosInfo.PosX, target.PosInfo.PosY, target.PosInfo.PosZ));
                //        if (distNow <= player.AttackRange)
                //            ApplyHit(player, target);

                //        _damageAppliedTimes++;
                //    }                 
                //}

                // 1번 샷: 모든 평타 공통
                if (!_shot1Fired && now >= _hitMomentUtc)
                {
                    // 첫 발 – 예: 오른손(혹은 현재 attackIndex 기준)
                    CreateProjectile(player, isLWeapon: (_curAnimName == AnimAttackA)? false : true);
                    _shot1Fired = true;
                }

                // 2번 샷: 패시브 ATTACK_3일 때만
                if (_isPassiveAttack && !_shot2Fired && now >= _hitMomentUtc2)
                {
                    // 두 번째 발 – 예: 왼손
                    CreateProjectile(player, isLWeapon: true);
                    _shot2Fired = true;
                }

                if (now >= _swingEndUtc)
                {
                    _swingActive = false;
                    _shot1Fired = false;
                    _shot2Fired = false;

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
        GameObject target = ObjectManager.Instance.Find(_targetId);
        if (target == null)
            return;

        _swingActive = true;
        _shot1Fired = false;
        _shot2Fired = false;

        _swingStartUtc = now;

        // 첫 번째 샷(기본 히트 타이밍)
        _hitMomentUtc = now.AddSeconds(RozziWindupSeconds / p.AttackSpeed);

        // 기본은 1타 공격
        _isPassiveAttack = false;
        string nextCombo = (_attackIndex == 0) ? AnimAttackA : AnimAttackB;

        if (p.OnAttackPerformed())
            _curAnimName = nextCombo;
        else if (p.TryHandleAttackWithTokens() != null)
        {
            _curAnimName = AnimPassiveAttack;
            _isPassiveAttack = true;

            // 두 번째 샷 타이밍: 첫 샷 이후 0.12초(공속 보정)
            double gap = 0.12 / p.AttackSpeed;
            _hitMomentUtc2 = _hitMomentUtc.AddSeconds(gap);
        }
        else
            _curAnimName = nextCombo;

        if (_curAnimName == AnimAttackA || _curAnimName == AnimAttackB)
            _attackIndex = 1 - _attackIndex;

        // 스윙 종료 시점: 패시브면 두 번째 샷 이후, 아니면 첫 샷 이후
        if (_isPassiveAttack)
            _swingEndUtc = _hitMomentUtc2.AddSeconds(BackswingSeconds / p.AttackSpeed);
        else
            _swingEndUtc = _hitMomentUtc.AddSeconds(BackswingSeconds / p.AttackSpeed);

        // 전투 모드 + 애니 송출 그대로
        {
            p.CombatState = CombatState.Combat;
            S_CombatMode combatModePkt = new S_CombatMode();
            combatModePkt.ObjectId = p.Id;
            combatModePkt.CombatMode = p.CombatState;
            p.Room.Broadcast(combatModePkt);
            p.CombatTime = 0f;
        }

        p.SendAnimPacket(_curAnimName, 0.05f, p.AttackSpeed);
    }

    private void CreateProjectile(Player p, bool isLWeapon)
    {
        Projectile_Rozzi_NormalAttack projectile = ObjectManager.Instance.Add<Projectile_Rozzi_NormalAttack>();
        if (projectile != null)
        {
            projectile.ProjectileType = ProjectileType.ProjectileRozziNormalAttack;
            projectile.Owner = p;
            projectile.Init();
            p.Room.Push(p.Room.EnterGame, projectile, 0);
            projectile.SendRozziNormalAttackPacket(p, _targetId, isLWeapon, _projectileSpeed);
        }
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

        p.SendSkillEffect(new Vector2(target.Position.X, target.Position.Z), keyCode: _keyCode, sendLookatMousePacket: true,
                targetPos: default, targetRot: default,
                type: "Select",  name: "FX_BI_Rozzi_NormalAttack_Hit",
                useTargetTransform: true, targetId: target.Id);
    }

    public void ApplyProjectileHit(Player p, C_RozziNormalAttack pkt)
    {
        if (p == null || pkt == null)
            return;

        if(pkt.HasHit)
        {
            GameObject target = ObjectManager.Instance.Find(pkt.TargetId);
            ApplyHit(p, target);
        }

        p.Room.Push(p.Room.LeaveGame, pkt.ObjectId);
        Console.WriteLine($"@ ApplyProjectileHit");
    }
}
