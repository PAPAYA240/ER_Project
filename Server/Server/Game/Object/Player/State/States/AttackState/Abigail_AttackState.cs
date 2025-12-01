using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public class Abigail_AttackState : Player_AttackState
{
    private const string AnimAttackT = "SKILL_T";
    KeyCode _keyCode = KeyCode.T;
    bool IsPassiveAttack = false;

    public Abigail_AttackState(int targetId, bool chaseAllowed = true) : base(targetId, chaseAllowed)
    {
    }

    public override void Enter(Player player)
    {
        base.Enter(player);

        CheckPassive(player);
    }

    protected override void StartSwing(Player p, DateTime now)
    {
        _swingActive = true;
        _damageApplied = false;

        _swingStartUtc = now;
        _hitMomentUtc = now.AddSeconds(WindupSeconds / p.AttackSpeed);
        _swingEndUtc = _hitMomentUtc.AddSeconds(BackswingSeconds / p.AttackSpeed);

        // 애니 송출(서버 권한)
        string animName = AnimAttackT;

        CheckPassive(p);

        if (IsPassiveAttack)
        {
            animName = AnimAttackT;
            p.Skill.StartCooldown(_keyCode);
            p.SendSkillCostPacket(_keyCode, p.Skill.GetCooldown(_keyCode));
            IsPassiveAttack = false;
        }
        else
        {
            animName = (_attackIndex == 0) ? AnimAttackA : AnimAttackB;
            _attackIndex = 1 - _attackIndex;
        }

        // 전투 상태 평타 칠 때마다 갱신
        // 전투 모드
        {
            p.CombatState = CombatState.Combat;
            S_CombatMode combatModePkt = new S_CombatMode();
            combatModePkt.CombatMode = p.CombatState;
            p.Room.Push(p.Session.Send, combatModePkt);
            p.CombatTime = 0f;
        }

        p.SendAnimPacket(animName, 0.05f, p.AttackSpeed, true);
    }

    protected override void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead || target.IsUntargetable())
            return;

        GameRoom room = p.Room;

        // 스킬 데미지
        if (IsPassiveAttack)
        {
            room.Push(room.AttackSkillTarget, p, target, _keyCode);
            room.Push(room.AddStatusEffect, p, target, _keyCode, "Hit"); // 방깎
        }
        
        // 평타 데미지
        room.Push(target.OnDamaged, p, p.Attack, false, true);
    }

    void CheckPassive(Player player)
    {
        if (player.Skill.IsPassiveAttackReady())
        {
            player.BonusAttackRange = 0.1f;
            IsPassiveAttack = true;
        }
        else
        {
            player.BonusAttackRange = 0;
            IsPassiveAttack = false;
        }
    }
}
