using Google.Protobuf.Protocol;
using Server.Game;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using static Player_StunState;
using static Server.Data.DataUtils;

public class Yuki_AttackState : Player_AttackState
{
    private const string AnimAttackT = "SKILL_Q";
    KeyCode _keyCode = KeyCode.Q;
    bool IsPassiveAttack = false;

    public Yuki_AttackState(int targetId, bool chaseAllowed = true) : base(targetId, chaseAllowed)
    {
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

        if (p.AttackActive == true)
        {
            p.AttackActive = false;

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

        p.SendAnimPacket(animName, 0.05f, p.AttackSpeed, true);
    }

    protected override void ApplyHit(Player p, GameObject target)
    {
        if (target == null || target.State == CreatureState.Dead || target.IsUntargetable())
            return;

        GameRoom room = p.Room;

        if (IsPassiveAttack)
        {
            Player targetPlayer = target as Player;

            if (targetPlayer != null)
            {
                StunStateDesc desc = new StunStateDesc();
                desc.EndPos = target.Position;
                desc.Duration = 0.5f;
                desc.Speed = 0f;

                targetPlayer.ChangeState(new Player_StunState(desc));
            }    
            //room.Push(target.OnDamaged, p, p.Attack, false, true);
        }

        // 평타 데미지
        room.Push(target.OnDamaged, p, p.Attack, false, true);
    }
}
