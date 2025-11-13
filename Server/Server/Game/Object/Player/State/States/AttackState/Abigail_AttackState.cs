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

    protected override void StartSwing(Player p, DateTime now)
    {
        _swingActive = true;
        _damageApplied = false;

        _swingStartUtc = now;
        _hitMomentUtc = now.AddSeconds(WindupSeconds);
        _swingEndUtc = _hitMomentUtc.AddSeconds(BackswingSeconds);

        // 애니 송출(서버 권한)
        string animName = AnimAttackT;

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

        p.SendAnimPacket(animName, 0.05f);
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
}
