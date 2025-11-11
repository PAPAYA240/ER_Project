using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Server.Data.DataUtils;

public class Abigail_AttackState : Player_AttackState
{
    static readonly float _tAttackRange = 2.15f;
    private const string AnimAttackT = "SKILL_T";
    KeyCode _keyCode = KeyCode.T;
    bool IsPassiveAttack = false;

    public Abigail_AttackState(int targetId, bool chaseAllowed = true, float attackRange = DefaultAttackRange) : base(targetId, chaseAllowed, _tAttackRange)
    {
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
        
        if (p.Skill.IsPassiveAttackReady())
        {
            animName = AnimAttackT;
            p.Skill.StartCooldown(_keyCode);
            p.SendSkillCostPacket(_keyCode, p.Skill.GetCooldown(_keyCode));
            IsPassiveAttack = true;
        }
        else
        {
            animName = (_attackIndex == 0) ? AnimAttackA : AnimAttackB;
            _attackIndex = 1 - _attackIndex;
            IsPassiveAttack = false;
        }

        p.SendAnimPacket(animName, 0.05f);
    }

    protected override void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead)
            return;

        // 스킬 데미지
        if (IsPassiveAttack)
            p.Room.Push(p.Room.AttackSkillTarget, p, target, _keyCode);

        // 평타 데미지
        target.OnDamaged(p, p.Attack, false, true);
    }
}
